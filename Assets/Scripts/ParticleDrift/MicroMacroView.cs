using System;
using DG.Tweening;
using UnityEngine;

public class MicroMacroView : MonoBehaviour
{
    public bool IsMicro { get; private set; }
    public event Action<bool> Changed;

    PCOrbitCamera _orbit;

    public void Bind(PCOrbitCamera orbit)
    {
        _orbit = orbit;
        if (_orbit != null)
        {
            _orbit.MinDistance = 0.42f;
            _orbit.MaxDistance = 3.6f;
            _orbit.Distance = 1.42f;
            _orbit.Pitch = 32f;
            _orbit.Yaw = 26f;
            _orbit.TargetOffset = new Vector3(0f, 0.08f, 0f);
        }
    }

    public void Toggle()
    {
        Set(!IsMicro);
    }

    public void Set(bool micro)
    {
        if (IsMicro == micro)
        {
            Changed?.Invoke(IsMicro);
            return;
        }

        IsMicro = micro;
        if (_orbit != null)
        {
            DOTween.Kill(this);
            float distance = micro ? 0.62f : 1.42f;
            float pitch = micro ? 42f : 32f;
            DOTween.To(() => _orbit.Distance, value => _orbit.Distance = value, distance, 0.65f)
                .SetEase(Ease.InOutCubic)
                .SetTarget(this);
            DOTween.To(() => _orbit.Pitch, value => _orbit.Pitch = value, pitch, 0.65f)
                .SetEase(Ease.InOutCubic)
                .SetTarget(this);
        }

        Changed?.Invoke(IsMicro);
    }

    void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
