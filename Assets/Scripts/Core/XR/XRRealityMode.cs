using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;

/// <summary>Session-wide VR/MR choice. Scene content and experiment state stay alive.</summary>
public sealed class XRRealityMode : MonoBehaviour
{
    public static XRRealityMode Instance { get; private set; }
    public bool IsMixedReality { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool HasError { get; private set; }
    public event Action Changed;

    QuestPassthroughFeature _feature;
    bool _requestedMixedReality;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (_feature == null && OpenXRSettings.Instance != null)
            _feature = OpenXRSettings.Instance.GetFeature<QuestPassthroughFeature>();

        bool available = XRSession.IsActive && _feature != null && _feature.IsSupported;
        // Session stop/loss temporarily restores opaque VR. Resume the user's
        // choice only after OpenXR has a usable session again.
        if (available && _requestedMixedReality &&
            (!IsAvailable || (!_feature.IsRunning && !HasError)))
            HasError = !_feature.TrySetPassthrough(true);

        if (!available && IsAvailable && _requestedMixedReality)
        {
            HasError = !string.IsNullOrEmpty(_feature.LastError);
            // A rejected composition stops submission immediately on the render
            // thread; release its active passthrough work here on the main thread.
            _feature.TrySetPassthrough(false);
        }

        bool mixed = available && _requestedMixedReality && _feature.IsRunning;
        if (IsAvailable == available && IsMixedReality == mixed) return;
        IsAvailable = available;
        IsMixedReality = mixed;
        Changed?.Invoke();
    }

    public void Toggle() => SetMixedReality(!IsMixedReality);

    public bool SetMixedReality(bool mixedReality)
    {
        if (mixedReality && (!IsAvailable || !_feature.TrySetPassthrough(true)))
        {
            HasError = true;
            Changed?.Invoke();
            return false;
        }

        if (!mixedReality && _feature != null) _feature.TrySetPassthrough(false);
        _requestedMixedReality = mixedReality;
        // Never clear the camera or hide the room until native passthrough starts.
        IsMixedReality = mixedReality && _feature != null && _feature.IsRunning;
        HasError = mixedReality && !IsMixedReality;
        Changed?.Invoke();
        return !HasError;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        if (_feature != null) _feature.TrySetPassthrough(false);
        Instance = null;
        Changed = null;
    }
}
