using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A neutral fill for virtual teaching objects; this is not real-world light estimation.</summary>
public sealed class MRStudioLighting : MonoBehaviour
{
    Light[] _lights;
    float[] _intensities;
    Color[] _colors;
    AmbientMode _ambientMode;
    Color _sky, _equator, _ground;
    bool _configured, _mixed;
    XRRealityMode _mode;

    public void Configure()
    {
        if (_configured) return;
        _lights = GetComponentsInChildren<Light>(true);
        _intensities = new float[_lights.Length];
        _colors = new Color[_lights.Length];
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
        if (!_configured || _mixed == mixed) return;
        _mixed = mixed;
        if (mixed)
        {
            _ambientMode = RenderSettings.ambientMode;
            _sky = RenderSettings.ambientSkyColor;
            _equator = RenderSettings.ambientEquatorColor;
            _ground = RenderSettings.ambientGroundColor;
        }
        RenderSettings.ambientMode = mixed ? AmbientMode.Trilight : _ambientMode;
        RenderSettings.ambientSkyColor = mixed ? new Color(0.57f, 0.64f, 0.67f) : _sky;
        RenderSettings.ambientEquatorColor = mixed ? new Color(0.35f, 0.41f, 0.43f) : _equator;
        RenderSettings.ambientGroundColor = mixed ? new Color(0.18f, 0.20f, 0.21f) : _ground;
        for (int i = 0; i < _lights.Length; i++)
        {
            var light = _lights[i];
            if (light == null) continue;
            if (mixed)
            {
                _intensities[i] = light.intensity;
                _colors[i] = light.color;
                // Remove the warm pool that belongs to the virtual studio.
                light.intensity = light.type == LightType.Directional ? _intensities[i] * 0.58f : 0f;
                light.color = Color.Lerp(_colors[i], Color.white, 0.6f);
            }
            else
            {
                light.intensity = _intensities[i];
                light.color = _colors[i];
            }
        }
    }

    void OnDisable()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = null;
        Apply(false);
    }
}
