using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>Scene-owned desk coordinates. Only entry/recenter moves the whole XR origin.</summary>
public class XRDeskStation : MonoBehaviour
{
    public const float SurfaceHeight = 0.86f;
    public const float TopThickness = 0.045f;
    public const float FrontClearance = 0.28f;
    public const float PanelScale = 0.00085f;
    public static readonly Vector2 PanelPixels = new Vector2(600f, 740f);

    public Vector3 StandingPosition { get; private set; }
    public Vector3 SurfaceCenter { get; private set; }
    XROrigin _origin;
    bool _placed;
    int _trackedFrames;

    public void Configure(XROrigin origin, Vector3 surfaceCenter, float deskDepth)
    {
        _origin = origin;
        SurfaceCenter = surfaceCenter;
        StandingPosition = new Vector3(surfaceCenter.x, 0f, surfaceCenter.z - deskDepth * 0.5f - FrontClearance);
    }

    void LateUpdate()
    {
        if (_placed || !XRSession.IsActive || _origin == null || !_origin.gameObject.activeInHierarchy) return;
        var hmd = InputSystem.GetDevice<XRHMD>();
        if (hmd == null || !hmd.isTracked.isPressed)
        {
            _trackedFrames = 0;
            return;
        }

        // Let the official pose driver and floor-origin initialization receive
        // tracking before correcting the physical room's initial X/Z offset.
        if (++_trackedFrames >= 2) ReturnToDesk();
    }

    public void ReturnToDesk()
    {
        if (_origin == null || _origin.Camera == null || !_origin.gameObject.activeInHierarchy) return;
        var head = _origin.Camera.transform;
        var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f) return;
        _origin.RotateAroundCameraUsingOriginUp(Vector3.SignedAngle(forward, Vector3.forward, Vector3.up));
        // Preserve floor-relative eye height and all tracked hand/head offsets.
        // Never clamp, scale, or continuously move the tracked camera itself.
        _origin.MoveCameraToWorldLocation(new Vector3(StandingPosition.x, head.position.y, StandingPosition.z));
        _placed = true;
    }

    public void PlacePanel(Transform panel, bool right)
    {
        float side = right ? 1f : -1f;
        panel.position = StandingPosition + new Vector3(side * 0.48f, SurfaceCenter.y + 0.32f, 0.46f);
        panel.rotation = Quaternion.LookRotation(new Vector3(side * 0.48f, -0.30f, 0.46f), Vector3.up);
        // Panels remain in these scene coordinates when the player looks away.
        panel.localScale = Vector3.one * PanelScale;
    }
}
