using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>Checks the configured official rig and real XRI action bindings, including separate aim and grip poses.</summary>
public static class XRInteractionChecks
{
    [MenuItem("XR/Run Interaction Checks")]
    public static void Run()
    {
        Require(!EditorApplication.isPlaying, "Run outside Play Mode.");
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>(OfficialXRSetup.RigPath);
        Require(rig != null, "Prepare the official interaction rig first.");
        Require(!rig.activeSelf, "Rig must wait for InputAdapter to activate it.");
        foreach (var transform in rig.GetComponentsInChildren<Transform>(true))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
        var modality = rig.GetComponent<XRInputModalityManager>();
        Require(modality != null && modality.leftHand != null && modality.rightHand != null &&
            modality.leftController != null && modality.rightController != null, "Official hand/controller modality references.");
        Require(rig.GetComponentsInChildren<XRHandCursor>(true).Length == 4, "Both hands and both controllers need experiment bridges.");
        Require(rig.GetComponentsInChildren<XRControllerHandAnimation>(true).Length == 2, "Both supplied Oculus models need animation bindings.");
        Require(rig.GetComponentsInChildren<XRHandSkeletonDriver>(true).Length >= 2, "Official tracked-hand skeletons.");
        Require(rig.GetComponentsInChildren<LocomotionProvider>(true).All(provider => !provider.enabled), "Locomotion must stay disabled.");
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(OfficialXRSetup.ActionsPath);
        Require(rig.GetComponent<InputActionManager>().actionAssets.Contains(actions), "The official input manager must enable the project actions.");
        foreach (var binding in actions.bindings)
        {
            string path = binding.path.ToLowerInvariant();
            Require(!path.Contains("thumbstick") && !path.Contains("primary2daxis") && !path.Contains("secondary2daxis") && !path.Contains("joystick"),
                "No joystick bindings: " + binding.path);
        }
        foreach (var cursor in rig.GetComponentsInChildren<XRHandCursor>(true))
        {
            var ray = cursor.RayInteractor;
            Require(ray != null && ray.enableUIInteraction && ray.raycastMask != 0, "Official UI ray must hit graphics.");
            Require(!ray.allowAnchorControl, "Object rotation/anchor controls must stay disabled.");
            Require(ray.rayOriginTransform != null && ray.rayOriginTransform.GetComponent<XRTransformStabilizer>() != null,
                "Official stabilized ray origin is required.");
        }

        var previousSettings = InputSystem.settings;
        var testSettings = UnityEngine.Object.Instantiate(previousSettings);
        try
        {
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = testSettings;
            actions.Enable();
            CheckBindings(rig, false);
            CheckBindings(rig, true);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/xr-interaction-checks.txt", "PASS: official rig dependencies, four modalities, Oculus models, XR Hands skeletons, no joystick/locomotion/anchor control, stabilized rays, separate OpenXR aim/grip, controller UI press/release, Meta hand aim and pinch.\n");
            Debug.Log("XR_INTERACTION_CHECKS_PASSED");
        }
        finally
        {
            actions.Disable();
            InputSystem.settings = previousSettings;
            UnityEngine.Object.DestroyImmediate(testSettings);
        }
    }

    static void CheckBindings(GameObject rig, bool right)
    {
        string side = right ? "Right" : "Left";
        string usage = side + "Hand";
        var bridge = rig.GetComponentsInChildren<XRHandCursor>(true).Single(cursor => cursor.Right == right && cursor.UsingController);
        var controller = bridge.GetComponentInChildren<ActionBasedController>(true);
        var gripDriver = bridge.GetComponentsInChildren<TrackedPoseDriver>(true).Single(driver => driver.name == side + " Controller Grip");
        const string layout = "XRScienceTestTouch";
        InputSystem.RegisterLayout<OculusTouchControllerProfile.OculusTouchController>(layout);
        var device = InputSystem.AddDevice<OculusTouchControllerProfile.OculusTouchController>();
        try
        {
            InputSystem.AddDeviceUsage(device, usage);
            var aim = new Vector3(0.2f, 1.3f, 0.5f);
            var aimRotation = Quaternion.Euler(18f, 27f, 0f);
            var grip = new Vector3(0.18f, 1.25f, 0.45f);
            foreach (bool pressed in new[] { false, true, false })
            {
                using (StateEvent.From(device, out var state))
                {
                    device.isTracked.WriteValueIntoEvent(1f, state);
                    device.trackingState.WriteValueIntoEvent(3, state);
                    device.devicePosition.WriteValueIntoEvent(grip, state);
                    device.deviceRotation.WriteValueIntoEvent(Quaternion.identity, state);
                    device.pointerPosition.WriteValueIntoEvent(aim, state);
                    device.pointerRotation.WriteValueIntoEvent(aimRotation, state);
                    device.triggerPressed.WriteValueIntoEvent(pressed ? 1f : 0f, state);
                    device.trigger.WriteValueIntoEvent(pressed ? 1f : 0f, state);
                    InputSystem.QueueEvent(state);
                }
                InputSystem.Update();
                Require(Vector3.Distance(controller.positionAction.action.ReadValue<Vector3>(), aim) < 0.0001f, side + " ray must use native aim position.");
                Require(Quaternion.Angle(controller.rotationAction.action.ReadValue<Quaternion>(), aimRotation) < 0.01f, side + " ray must use native aim rotation.");
                Require(Vector3.Distance(gripDriver.positionInput.action.ReadValue<Vector3>(), grip) < 0.0001f, side + " model must use grip position independently.");
                Require(controller.uiPressAction.action.IsPressed() == pressed, side + " controller UI press/release.");
            }
        }
        finally
        {
            InputSystem.RemoveDevice(device);
            InputSystem.RemoveLayout(layout);
        }

        var handBridge = rig.GetComponentsInChildren<XRHandCursor>(true).Single(cursor => cursor.Right == right && !cursor.UsingController);
        var handController = handBridge.RayInteractor.GetComponent<ActionBasedController>();
        var hand = InputSystem.AddDevice<MetaAimHand>();
        try
        {
            InputSystem.AddDeviceUsage(hand, usage);
            foreach (bool pinched in new[] { false, true, false })
            {
                var aim = new Vector3(0.25f, 1.2f, 0.45f);
                var rotation = Quaternion.Euler(-12f, 16f, 0f);
                using (StateEvent.From(hand, out var state))
                {
                    hand.isTracked.WriteValueIntoEvent(1f, state);
                    hand.devicePosition.WriteValueIntoEvent(aim, state);
                    hand.deviceRotation.WriteValueIntoEvent(rotation, state);
                    hand.indexPressed.WriteValueIntoEvent(pinched ? 1f : 0f, state);
                    hand.pinchStrengthIndex.WriteValueIntoEvent(pinched ? 1f : 0f, state);
                    InputSystem.QueueEvent(state);
                }
                InputSystem.Update();
                Require(Vector3.Distance(handController.positionAction.action.ReadValue<Vector3>(), aim) < 0.0001f, side + " hand uses native aim position.");
                Require(Quaternion.Angle(handController.rotationAction.action.ReadValue<Quaternion>(), rotation) < 0.01f, side + " hand uses native aim rotation.");
                Require(handController.uiPressAction.action.IsPressed() == pinched, side + " hand UI pinch/release.");
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
