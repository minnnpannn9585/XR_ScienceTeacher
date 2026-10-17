using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Observes XR Management startup and switches the scene once its display is running.
/// Headset presence/focus is not a reliable startup or camera-selection signal.
/// </summary>
public class XRSession : MonoBehaviour
{
    public static XRSession Instance { get; private set; }
    public static bool IsActive { get; private set; }
    public static event Action Ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsActive = false;
        Ready = null;
    }

    public static bool HasRunningDisplay
    {
        get
        {
            var settings = XRGeneralSettings.Instance;
            var loader = settings != null && settings.Manager != null ? settings.Manager.activeLoader : null;
            var display = loader != null ? loader.GetLoadedSubsystem<XRDisplaySubsystem>() : null;
            return display != null && display.running;
        }
    }

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
#if UNITY_ANDROID && !UNITY_EDITOR
        ApplyHeadsetBudget();
#endif
    }

    IEnumerator Start()
    {
        // Automatic XR initialization runs before scene load. Only initialize here
        // when automatic startup was explicitly disabled; never race/restart it.
        yield return null;
        var settings = XRGeneralSettings.Instance;
        var manager = settings != null ? settings.Manager : null;
        if (manager != null && !settings.InitManagerOnStart && manager.activeLoader == null)
        {
            yield return manager.InitializeLoader();
            if (manager.activeLoader != null)
            {
                manager.StartSubsystems();
            }
        }

        if (manager == null || manager.activeLoader == null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.LogError("[XR Startup] OpenXR loader failed. Check native loader packaging and the earlier OpenXR diagnostic log.");
#endif
            yield break;
        }

        Debug.Log("[XR Startup] Loader ready: " + manager.activeLoader.name);
        float deadline = Time.realtimeSinceStartup + 10f;
        bool reported = false;
        while (!HasRunningDisplay)
        {
            if (!reported && Time.realtimeSinceStartup >= deadline)
            {
                Debug.LogWarning("[XR Startup] Still waiting for the XR display; will activate when the headset session is ready.");
                reported = true;
            }

            yield return null;
        }

        IsActive = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        ApplyHeadsetBudget();
#endif

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
        Debug.Log("[XR Startup] XR display running; camera, input and world UI activated.");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static void ApplyHeadsetBudget()
    {
        // Quest compositing owns the refresh. vSync here fights 72 Hz and shows up as judder.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 72;
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        for (int i = 0; i < displays.Count; i++)
        {
            if (!displays[i].running)
            {
                continue;
            }

            // About Quest "High" fixed foveation. Gaze foveation stays off.
            displays[i].foveatedRenderingLevel = 0.75f;
        }
    }
#endif
}
