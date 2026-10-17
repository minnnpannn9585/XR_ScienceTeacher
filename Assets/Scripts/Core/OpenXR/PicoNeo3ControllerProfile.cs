using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(
        UiName = "Pico Neo3 Controller",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "XR Science Teacher",
        Desc = "Maps Pico Neo3 OpenXR controllers onto trigger, grip, thumbstick and aim.",
        Version = "1.0.0",
        Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
        FeatureId = "com.xrscienceteacher.openxr.piconeo3",
        OpenxrExtensionStrings = "")]
#endif
    public sealed class PicoNeo3ControllerProfile : VendorControllerProfile
    {
        protected override string ProfilePath => "/interaction_profiles/bytedance/pico_neo3_controller";
        protected override string ProductName => "Pico Neo3 Controller OpenXR";
        protected override Type DeviceType => typeof(PicoNeo3Controller);
    }
}
