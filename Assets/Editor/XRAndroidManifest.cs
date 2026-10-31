using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// Declare compositor passthrough as optional so other OpenXR headsets keep VR.
/// Also remove the eye-tracking requirement OpenXR 1.14.3 adds for Quest Pro
/// when the app does not use eye gaze. Passthrough needs no raw-camera permission.
/// </summary>
public class XRAndroidManifest : IPostGenerateGradleAndroidProject
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    internal const string PassthroughFeatureName = "com.oculus.feature.PASSTHROUGH";

    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var gaze = settings != null ? settings.GetFeature<EyeGazeInteraction>() : null;
        var passthrough = settings != null ? settings.GetFeature<QuestPassthroughFeature>() : null;
        bool removeEyeTracking = gaze == null || !gaze.enabled;
        bool enablePassthrough = passthrough != null && passthrough.enabled;
        ConfigureManifest(Path.Combine(path, "src/main/AndroidManifest.xml"), removeEyeTracking, enablePassthrough);
        ConfigureManifest(Path.Combine(path, "xrmanifest.androidlib/AndroidManifest.xml"), removeEyeTracking, enablePassthrough);
    }

    internal static void ConfigureManifest(string path, bool removeEyeTracking, bool enablePassthrough)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var manifest = new XmlDocument();
        manifest.Load(path);
        var namespaces = new XmlNamespaceManager(manifest.NameTable);
        namespaces.AddNamespace("android", AndroidNamespace);
        bool changed = false;
        if (removeEyeTracking)
        {
            var nodes = manifest.SelectNodes(
                "/manifest/uses-feature[@android:name='oculus.software.eye_tracking'] | " +
                "/manifest/uses-permission[@android:name='com.oculus.permission.EYE_TRACKING']", namespaces);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    node.ParentNode.RemoveChild(node);
                    changed = true;
                }
            }
        }

        if (enablePassthrough)
        {
            var feature = manifest.SelectSingleNode(
                "/manifest/uses-feature[@android:name='" + PassthroughFeatureName + "']", namespaces) as XmlElement;
            if (feature == null)
            {
                feature = manifest.CreateElement("uses-feature");
                feature.SetAttribute("name", AndroidNamespace, PassthroughFeatureName);
                manifest.DocumentElement.AppendChild(feature);
                changed = true;
            }

            if (feature.GetAttribute("required", AndroidNamespace) != "false")
            {
                feature.SetAttribute("required", AndroidNamespace, "false");
                changed = true;
            }
        }

        if (changed) manifest.Save(path);
    }
}
