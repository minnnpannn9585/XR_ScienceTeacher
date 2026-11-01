using UnityEngine;
using UnityEngine.UI;

/// <summary>Readable translucent slate in passthrough, retaining the original desktop/VR theme.</summary>
public sealed class MRPanelStyle : MonoBehaviour
{
    Image _panel, _hairline;
    Color _original, _originalLine;
    XRRealityMode _mode;
    bool _configured;

    public void Configure(Image panel, Image hairline)
    {
        _panel = panel;
        _hairline = hairline;
        _original = panel.color;
        if (hairline != null) _originalLine = hairline.color;
        _configured = true;
        if (isActiveAndEnabled) Subscribe();
    }

    void OnEnable() { if (_configured) Subscribe(); }
    void Start() { if (_configured) Subscribe(); }
    void Subscribe()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = XRRealityMode.Instance;
        if (_mode != null) _mode.Changed += Refresh;
        Refresh();
    }
    void Refresh() => Apply(_mode != null && _mode.IsMixedReality);

    public void Apply(bool mixed)
    {
        if (!_configured) return;
        if (_panel != null) _panel.color = mixed
            ? new Color(0.075f, 0.15f, 0.18f, Mathf.Clamp(_original.a * 0.83f, 0.62f, 0.8f)) : _original;
        if (_hairline != null) _hairline.color = mixed ? MRVisualStyle.Cyan : _originalLine;
    }

    void OnDisable()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = null;
        Apply(false);
    }
}
