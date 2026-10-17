using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(
        UiName = "Rokid Controller",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "XR Science Teacher",
        Desc = "Maps Rokid OpenXR controllers onto trigger, grip, thumbstick and aim.",
        Version = "1.0.0",
        Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
        FeatureId = "com.xrscienceteacher.openxr.rokid",
        OpenxrExtensionStrings = "")]
#endif
    public sealed class RokidControllerProfile : VendorControllerProfile
    {
        protected override string ProfilePath => "/interaction_profiles/rokid/rokid_controller";
        protected override string ProductName => "Rokid Controller OpenXR";
        protected override Type DeviceType => typeof(RokidController);
    }
}
