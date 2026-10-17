using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>Physical-controller shortcut buttons only. XRI owns every tracked pose and UI action.</summary>
public static class XRPoseReader
{
    public static InputDevice FindController(bool right)
    {
        string usage = right ? "RightHand" : "LeftHand";
        foreach (var device in InputSystem.devices)
        {
            if (!(device is XRController) || device is HandInteractionProfile.HandInteraction) continue;
            var tracked = device.TryGetChildControl<ButtonControl>("isTracked");
            if (tracked == null || !tracked.isPressed) continue;
            foreach (var entry in device.usages)
                if (entry.ToString() == usage) return device;
        }
        return null;
    }

    public static bool WasPressed(InputDevice device, string name)
    {
        var button = device?.TryGetChildControl<ButtonControl>(name);
        return button != null && button.wasPressedThisFrame;
    }
}
