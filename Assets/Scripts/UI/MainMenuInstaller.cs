using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MainMenuInstaller : MonoBehaviour
{
    void Awake()
    {
        if (FindObjectOfType<MainMenuUI>() != null)
        {
            return;
        }

        XRExperience.ConfigureEventSystem();
        if (FindObjectOfType<FeedbackService>() == null)
        {
            new GameObject("Feedback").AddComponent<FeedbackService>();
        }

        var pcRig = new GameObject("PC Rig");
        var camGo = new GameObject("MenuCamera");
        camGo.transform.SetParent(pcRig.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NetFoldTheme.Void;
        cam.nearClipPlane = 0.05f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();

        var input = gameObject.AddComponent<InputAdapter>();
        input.PcCamera = cam;
        input.PcRig = pcRig;

        XRExperience.Attach(input, transform, new Vector3(0.1f, 0f, -2.4f));
        var ui = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
        ui.Build();
        Debug.Log("[XR Startup] Main menu built.");
    }
}
