using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// OpenXR 1.14.3 adds a mandatory eye-tracking requirement when Quest Pro is a
/// supported device, even with Eye Gaze disabled. This app only needs hands and
/// controllers, so supporting Quest Pro must not require eye hardware on Quest 3.
/// </summary>
public class XRAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        var gaze = settings != null ? settings.GetFeature<EyeGazeInteraction>() : null;
        if (gaze != null && gaze.enabled)
        {
            return;
        }

        RemoveUnusedEyeTracking(Path.Combine(path, "src/main/AndroidManifest.xml"));
        RemoveUnusedEyeTracking(Path.Combine(path, "xrmanifest.androidlib/AndroidManifest.xml"));
    }

    static void RemoveUnusedEyeTracking(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var manifest = new XmlDocument();
        manifest.Load(path);
        var namespaces = new XmlNamespaceManager(manifest.NameTable);
        namespaces.AddNamespace("android", "http://schemas.android.com/apk/res/android");
        var nodes = manifest.SelectNodes(
            "/manifest/uses-feature[@android:name='oculus.software.eye_tracking'] | " +
            "/manifest/uses-permission[@android:name='com.oculus.permission.EYE_TRACKING']", namespaces);
        if (nodes == null || nodes.Count == 0)
        {
            return;
        }

        foreach (XmlNode node in nodes)
        {
            node.ParentNode.RemoveChild(node);
        }

        manifest.Save(path);
    }
}
