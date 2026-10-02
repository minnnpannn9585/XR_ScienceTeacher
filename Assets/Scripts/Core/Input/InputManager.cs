using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    InputAdapter _adapter;

    public IInputProvider Provider => _adapter != null ? _adapter.Provider : null;

    public event Action BackRequested;
    public event Action ConfirmRequested;
    public event Action ResetRequested;
    public event Action ViewToggled;
    public event Action RayToggled;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Bind(InputAdapter adapter)
    {
        _adapter = adapter;
    }

    void LateUpdate()
    {
        IInputProvider provider = Provider;
        if (provider == null)
        {
            return;
        }

        if (provider.BackPressed)
        {
            BackRequested?.Invoke();
        }

        if (provider.ConfirmPressed)
        {
            ConfirmRequested?.Invoke();
        }

        if (provider.ResetPressed)
        {
            ResetRequested?.Invoke();
        }

        if (provider.ViewTogglePressed)
        {
            ViewToggled?.Invoke();
        }

        if (provider.RayTogglePressed)
        {
            RayToggled?.Invoke();
        }
    }
}
