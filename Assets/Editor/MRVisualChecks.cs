using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Presentation restoration checks; no simulated camera feed or headset-performance claim.</summary>
public static class MRVisualChecks
{
    const string ReportDirectory = "Logs/MRVisualChecks";
    const string ReportPath = ReportDirectory + "/results.txt";

    [MenuItem("XR/Run MR Visual Checks")]
    public static void Run()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Run from Edit Mode.");
        Directory.CreateDirectory(ReportDirectory);
        try
        {
            RuntimeMaterialSetup.EnsureAssets();
            XRRealityModeChecks.Run();
            CheckGlass();
            CheckDecoration();
            CheckPanels();
            CheckLighting();
            File.WriteAllText(ReportPath, "PASS\n" +
                "Existing native passthrough, compositor alpha, Android configuration and VR/MR restoration checks passed.\n" +
                "MRGlass resource/shader available with no reported compiler errors; transparent runtime instances preserve the resource template.\n" +
                "20 decoration round trips preserve disabled renderers/colliders and leave experiment siblings visible and interactive.\n" +
                "20 panel round trips preserve text, color restoration and UI raycast behavior.\n" +
                "20 lighting round trips restore current lights and ambient settings; unrelated experiment lights remain unchanged.\n" +
                "Disable cleanup restores decoration, UI and lighting while in MR.\n" +
                "These checks do not verify passthrough image quality, headset frame rate or real-world registration.\n");
            Debug.Log("MR_VISUAL_CHECKS_PASSED");
        }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath, "FAIL\n" + error);
            throw;
        }
    }

    static void CheckGlass()
    {
        var template = Resources.Load<Material>(RuntimeMaterials.ResourceFolder + "MRGlass");
        Require(template != null && template.shader != null, "MRGlass resource must retain its shader in player builds.");
        Require(template.shader.name == "ScienceTeacher/MRGlass", "MRGlass must use the intended shader.");
        Require(!ShaderUtil.ShaderHasError(template.shader), "MRGlass shader has compiler errors.");
        Color original = template.GetColor("_BaseColor");
        Material normal = null, selected = null;
        try
        {
            normal = MRVisualStyle.Glass(MRVisualStyle.Cyan);
            selected = MRVisualStyle.Glass(MRVisualStyle.Gold, 0.3f);
            Require(normal != template && selected != template && normal != selected,
                "Runtime glass must own separate instances, so tinting cannot modify the template.");
            Require(normal.renderQueue >= (int)RenderQueue.Transparent, "Glass must render in the transparent queue.");
            Require(normal.GetColor("_BaseColor").a > 0f && normal.GetColor("_BaseColor").a < 0.25f,
                "Default glass must preserve a visible real-world view through the teaching object.");
            Require(selected.GetColor("_BaseColor").a > normal.GetColor("_BaseColor").a &&
                selected.GetColor("_BaseColor").a < 0.5f, "Selection must be more visible while retaining transparency.");
            UrpMaterialUtil.SetColor(normal, new Color(0.1f, 0.2f, 0.3f, 0.12f));
            Require(template.GetColor("_BaseColor") == original, "Runtime tint must not mutate MRGlass's resource template.");
        }
        finally
        {
            if (normal != null) Object.DestroyImmediate(normal);
            if (selected != null) Object.DestroyImmediate(selected);
        }
    }

    static void CheckDecoration()
    {
        var root = Temporary("MR visibility checks");
        try
        {
            var decor = Temporary("Registered decoration", root.transform);
            var visible = Primitive("Visible furniture", decor.transform);
            var hidden = Primitive("Initially disabled decoration", decor.transform);
            var mixed = Primitive("Visible nonblocking decoration", decor.transform);
            var experiment = Primitive("Experiment stays interactive", root.transform);
            hidden.GetComponent<Renderer>().enabled = false;
            hidden.GetComponent<Collider>().enabled = false;
            mixed.GetComponent<Collider>().enabled = false;
            var visibility = decor.AddComponent<MRDecorVisibility>();
            visibility.Configure();
            visibility.Configure();
            for (int i = 0; i < 20; i++)
            {
                // Changes while in VR must be captured on the next MR entry.
                bool visibleCollider = i % 2 == 0;
                visible.GetComponent<Collider>().enabled = visibleCollider;
                visibility.Apply(true);
                visibility.Apply(true);
                Require(decor.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled), "MR hides every registered decoration renderer.");
                Require(decor.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), "MR removes invisible furniture's ray blockers.");
                Require(decor.activeSelf && experiment.activeSelf && experiment.GetComponent<Renderer>().enabled && experiment.GetComponent<Collider>().enabled,
                    "MR must leave experiment objects and their interaction colliders alive.");
                visibility.Apply(false);
                Require(visible.GetComponent<Renderer>().enabled && visible.GetComponent<Collider>().enabled == visibleCollider,
                    "VR restores the latest visible furniture state.");
                Require(!hidden.GetComponent<Renderer>().enabled && !hidden.GetComponent<Collider>().enabled,
                    "VR must not enable originally disabled decoration.");
                Require(mixed.GetComponent<Renderer>().enabled && !mixed.GetComponent<Collider>().enabled,
                    "Renderer and collider state must restore independently.");
            }
            visibility.Apply(true);
            DisableCallback(visibility);
            Require(visible.GetComponent<Renderer>().enabled && !hidden.GetComponent<Renderer>().enabled,
                "Disabling the component while in MR must restore captured decoration state.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void CheckPanels()
    {
        var root = Temporary("MR panel checks");
        try
        {
            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(root.transform, false);
            var panel = panelObject.GetComponent<Image>();
            var hairlineObject = new GameObject("Hairline", typeof(RectTransform), typeof(Image));
            hairlineObject.transform.SetParent(panelObject.transform, false);
            var hairline = hairlineObject.GetComponent<Image>();
            var labelObject = new GameObject("Teaching text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panelObject.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "A + B + C = 180°";
            label.color = new Color(0.96f, 0.93f, 0.84f);
            label.fontSize = 34f;
            panel.color = new Color(0.05f, 0.08f, 0.1f, 0.9f);
            hairline.color = new Color(0.7f, 0.6f, 0.4f, 0.85f);
            panel.raycastTarget = true;
            hairline.raycastTarget = false;
            Color panelColor = panel.color, lineColor = hairline.color, textColor = label.color;
            var style = panelObject.AddComponent<MRPanelStyle>();
            style.Configure(panel, hairline);
            for (int i = 0; i < 20; i++)
            {
                style.Apply(true);
                Require(panel.color.a >= 0.6f && panel.color.a < panelColor.a, "MR panel must balance readable text and a visible background.");
                Require(hairline.color != lineColor, "MR panel should show the new edge accent.");
                Require(label.text == "A + B + C = 180°" && label.color == textColor && Mathf.Approximately(label.fontSize, 34f),
                    "MR styling must not rewrite or fade teaching text.");
                Require(panel.raycastTarget && !hairline.raycastTarget, "MR styling must preserve UI hit testing.");
                style.Apply(false);
                Require(panel.color == panelColor && hairline.color == lineColor, "VR restores both panel colors exactly.");
            }
            style.Apply(true);
            DisableCallback(style);
            Require(panel.color == panelColor && hairline.color == lineColor, "Disabling MR UI styling restores its original colors.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void CheckLighting()
    {
        var root = Temporary("MR lighting checks");
        var initialMode = RenderSettings.ambientMode;
        Color initialSky = RenderSettings.ambientSkyColor, initialEquator = RenderSettings.ambientEquatorColor, initialGround = RenderSettings.ambientGroundColor;
        try
        {
            var studio = Temporary("Studio lights", root.transform);
            var key = Temporary("Key", studio.transform).AddComponent<Light>();
            key.type = LightType.Directional;
            var pool = Temporary("Pool", studio.transform).AddComponent<Light>();
            pool.type = LightType.Point;
            pool.intensity = 3.2f;
            pool.color = new Color(1f, 0.75f, 0.4f);
            var disabled = Temporary("Disabled fill", studio.transform).AddComponent<Light>();
            disabled.type = LightType.Directional;
            disabled.enabled = false;
            disabled.intensity = 0.4f;
            disabled.color = Color.blue;
            var experiment = Temporary("Experiment light", root.transform).AddComponent<Light>();
            experiment.intensity = 0.7f;
            experiment.color = Color.red;
            Color poolColor = pool.color, disabledColor = disabled.color;
            var style = studio.AddComponent<MRStudioLighting>();
            style.Configure();
            for (int i = 0; i < 20; i++)
            {
                key.intensity = 1.1f + i * 0.03f;
                key.color = new Color(1f, 0.82f, 0.6f);
                float keyIntensity = key.intensity;
                Color keyColor = key.color;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientSkyColor = new Color(0.2f + i * 0.005f, 0.25f, 0.3f);
                RenderSettings.ambientEquatorColor = new Color(0.1f, 0.12f, 0.14f);
                RenderSettings.ambientGroundColor = new Color(0.03f, 0.04f, 0.05f);
                Color sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
                style.Apply(true);
                style.Apply(true);
                Require(RenderSettings.ambientMode == AmbientMode.Trilight && RenderSettings.ambientSkyColor != sky,
                    "MR applies neutral ambient illumination.");
                Require(key.intensity > 0f && key.intensity < keyIntensity && Mathf.Approximately(pool.intensity, 0f),
                    "MR softens the virtual key and removes the studio's warm light pool.");
                Require(!disabled.enabled && Mathf.Approximately(experiment.intensity, 0.7f) && experiment.color == Color.red,
                    "MR must not activate disabled lights or modify experiment lights outside the studio.");
                style.Apply(false);
                Require(Mathf.Approximately(key.intensity, keyIntensity) && key.color == keyColor && Mathf.Approximately(pool.intensity, 3.2f) && pool.color == poolColor,
                    "VR restores current key and pool values without cumulative intensity drift.");
                Require(!disabled.enabled && Mathf.Approximately(disabled.intensity, 0.4f) && disabled.color == disabledColor,
                    "Disabled light values restore exactly.");
                Require(RenderSettings.ambientMode == AmbientMode.Flat && RenderSettings.ambientSkyColor == sky &&
                    RenderSettings.ambientEquatorColor == equator && RenderSettings.ambientGroundColor == ground,
                    "VR restores ambient mode and all three ambient colors.");
            }
            float restoredIntensity = key.intensity;
            Color restoredSky = RenderSettings.ambientSkyColor;
            style.Apply(true);
            DisableCallback(style);
            Require(Mathf.Approximately(key.intensity, restoredIntensity) && RenderSettings.ambientSkyColor == restoredSky && RenderSettings.ambientMode == AmbientMode.Flat,
                "Disabling MR lighting restores lights and ambient settings.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            RenderSettings.ambientMode = initialMode;
            RenderSettings.ambientSkyColor = initialSky;
            RenderSettings.ambientEquatorColor = initialEquator;
            RenderSettings.ambientGroundColor = initialGround;
        }
    }

    [MenuItem("XR/Build Android MR Visual Polish")]
    public static void BuildAndroid()
    {
        RuntimeMaterialSetup.EnsureAssets();
        Run();
        XRBuildValidation.ValidateAndroid();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.Android,
            locationPathName = "Builds/XR-ScienceTeacher-Quest-mr-visual-polish.apk",
            options = BuildOptions.None
        });
        File.WriteAllText(ReportDirectory + "/build.txt", report.summary.result + "\n" + report.summary.totalErrors + " errors\n" + report.summary.outputPath);
        Require(report.summary.result == BuildResult.Succeeded, "MR visual polish Android build failed. Check Logs/MRVisualChecks/build.txt.");
        Debug.Log("MR_VISUAL_ANDROID_BUILD_PASSED");
    }

    static GameObject Temporary(string name, Transform parent = null)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject Primitive(string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void DisableCallback(MonoBehaviour component)
    {
        // Edit-mode AddComponent is not guaranteed to receive runtime lifecycle
        // messages. Exercise the real cleanup callback explicitly.
        var callback = component.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(callback != null, component.GetType().Name + " must expose cleanup.");
        callback.Invoke(component, null);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new BuildFailedException("[MR Visual Checks] " + message);
    }
}
