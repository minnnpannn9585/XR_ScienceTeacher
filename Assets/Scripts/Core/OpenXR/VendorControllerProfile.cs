using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR.OpenXR.Input;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_INPUT_SYSTEM_POSE_CONTROL
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#else
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif
#if USE_STICK_CONTROL_THUMBSTICKS
using ThumbstickControl = UnityEngine.InputSystem.Controls.StickControl;
#else
using ThumbstickControl = UnityEngine.InputSystem.Controls.Vector2Control;
#endif

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
    /// <summary>
    /// Shared OpenXR controller layout. Concrete profiles below register the same
    /// trigger, grip, stick and aim paths for Pico and Rokid, so the rig can keep
    /// using the generic XRController bindings.
    /// </summary>
    [Preserve]
    public class VendorControllerDevice : XRControllerWithRumble
    {
        [Preserve, InputControl(aliases = new[] { "Primary2DAxis", "Joystick" }, usage = "Primary2DAxis")]
        public ThumbstickControl thumbstick { get; private set; }

        [Preserve, InputControl(aliases = new[] { "GripAxis", "squeeze" }, usage = "Grip")]
        public AxisControl grip { get; private set; }

        [Preserve, InputControl(aliases = new[] { "GripButton", "squeezeClicked" }, usage = "GripButton")]
        public ButtonControl gripPressed { get; private set; }

        [Preserve, InputControl(aliases = new[] { "Primary", "menuButton" }, usage = "MenuButton")]
        public ButtonControl menu { get; private set; }

        [Preserve, InputControl(aliases = new[] { "A", "X" }, usage = "PrimaryButton")]
        public ButtonControl primaryButton { get; private set; }

        [Preserve, InputControl(aliases = new[] { "B", "Y" }, usage = "SecondaryButton")]
        public ButtonControl secondaryButton { get; private set; }

        [Preserve, InputControl(usage = "Trigger")]
        public AxisControl trigger { get; private set; }

        [Preserve, InputControl(aliases = new[] { "indexButton", "triggerbutton" }, usage = "TriggerButton")]
        public ButtonControl triggerPressed { get; private set; }

        [Preserve, InputControl(aliases = new[] { "thumbstickClick" }, usage = "Primary2DAxisClick")]
        public ButtonControl thumbstickClicked { get; private set; }

        [Preserve, InputControl(offset = 0, aliases = new[] { "device", "gripPose" }, usage = "Device")]
        public PoseControl devicePose { get; private set; }

        [Preserve, InputControl(offset = 0, alias = "aimPose", usage = "Pointer")]
        public PoseControl pointer { get; private set; }

        [Preserve, InputControl(offset = 28, usage = "IsTracked")]
        public new ButtonControl isTracked { get; private set; }

        [Preserve, InputControl(offset = 32, usage = "TrackingState")]
        public new IntegerControl trackingState { get; private set; }

        [Preserve, InputControl(offset = 36, noisy = true, alias = "gripPosition")]
        public new Vector3Control devicePosition { get; private set; }

        [Preserve, InputControl(offset = 48, noisy = true, alias = "gripOrientation")]
        public new QuaternionControl deviceRotation { get; private set; }

        [Preserve, InputControl(offset = 96)]
        public Vector3Control pointerPosition { get; private set; }

        [Preserve, InputControl(offset = 108, alias = "pointerOrientation")]
        public QuaternionControl pointerRotation { get; private set; }

        [Preserve, InputControl(usage = "Haptic")]
        public HapticControl haptic { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            thumbstick = GetChildControl<ThumbstickControl>("thumbstick");
            grip = GetChildControl<AxisControl>("grip");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            menu = GetChildControl<ButtonControl>("menu");
            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            devicePose = GetChildControl<PoseControl>("devicePose");
            pointer = GetChildControl<PoseControl>("pointer");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
            pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");
            haptic = GetChildControl<HapticControl>("haptic");
        }
    }

    [Preserve, InputControlLayout(displayName = "Pico 4 Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class Pico4Controller : VendorControllerDevice
    {
    }

    [Preserve, InputControlLayout(displayName = "Pico Neo3 Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class PicoNeo3Controller : VendorControllerDevice
    {
    }

    [Preserve, InputControlLayout(displayName = "Pico 4 Ultra Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class Pico4SController : VendorControllerDevice
    {
    }

    [Preserve, InputControlLayout(displayName = "Rokid Controller (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class RokidController : VendorControllerDevice
    {
    }

    public abstract class VendorControllerProfile : OpenXRInteractionFeature
    {
        const string Trigger = "/input/trigger/value";
        const string Squeeze = "/input/squeeze/value";
        const string Thumbstick = "/input/thumbstick";
        const string ThumbstickClick = "/input/thumbstick/click";
        const string Menu = "/input/menu/click";
        const string ButtonX = "/input/x/click";
        const string ButtonY = "/input/y/click";
        const string ButtonA = "/input/a/click";
        const string ButtonB = "/input/b/click";
        const string GripPose = "/input/grip/pose";
        const string AimPose = "/input/aim/pose";
        const string HapticPath = "/output/haptic";

        protected abstract string ProfilePath { get; }
        protected abstract string ProductName { get; }
        protected abstract Type DeviceType { get; }

        static readonly System.Collections.Generic.HashSet<string> Registered = new System.Collections.Generic.HashSet<string>();

        protected override void RegisterDeviceLayout()
        {
            if (!Registered.Add(DeviceType.Name))
            {
                return;
            }

            global::UnityEngine.InputSystem.InputSystem.RegisterLayout(DeviceType,
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(ProductName));
        }

        protected override void UnregisterDeviceLayout()
        {
            if (!Registered.Remove(DeviceType.Name))
            {
                return;
            }

            global::UnityEngine.InputSystem.InputSystem.RemoveLayout(DeviceType.Name);
        }

        protected override string GetDeviceLayoutName()
        {
            return DeviceType.Name;
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            var map = new ActionMapConfig
            {
                name = DeviceType.Name.ToLowerInvariant(),
                localizedName = ProductName,
                desiredInteractionProfile = ProfilePath,
                manufacturer = ProductName,
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left),
                        userPath = UserPaths.leftHand
                    },
                    new DeviceConfig
                    {
                        characteristics = (InputDeviceCharacteristics)(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right),
                        userPath = UserPaths.rightHand
                    }
                },
                actions = BuildActions()
            };
            AddActionMap(map);
        }

        List<ActionConfig> BuildActions()
        {
            return new List<ActionConfig>
            {
                Axis2("thumbstick", "Thumbstick", "Primary2DAxis", Thumbstick),
                Axis1("grip", "Grip", "Grip", Squeeze),
                Button("gripPressed", "Grip Pressed", "GripButton", Squeeze, null),
                Button("menu", "Menu", "MenuButton", Menu, UserPaths.leftHand),
                ButtonPair("primaryButton", "Primary Button", "PrimaryButton", ButtonX, ButtonA),
                ButtonPair("secondaryButton", "Secondary Button", "SecondaryButton", ButtonY, ButtonB),
                Axis1("trigger", "Trigger", "Trigger", Trigger),
                Button("triggerPressed", "Trigger Pressed", "TriggerButton", Trigger, null),
                Button("thumbstickClicked", "Thumbstick Clicked", "Primary2DAxisClick", ThumbstickClick, null),
                Pose("devicePose", "Device Pose", "Device", GripPose),
                Pose("pointer", "Pointer Pose", "Pointer", AimPose),
                HapticAction()
            };
        }

        ActionConfig Axis2(string name, string label, string usage, string path)
        {
            return new ActionConfig
            {
                name = name,
                localizedName = label,
                type = ActionType.Axis2D,
                usages = new List<string> { usage },
                bindings = Both(path)
            };
        }

        ActionConfig Axis1(string name, string label, string usage, string path)
        {
            return new ActionConfig
            {
                name = name,
                localizedName = label,
                type = ActionType.Axis1D,
                usages = new List<string> { usage },
                bindings = Both(path)
            };
        }

        ActionConfig Button(string name, string label, string usage, string path, string userPath)
        {
            var binding = new ActionBinding
            {
                interactionPath = path,
                interactionProfileName = ProfilePath
            };
            if (!string.IsNullOrEmpty(userPath))
            {
                binding.userPaths = new List<string> { userPath };
            }

            return new ActionConfig
            {
                name = name,
                localizedName = label,
                type = ActionType.Binary,
                usages = new List<string> { usage },
                bindings = new List<ActionBinding> { binding }
            };
        }

        ActionConfig ButtonPair(string name, string label, string usage, string leftPath, string rightPath)
        {
            return new ActionConfig
            {
                name = name,
                localizedName = label,
                type = ActionType.Binary,
                usages = new List<string> { usage },
                bindings = new List<ActionBinding>
                {
                    new ActionBinding
                    {
                        interactionPath = leftPath,
                        interactionProfileName = ProfilePath,
                        userPaths = new List<string> { UserPaths.leftHand }
                    },
                    new ActionBinding
                    {
                        interactionPath = rightPath,
                        interactionProfileName = ProfilePath,
                        userPaths = new List<string> { UserPaths.rightHand }
                    }
                }
            };
        }

        ActionConfig Pose(string name, string label, string usage, string path)
        {
            return new ActionConfig
            {
                name = name,
                localizedName = label,
                type = ActionType.Pose,
                usages = new List<string> { usage },
                bindings = Both(path)
            };
        }

        ActionConfig HapticAction()
        {
            return new ActionConfig
            {
                name = "haptic",
                localizedName = "Haptic Output",
                type = ActionType.Vibrate,
                usages = new List<string> { "Haptic" },
                bindings = Both(HapticPath)
            };
        }

        List<ActionBinding> Both(string path)
        {
            return new List<ActionBinding>
            {
                new ActionBinding
                {
                    interactionPath = path,
                    interactionProfileName = ProfilePath
                }
            };
        }
    }

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

#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(
        UiName = "Pico 4 Ultra Controller",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "XR Science Teacher",
        Desc = "Maps Pico 4 Ultra / 4S OpenXR controllers onto trigger, grip, thumbstick and aim.",
        Version = "1.0.0",
        Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
        FeatureId = "com.xrscienceteacher.openxr.pico4s",
        OpenxrExtensionStrings = "")]
#endif
    public sealed class Pico4SControllerProfile : VendorControllerProfile
    {
        protected override string ProfilePath => "/interaction_profiles/bytedance/pico4s_controller";
        protected override string ProductName => "Pico 4 Ultra Controller OpenXR";
        protected override Type DeviceType => typeof(Pico4SController);
    }

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
