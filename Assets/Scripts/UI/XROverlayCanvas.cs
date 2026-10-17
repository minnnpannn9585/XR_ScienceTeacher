using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Reflows desktop HUD content into two fixed, scene-owned XR desk panels.</summary>
public class XROverlayCanvas : MonoBehaviour
{
    bool _requested;
    bool _applied;
    Canvas _canvas;
    Canvas _rightCanvas;

    void OnEnable()
    {
        XRSession.Ready += Apply;
        Apply();
    }

    void OnDisable() => XRSession.Ready -= Apply;

    public void Apply() => _requested = XRSession.IsActive;

    void LateUpdate()
    {
        if (!_requested || !XRSession.IsActive) return;
        if (!_applied)
        {
            // UI builders finish synchronously after CreateOverlay. Defer until
            // LateUpdate so all panels, including initially hidden results, exist.
            var station = FindObjectOfType<XRDeskStation>();
            if (station == null) return;
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) return;
            ConfigureCanvas(_canvas, station, false);
            var right = new GameObject(name + " Right Desk", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            right.transform.SetParent(transform.parent, false);
            _rightCanvas = right.GetComponent<Canvas>();
            ConfigureCanvas(_rightCanvas, station, true);
            var layout = gameObject.AddComponent<XRDeskUILayout>();
            layout.Build(_canvas, _rightCanvas, station);
            _applied = true;
        }

        // Camera is used only for event raycasting, never for positioning/facing.
        var camera = Camera.main;
        _canvas.worldCamera = camera;
        if (_rightCanvas != null) _rightCanvas.worldCamera = camera;
    }

    static void ConfigureCanvas(Canvas canvas, XRDeskStation station, bool right)
    {
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        var rect = canvas.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = XRDeskStation.PanelPixels;
        station.PlacePanel(rect, right);
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.dynamicPixelsPerUnit = 20f;
        }
        if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        var graphic = canvas.GetComponent<GraphicRaycaster>();
        if (graphic != null) graphic.enabled = false;
    }

    void OnDestroy()
    {
        if (_rightCanvas != null) Destroy(_rightCanvas.gameObject);
    }
}
