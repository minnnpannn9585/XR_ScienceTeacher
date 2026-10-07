using UnityEngine;

public class MainMenuInstaller : MonoBehaviour
{
    void Awake()
    {
        if (FindObjectOfType<MainMenuUI>() != null)
        {
            return;
        }

        if (FindObjectOfType<Camera>() == null)
        {
            var cam = new GameObject("MenuCamera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NetFoldTheme.Void;
            cam.gameObject.AddComponent<AudioListener>();
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        if (FindObjectOfType<FeedbackService>() == null)
        {
            new GameObject("Feedback").AddComponent<FeedbackService>();
        }

        var ui = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
        ui.Build();
    }
}
