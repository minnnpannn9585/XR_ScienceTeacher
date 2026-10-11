using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Hands;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// One sample of a hand or controller. Controllers win when that side is tracked,
/// otherwise OpenXR hand joints and the hand-interaction profile are used.
/// </summary>
public struct XRHandSample
{
    public bool Tracked;
    public bool UsingController;
    public Vector3 PokePosition;
    public Quaternion PokeRotation;
    public Vector3 AimOrigin;
    public Quaternion AimRotation;
    public Vector3 GripPosition;
    public Quaternion GripRotation;
    public Vector3 ThumbPosition;
    public bool HasThumb;
    public float Trigger;
    public float Grip;
    public bool Pinching;
}

public static class XRPoseReader
{
    static readonly List<XRHandSubsystem> Hands = new List<XRHandSubsystem>();
    static bool _leftPinch;
    static bool _rightPinch;

    public static XRHandSample Read(bool right)
    {
        var sample = new XRHandSample
        {
            PokeRotation = Quaternion.identity,
            AimRotation = Quaternion.identity,
            GripRotation = Quaternion.identity
        };

        InputDevice controller = FindController(right);
        if (controller != null)
        {
            FillController(controller, ref sample);
            return sample;
        }

        bool joints = TryJoints(right, out Pose indexTip, out Pose indexDistal, out Pose thumbTip, out Pose palm);
        InputDevice hand = FindHand(right);
        bool profile = hand != null;
        if (!joints && !profile)
        {
            SetPinch(right, false);
            return sample;
        }

        sample.Tracked = true;
        sample.UsingController = false;
        if (profile)
        {
            if (TryPose(hand, "pokePosition", "pokeRotation", out Pose poke))
            {
                sample.PokePosition = poke.position;
                sample.PokeRotation = poke.rotation;
            }

            if (TryPose(hand, "pointerPosition", "pointerRotation", out Pose aim))
            {
                sample.AimOrigin = aim.position;
                sample.AimRotation = aim.rotation;
            }

            if (TryPose(hand, "devicePosition", "deviceRotation", out Pose grip))
            {
                sample.GripPosition = grip.position;
                sample.GripRotation = grip.rotation;
            }

            sample.Trigger = Mathf.Max(Axis(hand, "pinchValue"), Button(hand, "pinchReady"));
            sample.Grip = Mathf.Max(Axis(hand, "graspValue"), Button(hand, "graspReady"));
        }

        if (joints)
        {
            Vector3 dir = indexTip.position - indexDistal.position;
            Quaternion pokeRot = dir.sqrMagnitude > 0.0000001f
                ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                : indexTip.rotation;
            sample.PokePosition = indexTip.position;
            sample.PokeRotation = pokeRot;
            sample.HasThumb = true;
            sample.ThumbPosition = thumbTip.position;
            if (sample.AimOrigin == Vector3.zero)
            {
                Vector3 origin = Vector3.Lerp(thumbTip.position, indexTip.position, 0.5f);
                Vector3 aimDir = origin - palm.position;
                if (aimDir.sqrMagnitude < 0.0001f)
                {
                    aimDir = pokeRot * Vector3.forward;
                }

                sample.AimOrigin = origin;
                sample.AimRotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
            }

            float gap = Vector3.Distance(indexTip.position, thumbTip.position);
            bool was = PinchLatched(right);
            bool jointPinch = gap < (was ? 0.04f : 0.022f);
            if (jointPinch)
            {
                sample.Trigger = 1f;
            }
        }

        if (sample.PokePosition == Vector3.zero)
        {
            sample.PokePosition = sample.AimOrigin;
            sample.PokeRotation = sample.AimRotation;
        }

        if (sample.AimOrigin == Vector3.zero)
        {
            sample.AimOrigin = sample.PokePosition;
            sample.AimRotation = sample.PokeRotation;
        }

        bool pinching = sample.Trigger > (PinchLatched(right) ? 0.4f : 0.65f);
        SetPinch(right, pinching);
        sample.Pinching = pinching;
        sample.GripPosition = sample.GripPosition == Vector3.zero ? sample.PokePosition : sample.GripPosition;
        sample.GripRotation = sample.GripRotation == Quaternion.identity ? sample.PokeRotation : sample.GripRotation;
        return sample;
    }

