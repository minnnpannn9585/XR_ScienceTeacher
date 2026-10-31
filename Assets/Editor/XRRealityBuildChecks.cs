using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;

/// <summary>Build-time coverage for optional passthrough without camera permissions.</summary>
public static class XRRealityBuildChecks
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

    [MenuItem("XR/Run VR MR Build Checks")]
    public static void Run()
    {
        OpenXRDeviceSetup.Apply();
        XRBuildValidation.ValidateAndroid();
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var feature = settings.GetFeature<QuestPassthroughFeature>();
        Require(!new SerializedObject(feature).FindProperty("required").boolValue,
            "Passthrough must be optional so unsupported runtimes can initialize VR.");

        string path = Path.GetTempFileName();
        try
        {
            const string original = "<manifest xmlns:android='http://schemas.android.com/apk/res/android' package='com.test.xr'>" +
                "<application android:label='XR'/>" +
                "<uses-feature android:name='oculus.software.eye_tracking' android:required='true'/>" +
                "<uses-permission android:name='com.oculus.permission.EYE_TRACKING'/>" +
                "<uses-permission android:name='com.oculus.permission.HAND_TRACKING'/></manifest>";

            // Eye-gaze enabled must not short-circuit passthrough insertion.
            File.WriteAllText(path, original);
            XRAndroidManifest.ConfigureManifest(path, false, true);
            CheckManifest(path, true);

            // A second pass is a no-op, and removing unused gaze preserves passthrough/hands.
            string firstPass = File.ReadAllText(path);
            XRAndroidManifest.ConfigureManifest(path, false, true);
            Require(File.ReadAllText(path) == firstPass, "Manifest processing must be idempotent.");
            XRAndroidManifest.ConfigureManifest(path, true, true);
            CheckManifest(path, false);

            // An existing mandatory declaration is made optional, not duplicated.
            File.WriteAllText(path, original.Replace("</manifest>",
                "<uses-feature android:name='com.oculus.feature.PASSTHROUGH' android:required='true'/></manifest>"));
            XRAndroidManifest.ConfigureManifest(path, true, true);
            CheckManifest(path, false);

            File.WriteAllText(path, original);
            XRAndroidManifest.ConfigureManifest(path, false, false);
            Require(File.ReadAllText(path) == original, "Disabled features must leave the manifest unchanged.");
            Debug.Log("XR_REALITY_BUILD_CHECKS_PASSED");
        }
        finally
        {
            File.Delete(path);
        }
    }

    static void CheckManifest(string path, bool expectEyeTracking)
    {
        var manifest = new XmlDocument();
        manifest.Load(path);
        var ns = new XmlNamespaceManager(manifest.NameTable);
        ns.AddNamespace("android", AndroidNamespace);
        var passthrough = manifest.SelectNodes(
            "/manifest/uses-feature[@android:name='com.oculus.feature.PASSTHROUGH']", ns);
        Require(passthrough.Count == 1, "Manifest must declare passthrough exactly once.");
        Require(((XmlElement)passthrough[0]).GetAttribute("required", AndroidNamespace) == "false",
            "Passthrough must not restrict installation on other headsets.");
        Require(manifest.SelectNodes("/manifest/uses-permission", ns).Count == (expectEyeTracking ? 2 : 1),
            "Compositor passthrough must not add camera or other permissions.");
        Require(manifest.SelectSingleNode("/manifest/uses-permission[@android:name='com.oculus.permission.HAND_TRACKING']", ns) != null,
            "Hand tracking permission must be preserved.");
        Require((manifest.SelectSingleNode("/manifest/uses-feature[@android:name='oculus.software.eye_tracking']", ns) != null) == expectEyeTracking,
            "Eye-tracking feature must match the configured gaze setting.");
        Require((manifest.SelectSingleNode("/manifest/uses-permission[@android:name='com.oculus.permission.EYE_TRACKING']", ns) != null) == expectEyeTracking,
            "Eye-tracking permission must match the configured gaze setting.");
        Require(manifest.SelectSingleNode("/manifest/application[@android:label='XR']", ns) != null,
            "Application configuration must be preserved.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
