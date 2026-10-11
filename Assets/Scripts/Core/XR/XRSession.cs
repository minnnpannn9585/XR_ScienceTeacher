using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Starts the OpenXR loader and tells the labs when a headset is actually running.
/// Detection in Awake is too early: the loader finishes a frame or two later.
/// </summary>
public class XRSession : MonoBehaviour
{
    public static XRSession Instance { get; private set; }
    public static bool IsActive { get; private set; }
    public static event Action Ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        var go = new GameObject("XR Session");
        DontDestroyOnLoad(go);
        go.AddComponent<XRSession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    IEnumerator Start()
    {
        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        float waited = 0f;
        while (manager != null && !manager.isInitializationComplete && waited < 5f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (manager != null && manager.activeLoader == null && !manager.isInitializationComplete)
        {
            yield return manager.InitializeLoader();
        }

        if (manager != null && manager.activeLoader != null)
        {
            manager.StartSubsystems();
        }

        waited = 0f;
        while (!XRSettings.isDeviceActive && waited < 2f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        IsActive = XRSettings.isDeviceActive;
        if (!IsActive)
        {
            yield break;
        }

        XRExperience.ConfigureEventSystem();
        var adapters = FindObjectsOfType<InputAdapter>(true);
        for (int i = 0; i < adapters.Length; i++)
        {
            adapters[i].ApplyPlatform();
        }

        var canvases = FindObjectsOfType<XROverlayCanvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].Apply();
        }

        if (GetComponent<XRNumericPad>() == null)
        {
            gameObject.AddComponent<XRNumericPad>();
        }

        Ready?.Invoke();
    }
}
