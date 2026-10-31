using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Hardware-independent restoration and unsupported-device regression checks.</summary>
public static class XRRealityModeChecks
{
    [MenuItem("XR/Run VR MR Checks")]
    public static void Run()
    {
        Require(!EditorApplication.isPlaying, "Run outside Play Mode.");
        XRRealityBuildChecks.Run();
        QuestPassthroughChecks.Run();
        var root = new GameObject("VR MR checks");
        bool fog = RenderSettings.fog;
        try
        {
            RenderSettings.fog = true;
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            camera.allowHDR = true;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var room = new GameObject("Environment");
            room.transform.SetParent(root.transform);
            var visible = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visible.transform.SetParent(room.transform);
            var hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hidden.transform.SetParent(room.transform);
            hidden.GetComponent<Renderer>().enabled = false;
            hidden.GetComponent<Collider>().enabled = false;
            var volume = room.AddComponent<Volume>();
            var light = room.AddComponent<Light>();
            var experiment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            experiment.transform.SetParent(root.transform);
            var view = root.AddComponent<XRRealityPresentation>();
            view.Configure(camera, room.transform);

            for (int i = 0; i < 20; i++)
            {
                view.Apply(true);
                view.Apply(true);
                Require(camera.clearFlags == CameraClearFlags.SolidColor && camera.backgroundColor == Color.clear,
                    "MR camera has a transparent clear.");
                Require(!camera.allowHDR && !data.renderPostProcessing && data.antialiasing == AntialiasingMode.None,
                    "MR preserves compositor alpha through URP.");
                Require(!RenderSettings.fog && !visible.GetComponent<Renderer>().enabled &&
                    !visible.GetComponent<Collider>().enabled && !volume.enabled, "MR removes virtual environment and fog.");
                Require(room.activeSelf && light.enabled && experiment.GetComponent<Renderer>().enabled &&
                    experiment.GetComponent<Collider>().enabled, "MR retains lights and interactive experiment objects.");
                view.Apply(false);
                Require(camera.clearFlags == CameraClearFlags.Skybox && camera.backgroundColor == new Color(0.1f, 0.2f, 0.3f, 1f),
                    "VR restores original camera values.");
                Require(camera.allowHDR && data.renderPostProcessing && data.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                    "VR restores original URP values.");
                Require(RenderSettings.fog && visible.GetComponent<Renderer>().enabled && visible.GetComponent<Collider>().enabled && volume.enabled,
                    "VR restores environment.");
                Require(!hidden.GetComponent<Renderer>().enabled && !hidden.GetComponent<Collider>().enabled,
                    "VR does not enable originally disabled objects.");
            }

            view.Apply(true);
            // Edit-mode-created MonoBehaviours do not reliably receive runtime
            // lifecycle messages. Exercise the cleanup callback explicitly.
            typeof(XRRealityPresentation).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            UnityEngine.Object.DestroyImmediate(view);
            Require(RenderSettings.fog && camera.backgroundColor.a == 1f && visible.GetComponent<Renderer>().enabled,
                "Scene teardown restores presentation.");

            var mode = root.AddComponent<XRRealityMode>();
            Require(!mode.IsMixedReality && !mode.IsAvailable, "Default is VR; unavailable runtime cannot offer MR.");
            Require(!mode.SetMixedReality(true) && !mode.IsMixedReality && mode.HasError,
                "Unsupported MR request retains VR and reports failure.");
            Require(mode.SetMixedReality(false) && !mode.HasError, "VR recovery always succeeds.");

            CheckModeButton(root.transform, camera);

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/xr-reality-mode-checks.txt",
                "PASS: 20 VR/MR presentation round trips, camera alpha and URP restoration, environment/fog/colliders, preserved lighting/experiment, initially disabled objects, scene cleanup, unsupported-device fallback, independent world-space mode button, XR raycaster, nonoverlapping placement, modal isolation, duplicate attach, bilingual copy, Android configuration.\n");
            Debug.Log("XR_REALITY_MODE_CHECKS_PASSED");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            RenderSettings.fog = fog;
        }
    }

    static void CheckModeButton(Transform root, Camera camera)
    {
        var stationObject = new GameObject("Mode button station");
        stationObject.transform.SetParent(root, false);
        var station = stationObject.AddComponent<XRDeskStation>();
        station.Configure(null, new Vector3(1.05f, XRDeskStation.SurfaceHeight, 0.15f), 0.78f);
        XRRealityModeButton.Attach(station, camera);
        XRRealityModeButton.Attach(station, camera);
        var controls = station.GetComponentsInChildren<XRRealityModeButton>(true);
        Require(controls.Length == 1, "Repeated Attach creates one mode control.");
        var control = controls[0];
        var canvas = control.GetComponent<Canvas>();
        Require(canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == camera,
            "Mode control has its own world canvas and XR event camera.");
        Require(control.transform.parent == station.transform && control.GetComponent<XROverlayCanvas>() == null,
            "Mode control stays outside desktop HUD reflow.");
        Require(control.GetComponent<TrackedDeviceGraphicRaycaster>() != null,
            "Hands and controllers can raycast the mode button.");
        var button = control.GetComponentInChildren<Button>(true);
        Require(button != null && button.GetComponentsInParent<CanvasGroup>(true).Length == 0,
            "No modal CanvasGroup can disable the mode button.");
        Require(button.GetComponentInChildren<TMP_Text>().text == Loc.Get("xr.switchToMR"),
            "VR mode labels its target action as MR.");
        Require(!button.interactable, "Unavailable passthrough disables the MR action.");

        var rightObject = new GameObject("Right desk modal", typeof(RectTransform), typeof(CanvasGroup));
        var right = rightObject.GetComponent<RectTransform>();
        right.SetParent(station.transform, false);
        right.sizeDelta = XRDeskStation.PanelPixels;
        station.PlacePanel(right, true);
        var corners = new Vector3[4];
        control.GetComponent<RectTransform>().GetWorldCorners(corners);
        foreach (var corner in corners)
            Require(right.InverseTransformPoint(corner).y >= right.rect.yMax + 15.9f,
                "Mode control is outside the entire right desk/settings panel with a gap.");
        rightObject.GetComponent<CanvasGroup>().interactable = false;
        button.interactable = true;
        Require(button.IsInteractable(), "Result-panel disabling leaves mode control interactive.");
        button.interactable = false;

        string[] keys = { "xr.switchToMR", "xr.switchToVR", "xr.modeVR", "xr.modeMR", "xr.modeUnavailable", "xr.modeError" };
        foreach (string key in keys)
        {
            string chinese = UiCopy.Fallback(key, false);
            string english = UiCopy.Fallback(key, true);
            Require(!string.IsNullOrEmpty(chinese) && chinese != key && !string.IsNullOrEmpty(english) && english != key && chinese != english,
                "Chinese and English fallback copy exists for " + key);
        }
        Require(UiCopy.Fallback("xr.switchToMR", false) == "切换到 MR" && UiCopy.Fallback("xr.switchToVR", true) == "Switch to VR",
            "Switch labels describe the destination mode in both languages.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("VR/MR check failed: " + message);
    }
}
