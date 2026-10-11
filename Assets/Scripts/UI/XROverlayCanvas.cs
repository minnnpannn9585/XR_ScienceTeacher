using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Screen-space menus become a world panel in a headset so the same layout can be
/// poked up close and aimed at from a pinch or trigger ray.
/// </summary>
public class XROverlayCanvas : MonoBehaviour
{
    bool _applied;

    void OnEnable()
    {
        XRSession.Ready += Apply;
        Apply();
    }

    void OnDisable()
    {
        XRSession.Ready -= Apply;
    }

    public void Apply()
    {
        if (_applied || !XRSession.IsActive)
        {
            return;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        transform.localScale = Vector3.one * 0.00058f;
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
        }

        if (GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        var graphic = GetComponent<GraphicRaycaster>();
        if (graphic != null)
        {
            graphic.enabled = false;
        }

        if (GetComponent<XRPanelFollow>() == null)
        {
            gameObject.AddComponent<XRPanelFollow>();
        }

        _applied = true;
    }

    void LateUpdate()
    {
        if (!_applied || !XRSession.IsActive)
        {
            return;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas.worldCamera == null || !canvas.worldCamera.isActiveAndEnabled)
        {
            canvas.worldCamera = Camera.main;
        }
    }
}

public class XRPanelFollow : MonoBehaviour
{
    public static bool Hold;

    const float Distance = 0.78f;
    const float Drop = 0.08f;
    Transform _head;
    bool _placed;

    void LateUpdate()
    {
        bool freeze = Hold;
        Hold = false;
        if (freeze && _placed)
        {
            return;
        }

        if (_head == null || !_head.gameObject.activeInHierarchy)
        {
            var cam = Camera.main;
            _head = cam != null ? cam.transform : null;
        }

        if (_head == null)
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = _head.forward;
        }

        forward.Normalize();
        Vector3 desired = _head.position + forward * Distance + Vector3.up * -Drop;
        Vector3 toPanel = transform.position - _head.position;
        float angle = _placed ? Vector3.Angle(forward, Vector3.ProjectOnPlane(toPanel, Vector3.up)) : 180f;
        bool behind = _placed && Vector3.Dot(forward, toPanel) < 0.05f;
        if (!_placed || behind || angle > 55f)
        {
            transform.position = desired;
            _placed = true;
        }
        else if (angle > 28f || Mathf.Abs(toPanel.magnitude - Distance) > 0.28f)
        {
            transform.position = Vector3.Lerp(transform.position, desired, 0.14f);
        }

        Vector3 facing = transform.position - _head.position;
        if (facing.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }
    }
}