    public static InputDevice FindController(bool right)
    {
        string usage = right ? "RightHand" : "LeftHand";
        foreach (var device in InputSystem.devices)
        {
            if (device is HandInteractionProfile.HandInteraction || device.layout.ToString().IndexOf("HandInteraction") >= 0)
            {
                continue;
            }

            if (!HasUsage(device, usage) || !IsTracked(device))
            {
                continue;
            }

            if (device.TryGetChildControl<Vector3Control>("devicePosition") == null &&
                device.TryGetChildControl<ButtonControl>("select") == null &&
                device.TryGetChildControl<AxisControl>("trigger") == null)
            {
                continue;
            }

            return device;
        }

        return null;
    }

    static void FillController(InputDevice device, ref XRHandSample sample)
    {
        sample.Tracked = true;
        sample.UsingController = true;
        sample.Pinching = false;
        if (TryPose(device, "devicePosition", "deviceRotation", out Pose grip))
        {
            sample.GripPosition = grip.position;
            sample.GripRotation = grip.rotation;
        }

        if (!TryPose(device, "pointerPosition", "pointerRotation", out Pose aim))
        {
            aim = new Pose(sample.GripPosition, sample.GripRotation);
        }

        sample.AimOrigin = aim.position;
        sample.AimRotation = aim.rotation;
        sample.PokeRotation = aim.rotation;
        sample.PokePosition = aim.position + (aim.rotation * Vector3.forward) * 0.03f;
        sample.Trigger = Mathf.Max(Axis(device, "trigger"), Button(device, "triggerPressed"), Button(device, "select"));
        sample.Grip = Mathf.Max(Axis(device, "grip"), Button(device, "gripPressed"));
    }

    static InputDevice FindHand(bool right)
    {
        string usage = right ? "RightHand" : "LeftHand";
        foreach (var device in InputSystem.devices)
        {
            if (device is HandInteractionProfile.HandInteraction && HasUsage(device, usage) && IsTracked(device))
            {
                return device;
            }
        }

        return null;
    }

    static bool TryJoints(bool right, out Pose indexTip, out Pose indexDistal, out Pose thumbTip, out Pose palm)
    {
        indexTip = indexDistal = thumbTip = palm = default;
        SubsystemManager.GetSubsystems(Hands);
        for (int i = 0; i < Hands.Count; i++)
        {
            XRHand hand = right ? Hands[i].rightHand : Hands[i].leftHand;
            if (!hand.isTracked)
            {
                continue;
            }

            bool tip = hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out indexTip);
            if (!hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out indexDistal))
            {
                indexDistal = indexTip;
            }

            hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out thumbTip);
            if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out palm))
            {
                palm = indexDistal;
            }

            return tip;
        }

        return false;
    }

    static bool TryPose(InputDevice device, string position, string rotation, out Pose pose)
    {
        pose = default;
        var pos = device.TryGetChildControl<Vector3Control>(position);
        var rot = device.TryGetChildControl<QuaternionControl>(rotation);
        if (pos == null || rot == null)
        {
            return false;
        }

        pose.position = pos.ReadValue();
        pose.rotation = rot.ReadValue();
        if (pose.rotation == default)
        {
            pose.rotation = Quaternion.identity;
        }

        return pose.position.sqrMagnitude > 0.0000001f || pose.rotation != Quaternion.identity;
    }

    static float Axis(InputDevice device, string name)
    {
        var axis = device.TryGetChildControl<AxisControl>(name);
        return axis != null ? axis.ReadValue() : 0f;
    }

    static float Button(InputDevice device, string name)
    {
        var button = device.TryGetChildControl<ButtonControl>(name);
        return button != null && button.isPressed ? 1f : 0f;
    }

    public static bool WasPressed(InputDevice device, string name)
    {
        if (device == null)
        {
            return false;
        }

        var button = device.TryGetChildControl<ButtonControl>(name);
        return button != null && button.wasPressedThisFrame;
    }

    static bool HasUsage(InputDevice device, string usage)
    {
        foreach (var entry in device.usages)
        {
            if (entry.ToString() == usage)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsTracked(InputDevice device)
    {
        var tracked = device.TryGetChildControl<ButtonControl>("isTracked");
        if (tracked != null)
        {
            return tracked.isPressed;
        }

        var position = device.TryGetChildControl<Vector3Control>("devicePosition");
        return position != null && position.ReadValue().sqrMagnitude > 0.0001f;
    }

    static bool PinchLatched(bool right)
    {
        return right ? _rightPinch : _leftPinch;
    }

    static void SetPinch(bool right, bool value)
    {
        if (right)
        {
            _rightPinch = value;
        }
        else
        {
            _leftPinch = value;
        }
    }
}
