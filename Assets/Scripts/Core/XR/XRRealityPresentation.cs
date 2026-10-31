using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Restorable scene presentation; never disables experiment roots, rigs or lighting.</summary>
public sealed class XRRealityPresentation : MonoBehaviour
{
    Camera _camera;
    UniversalAdditionalCameraData _cameraData;
    CameraClearFlags _clearFlags;
    Color _background;
    bool _hdr;
    bool _postProcessing;
    AntialiasingMode _antialiasing;
    Renderer[] _renderers;
    Collider[] _colliders;
    Volume[] _volumes;
    bool[] _rendererStates;
    bool[] _colliderStates;
    bool[] _volumeStates;
    bool _ownsFog;
    bool _fog;
    bool _configured;
    bool _mixed;
    XRRealityMode _mode;

    public void Configure(Camera camera, Transform environment)
    {
        if (_configured) return;
        _camera = camera;
        if (_camera != null)
        {
            _clearFlags = _camera.clearFlags;
            _background = _camera.backgroundColor;
            _hdr = _camera.allowHDR;
            _cameraData = _camera.GetComponent<UniversalAdditionalCameraData>();
            if (_cameraData != null)
            {
                _postProcessing = _cameraData.renderPostProcessing;
                _antialiasing = _cameraData.antialiasing;
            }
        }
        if (environment != null)
        {
            _ownsFog = true;
            _fog = RenderSettings.fog;
            _renderers = environment.GetComponentsInChildren<Renderer>(true);
            _colliders = environment.GetComponentsInChildren<Collider>(true);
            _volumes = environment.GetComponentsInChildren<Volume>(true);
            _rendererStates = new bool[_renderers.Length];
            _colliderStates = new bool[_colliders.Length];
            _volumeStates = new bool[_volumes.Length];
            for (int i = 0; i < _renderers.Length; i++) _rendererStates[i] = _renderers[i].enabled;
            for (int i = 0; i < _colliders.Length; i++) _colliderStates[i] = _colliders[i].enabled;
            for (int i = 0; i < _volumes.Length; i++) _volumeStates[i] = _volumes[i].enabled;
        }
        _configured = true;
        if (isActiveAndEnabled) Subscribe();
    }

    void OnEnable()
    {
        if (_configured) Subscribe();
    }

    void Subscribe()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = XRRealityMode.Instance;
        if (_mode != null) _mode.Changed += Refresh;
        Refresh();
    }

    void Refresh() => Apply(_mode != null && _mode.IsMixedReality);

    // This changes only presentation, making it independently checkable without
    // pretending the Editor can display a Quest's real-world camera feed.
    public void Apply(bool mixedReality)
    {
        if (!_configured || _mixed == mixedReality) return;
        _mixed = mixedReality;
        if (_camera != null)
        {
            _camera.clearFlags = mixedReality ? CameraClearFlags.SolidColor : _clearFlags;
            _camera.backgroundColor = mixedReality ? Color.clear : _background;
            // URP 14 post-processing/AA can overwrite the alpha needed by the compositor.
            _camera.allowHDR = mixedReality ? false : _hdr;
            if (_cameraData != null)
            {
                _cameraData.renderPostProcessing = mixedReality ? false : _postProcessing;
                _cameraData.antialiasing = mixedReality ? AntialiasingMode.None : _antialiasing;
            }
        }
        if (!_ownsFog) return;
        RenderSettings.fog = mixedReality ? false : _fog;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].enabled = !mixedReality && _rendererStates[i];
        for (int i = 0; i < _colliders.Length; i++)
            if (_colliders[i] != null) _colliders[i].enabled = !mixedReality && _colliderStates[i];
        for (int i = 0; i < _volumes.Length; i++)
            if (_volumes[i] != null) _volumes[i].enabled = !mixedReality && _volumeStates[i];
    }

    void OnDisable()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = null;
        Apply(false);
    }
}
