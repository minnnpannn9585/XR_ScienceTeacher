using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

/// <summary>Catch player-only startup failures before an APK reaches a headset.</summary>
public class XRBuildValidation : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => -50;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            ValidateAndroid();
        }
    }

    public static void ValidateAndroid()
    {
        var general = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        Require(general != null && general.InitManagerOnStart, "Android must initialize XR on startup.");
        Require(general.Manager != null && general.Manager.activeLoaders.Any(loader => loader is OpenXRLoader),
            "Android must have an OpenXR loader assigned.");

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Require(settings != null, "Android OpenXR settings are missing.");
        var types = new HashSet<Type>();
        foreach (var feature in settings.GetFeatures())
        {
            Require(feature != null, "OpenXR settings contain a missing feature script. Refresh the OpenXR features.");
            var script = MonoScript.FromScriptableObject(feature);
            // An empty script path also makes Unity's OpenXR build hook match ALL
            // native plugins and can exclude the real loader AAR from the APK.
            Require(script != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(script)),
                "OpenXR feature needs its own matching .cs file: " + feature.GetType().Name);
            Require(types.Add(feature.GetType()), "Duplicate OpenXR feature: " + feature.GetType().Name);
        }

        var importer = AssetImporter.GetAtPath("Packages/com.unity.xr.openxr/RuntimeLoaders/android/openxr_loader.aar") as PluginImporter;
        Require(importer != null && importer.GetCompatibleWithPlatform(BuildTarget.Android), "OpenXR Android loader AAR is not enabled.");

        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        Require(scenes.Length > 0 && scenes[0].path == "Assets/Scenes/MainMenu.unity", "MainMenu must be the first enabled build scene.");
        Require(scenes.All(scene => File.Exists(scene.path)), "An enabled build scene is missing.");

        foreach (string name in new[] { "LitOpaque", "LitOpaqueEmissive", "LitTransparent", "LitTransparentEmissive", "UnlitOpaque", "UnlitTransparent", "ParticlesUnlit" })
        {
            var material = Resources.Load<Material>(RuntimeMaterials.ResourceFolder + name);
            Require(material != null && material.shader != null, "Runtime material/shader missing: " + name);
        }

        Debug.Log("[XR Build] Android startup settings, feature scripts and runtime materials validated.");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android || !report.summary.outputPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using (var apk = ZipFile.OpenRead(report.summary.outputPath))
        {
            Require(apk.GetEntry("lib/arm64-v8a/libopenxr_loader.so") != null,
                "APK is missing libopenxr_loader.so; this produces an endless Quest system loading screen.");
            Require(apk.GetEntry("lib/arm64-v8a/libUnityOpenXR.so") != null, "APK is missing libUnityOpenXR.so.");
            Require(apk.GetEntry("lib/arm64-v8a/libmock_runtime.so") == null, "Production APK contains the OpenXR mock runtime.");
        }

        Debug.Log("[XR Build] APK contains the OpenXR runtime loader and no mock runtime.");
    }

    [MenuItem("XR/Build Android Startup Fix")]
    public static void BuildStartupFix()
    {
        BuildQuest("startup-fix");
    }

    [MenuItem("XR/Build Android Hand Interaction Fix")]
    public static void BuildHandInteractionFix()
    {
        XRInteractionChecks.Run();
        BuildQuest("hand-interaction-fix");
    }

    [MenuItem("XR/Build Android Official Interactions")]
    public static void BuildOfficialInteractions()
    {
        OfficialXRSetup.Prepare();
        XRInteractionChecks.Run();
        BuildQuest("official-interactions");
    }

    static void BuildQuest(string suffix)
    {
        RuntimeMaterialSetup.EnsureAssets();
        OpenXRDeviceSetup.Apply();
        ValidateAndroid();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.Android,
            locationPathName = "Builds/XR-ScienceTeacher-Quest-" + suffix + ".apk",
            options = BuildOptions.None
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/quest-build-validation.txt", report.summary.result + "\n" + report.summary.totalErrors + " errors\n" + report.summary.outputPath);
        Require(report.summary.result == BuildResult.Succeeded, "Quest build failed. See the build report.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new BuildFailedException(message);
        }
    }
}
