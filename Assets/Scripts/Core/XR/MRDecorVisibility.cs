using UnityEngine;

/// <summary>Removes only registered virtual furniture/backplates in MR, with exact restoration.</summary>
public sealed class MRDecorVisibility : MonoBehaviour
{
    Renderer[] _renderers;
    Collider[] _colliders;
    bool[] _rendererStates, _colliderStates;
    bool _configured, _mixed;
    XRRealityMode _mode;

    public void Configure()
    {
        if (_configured) return;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _rendererStates = new bool[_renderers.Length];
        _colliderStates = new bool[_colliders.Length];
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
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            if (mixed) _rendererStates[i] = _renderers[i].enabled;
            _renderers[i].enabled = !mixed && _rendererStates[i];
        }
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] == null) continue;
            if (mixed) _colliderStates[i] = _colliders[i].enabled;
            _colliders[i].enabled = !mixed && _colliderStates[i];
        }
    }

    void OnDisable()
    {
        if (_mode != null) _mode.Changed -= Refresh;
        _mode = null;
        Apply(false);
    }
}
