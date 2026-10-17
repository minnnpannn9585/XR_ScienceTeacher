using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(
        UiName = "Pico 4 Controller",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "XR Science Teacher",
        Desc = "Maps Pico 4 OpenXR controllers onto trigger, grip, thumbstick and aim.",
        Version = "1.0.0",
        Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
        FeatureId = "com.xrscienceteacher.openxr.pico4",
        OpenxrExtensionStrings = "")]
#endif
    public sealed class Pico4ControllerProfile : VendorControllerProfile
    {
        protected override string ProfilePath => "/interaction_profiles/bytedance/pico4_controller";
        protected override string ProductName => "Pico 4 Controller OpenXR";
        protected override Type DeviceType => typeof(Pico4Controller);
    }
}
