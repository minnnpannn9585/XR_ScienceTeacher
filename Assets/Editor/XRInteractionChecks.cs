using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Hands;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>Regression checks for tracking-space alignment and the XRI UI press path.</summary>
public static class XRInteractionChecks
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("XR/Run Interaction Checks")]
    public static void Run()
    {
        Require(!EditorApplication.isPlaying, "Run these checks outside Play Mode.");
        var root = new GameObject("XR interaction checks");
        var previousSettings = InputSystem.settings;
        var checkSettings = UnityEngine.Object.Instantiate(previousSettings);
        try
        {
            // Use a temporary copy so gameplay actions can run in edit-mode
            // checks without changing the project's input settings asset.
            checkSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = checkSettings;
            CheckCoordinates(root.transform);
            CheckUiPress(root);
            CheckReadyIsNotPressed();
            CheckMetaHand();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/xr-interaction-checks.txt",
                "PASS: head/hand alignment at all four scene origins, floor/device height, rotated origins, both hands/controllers, aim ray, pinch distance, UI press/release, ready without pinch, Meta hand classification and pinch.\n");
            Debug.Log("XR_INTERACTION_CHECKS_PASSED");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            InputSystem.settings = previousSettings;
            UnityEngine.Object.DestroyImmediate(checkSettings);
        }
    }

    static void CheckCoordinates(Transform root)
    {
        var offset = new GameObject("Camera Offset").transform;
        offset.SetParent(root, false);
        var head = new GameObject("Head").transform;
        head.SetParent(offset, false);
        var target = new GameObject("UI target").transform;
        target.SetParent(offset, false);
        var origins = new[] { new Vector3(0.1f, 0f, -2.4f), new Vector3(0f, 0f, -0.55f), new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, -0.7f) };
        foreach (var origin in origins)
        foreach (float height in new[] { 0f, 1.6f })
        foreach (float yaw in new[] { 0f, 90f })
        foreach (float side in new[] { -1f, 1f })
        foreach (bool controller in new[] { false, true })
        {
            root.SetPositionAndRotation(origin, Quaternion.Euler(0f, yaw, 0f));
            offset.localPosition = Vector3.up * height;
            head.localPosition = new Vector3(0f, 1.6f - height, 0f);
            target.localPosition = head.localPosition + new Vector3(side * 0.2f, -0.2f, 0.78f);
            var aim = head.localPosition + new Vector3(side * 0.2f, -0.2f, 0.3f);
            var sample = new XRHandSample
            {
                Tracked = true, UsingController = controller,
                GripPosition = aim, GripRotation = Quaternion.identity,
                AimOrigin = aim, AimRotation = Quaternion.identity,
                PokePosition = aim + Vector3.forward * 0.03f, PokeRotation = Quaternion.identity,
                HasThumb = !controller, ThumbPosition = aim + new Vector3(0.018f, 0f, 0.03f),
                Trigger = 0.8f, Grip = 0.7f, Pinching = !controller
            };
            var world = sample.ToWorldSpace(offset);
            Require(Mathf.Abs(Vector3.Distance(head.position, world.GripPosition) - Vector3.Distance(head.localPosition, aim)) < 0.0001f,
                "Hand must remain the same distance from the head after the scene/floor offset.");
            Require(Vector3.Distance(head.InverseTransformPoint(world.PokePosition), new Vector3(side * 0.2f, -0.2f, 0.33f)) < 0.0001f,
                "Poke must remain at the fingertip relative to the head.");
            var ray = new Ray(world.AimOrigin, world.AimRotation * Vector3.forward);
            var plane = new Plane(target.forward, target.position);
            Require(plane.Raycast(ray, out float distance) && Vector3.Distance(ray.GetPoint(distance), target.position) < 0.0001f,
                "Aiming at the panel must still hit it after moving or rotating the origin.");
            Require(Quaternion.Angle(world.PokeRotation, head.rotation) < 0.01f && Quaternion.Angle(world.GripRotation, head.rotation) < 0.01f,
                "Hand and fingertip directions must use the head's tracking frame.");
            Require(!world.HasThumb || Mathf.Abs(Vector3.Distance(world.PokePosition, world.ThumbPosition) - 0.018f) < 0.0001f,
                "A world-space conversion must preserve the pinch gap.");
            Require(world.UsingController == controller && world.Trigger == 0.8f && world.Grip == 0.7f && world.Pinching == !controller,
                "Converting a pose must not change the input state.");
        }

        Require(!default(XRHandSample).ToWorldSpace(offset).Tracked, "Missing tracking must not create a hand at the origin.");
    }

    static void CheckUiPress(GameObject root)
    {
        var controller = root.AddComponent<ActionBasedController>();
        controller.enableInputTracking = false;
        var cursor = root.AddComponent<XRHandCursor>();
        // Exercise the same synthesized action binding used by hand pinch and
        // controller trigger, without needing a physical headset in the editor.
        typeof(XRHandCursor).GetField("_controller", PrivateInstance).SetValue(cursor, controller);
        typeof(XRHandCursor).GetMethod("EnsureSelect", PrivateInstance).Invoke(cursor, null);
        var device = (InputDevice)typeof(XRHandCursor).GetField("_device", PrivateInstance).GetValue(cursor);
        var action = controller.uiPressAction.action;
        var state = new XRControllerState();
        var update = typeof(ActionBasedController).GetMethod("UpdateInput", PrivateInstance);
        try
        {
            foreach (bool pressed in new[] { false, true, false, true, false })
            {
                InputSystem.QueueStateEvent(device, new SynthesizedSelectState { pressed = pressed ? 1f : 0f });
                InputSystem.Update();
                update.Invoke(controller, new object[] { state });
                Require(state.uiPressInteractionState.active == pressed,
                    "Pinch/trigger must press and release the XRI UI action. Expected=" + pressed +
                    ", phase=" + action.phase + ", controls=" + action.controls.Count);
            }
        }
        finally
        {
            action.Disable();
            controller.selectAction = default;
            controller.activateAction = default;
            controller.uiPressAction = default;
            action.Dispose();
            InputSystem.RemoveDevice(device);
            typeof(XRHandCursor).GetField("_select", PrivateInstance).SetValue(cursor, null);
            typeof(XRHandCursor).GetField("_device", PrivateInstance).SetValue(cursor, null);
        }
    }

    static void CheckReadyIsNotPressed()
    {
        const string layout = "XRScienceCheckHand";
        InputSystem.RegisterLayout<HandInteractionProfile.HandInteraction>(layout);
        var hand = InputSystem.AddDevice<HandInteractionProfile.HandInteraction>(layout);
        try
        {
            InputSystem.AddDeviceUsage(hand, "RightHand");
            SetHandState(hand, 0f);
            var sample = XRPoseReader.Read(true);
            Require(sample.Tracked && !sample.UsingController && !sample.Pinching && sample.Grip == 0f,
                "An open tracked hand being ready must not hold the UI button down.");
            SetHandState(hand, 1f);
            Require(XRPoseReader.Read(true).Pinching, "An actual pinch must press.");
            SetHandState(hand, 0f);
            Require(!XRPoseReader.Read(true).Pinching, "Releasing a pinch must release even while ready remains true.");
        }
        finally
        {
            InputSystem.RemoveDevice(hand);
            InputSystem.RemoveLayout(layout);
        }
    }

    static void SetHandState(HandInteractionProfile.HandInteraction hand, float pinch)
    {
        using (StateEvent.From(hand, out var state))
        {
            hand.isTracked.WriteValueIntoEvent(1f, state);
            hand.pinchReady.WriteValueIntoEvent(1f, state);
            hand.graspReady.WriteValueIntoEvent(1f, state);
            hand.pinchValue.WriteValueIntoEvent(pinch, state);
            InputSystem.QueueEvent(state);
        }
        InputSystem.Update();
    }

    static void CheckMetaHand()
    {
        var hand = InputSystem.AddDevice<MetaAimHand>();
        try
        {
            InputSystem.AddDeviceUsage(hand, "RightHand");
            foreach (float pinch in new[] { 0f, 1f, 0f })
            {
                using (StateEvent.From(hand, out var state))
                {
                    hand.isTracked.WriteValueIntoEvent(1f, state);
                    hand.devicePosition.WriteValueIntoEvent(new Vector3(0.2f, 1.3f, 0.4f), state);
                    hand.deviceRotation.WriteValueIntoEvent(Quaternion.identity, state);
                    hand.aimFlags.WriteValueIntoEvent((int)(MetaAimFlags.Computed | MetaAimFlags.Valid), state);
                    hand.pinchStrengthIndex.WriteValueIntoEvent(pinch, state);
                    InputSystem.QueueEvent(state);
                }
                InputSystem.Update();
                var sample = XRPoseReader.Read(true);
                Require(sample.Tracked && !sample.UsingController && sample.Pinching == (pinch > 0f),
                    "Quest hand aim must remain a hand and use the index pinch instead of a controller trigger.");
            }
        }
        finally
        {
            InputSystem.RemoveDevice(hand);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("XR interaction check failed: " + message);
    }
}
