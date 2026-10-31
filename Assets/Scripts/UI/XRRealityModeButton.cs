using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>A scene-owned mode switch outside lesson, settings and result modal groups.</summary>
public sealed class XRRealityModeButton : MonoBehaviour
{
    Canvas _canvas;
    TrackedDeviceGraphicRaycaster _raycaster;
    Button _button;
    TMP_Text _label;
    TMP_Text _status;
    XRRealityMode _mode;

    public static void Attach(XRDeskStation station, Camera camera)
    {
        if (station == null || station.GetComponentInChildren<XRRealityModeButton>(true) != null) return;

        const float height = 140f;
        var canvas = UiFactory.CreateWorld("VR MR Switch", station.transform, Vector3.zero,
            new Vector2(XRDeskStation.PanelPixels.x, height), Vector3.zero);
        var rect = canvas.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        station.PlacePanel(rect, true);
        // Match the right panel's plane, leaving a gap above it. The control is
        // deliberately outside its CanvasGroup and all full-panel blockers.
        rect.position += rect.up * ((XRDeskStation.PanelPixels.y + height) * 0.5f + 16f) * XRDeskStation.PanelScale;
        canvas.worldCamera = camera;
        canvas.sortingOrder = 20;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 20f;

        UiFactory.Panel(rect, "ModeSurface", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.055f, 0.065f, 0.08f, 0.98f));
        var button = UiFactory.Button(rect, "SwitchMode", "", Vector2.zero, Vector2.one,
            new Vector2(24f, 54f), new Vector2(-24f, -14f), () => XRRealityMode.Instance?.Toggle());
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var status = UiFactory.Label(rect, "ModeStatus", "", 23, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 8f), new Vector2(-24f, 48f), NetFoldTheme.TextDim);
        status.enableAutoSizing = true;
        status.fontSizeMin = 19f;
        status.fontSizeMax = 23f;

        var view = canvas.gameObject.AddComponent<XRRealityModeButton>();
        view._canvas = canvas;
        view._raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        view._button = button;
        view._label = button.GetComponentInChildren<TMP_Text>();
        view._status = status;
        view.Refresh();
    }

    void OnEnable()
    {
        Loc.Changed += Refresh;
        XRSession.Ready += Refresh;
        BindMode();
        Refresh();
    }

    void OnDisable()
    {
        Loc.Changed -= Refresh;
        XRSession.Ready -= Refresh;
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = null;
    }

    void LateUpdate()
    {
        if (_mode == XRRealityMode.Instance) return;
        BindMode();
        Refresh();
    }

    void BindMode()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = XRRealityMode.Instance;
        if (_mode != null) _mode.Changed += Refresh;
    }

    void Refresh()
    {
        if (_canvas == null) return;
        // Keep the component alive before XR startup so it can receive Ready.
        _canvas.enabled = XRSession.IsActive;
        _raycaster.enabled = XRSession.IsActive;
        bool mixed = _mode != null && _mode.IsMixedReality;
        bool available = _mode != null && _mode.IsAvailable;
        _button.interactable = available || mixed;
        _label.text = Loc.Get(mixed ? "xr.switchToVR" : "xr.switchToMR");
        _status.text = Loc.Get(_mode != null && _mode.HasError ? "xr.modeError" :
            !available && !mixed ? "xr.modeUnavailable" : mixed ? "xr.modeMR" : "xr.modeVR");
        UiFactory.SetButtonColor(_button, mixed ? NetFoldTheme.Accent : NetFoldTheme.AccentDeep);
    }
}
