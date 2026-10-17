using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

[InitializeOnLoad]
static class OpenXRDeviceSetup
{
    static int _attempts;

    static OpenXRDeviceSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("XR/Configure OpenXR Devices")]
    public static void Apply()
    {
        _attempts++;
        bool android = Enable(BuildTargetGroup.Android);
        bool standalone = Enable(BuildTargetGroup.Standalone);
        if ((!android || !standalone) && _attempts < 8)
        {
            EditorApplication.delayCall += Apply;
        }
    }

    static bool Enable(BuildTargetGroup group)
    {
        FeatureHelpers.RefreshFeatures(group);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (settings == null)
        {
            return false;
        }

        bool sawPico = false;
        bool changed = false;
        var features = settings.GetFeatures();
        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            if (feature == null)
            {
                continue;
            }

            string name = feature.GetType().Name;
            if (name == nameof(Pico4ControllerProfile))
            {
                sawPico = true;
            }

            bool on = Wanted(name, group);
            if (feature.enabled == on)
            {
                continue;
            }

            feature.enabled = on;
            EditorUtility.SetDirty(feature);
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        return sawPico;
    }

    static bool Wanted(string typeName, BuildTargetGroup group)
    {
        switch (typeName)
        {
            case nameof(HandInteractionProfile):
            case nameof(HandCommonPosesInteraction):
            case nameof(PalmPoseInteraction):
            case nameof(OculusTouchControllerProfile):
            case nameof(MetaQuestTouchProControllerProfile):
            case nameof(MetaQuestTouchPlusControllerProfile):
            case nameof(KHRSimpleControllerProfile):
            case nameof(MicrosoftMotionControllerProfile):
            case nameof(Pico4ControllerProfile):
            case nameof(PicoNeo3ControllerProfile):
            case nameof(Pico4SControllerProfile):
            case nameof(RokidControllerProfile):
                return true;
            case "HandTracking":
            case "MetaHandTrackingAim":
                return true;
            case "MetaQuestFeature":
                return group == BuildTargetGroup.Android;
            default:
                return false;
        }
    }
}
