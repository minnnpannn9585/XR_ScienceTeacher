using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Run with -batchmode -executeMethod MiniLabSmokeChecks.Run (without -quit).</summary>
[InitializeOnLoad]
public static class MiniLabSmokeChecks
{
    const string ActiveKey = "MiniLabSmokeChecks.Active";
    static int _stage, _frame;
    static float _notBefore;
    static bool _failed;
    static string _locale;

    static MiniLabSmokeChecks()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
    }

    [MenuItem("Tools/Science Studio/Run Mini Lab Smoke Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run from Edit Mode.");
        foreach (string name in new[] { "TriangleAngles", "LeverBalance", "IndicatorLab" })
            Require(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/" + name + ".unity"), name + " is enabled for builds");
        XRInteractionChecks.Run();
        Directory.CreateDirectory("Logs/MiniLabs");
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString("MiniLabSmokeChecks.Locale", Loc.PreferredCode());
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!Application.isPlaying || _failed || Time.frameCount < _frame + 35 || Time.time < _notBefore) return;
        _frame = Time.frameCount;
        try
        {
            switch (_stage++)
            {
                case 0:
                    _locale = SessionState.GetString("MiniLabSmokeChecks.Locale", Loc.Chinese);
                    Loc.SetLocale(Loc.Chinese);
                    CheckMenu();
                    ScreenCapture.CaptureScreenshot("Logs/MiniLabs/menu.png");
                    break;
                case 1:
                    SceneLoader.LoadTriangleAngles();
                    break;
                case 2:
                    CheckTriangle();
                    ScreenCapture.CaptureScreenshot("Logs/MiniLabs/triangle.png");
                    break;
                case 3:
                    CheckXRPanel();
                    SceneLoader.LoadLeverBalance();
                    break;
                case 4:
                    CheckLever();
                    CaptureAfterSeconds("Logs/MiniLabs/lever.png", 1f);
                    _notBefore = Time.time + 1.5f;
                    break;
                case 5:
                    var beam = UnityEngine.Object.FindObjectOfType<LeverBalanceLab>().GetComponentsInChildren<Transform>().Single(t => t.name == "Lever Beam");
                    Require(Quaternion.Angle(beam.localRotation, Quaternion.identity) < 0.5f, "Balanced lever settles horizontally");
                    CheckXRPanel();
                    SceneLoader.LoadIndicatorLab();
                    break;
                case 6:
                    CheckIndicator();
                    ScreenCapture.CaptureScreenshot("Logs/MiniLabs/indicator.png");
                    break;
                case 7:
                    CheckXRPanel();
                    Loc.SetLocale(Loc.English);
                    Require(UnityEngine.Object.FindObjectOfType<MiniLabBase>().GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("TRY IT")), "English labels update live");
                    SceneLoader.LoadMainMenu();
                    break;
                case 8:
                    CheckXRMenu();
                    Finish(true, "PASS: 13 build scenes; menu hierarchy and back navigation; triangle angle invariants, dragging, completion/reset; queued PC mouse events (rapid drag, same-frame release, UI-origin gating, release over UI) and Escape navigation event; lever torque and bounds; indicator samples/completion/reset; live English labels; all new XR canvases/raycasters; official controller and hand action bindings. Hardware headset verification remains manual.");
                    break;
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    static void CheckMenu()
    {
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        Require(menu != null && menu.CurrentSubjectIndex == -1, "Menu starts on subjects");
        var home = menu.GetComponentsInChildren<Transform>(true).Single(t => t.name == "HomePage");
        Require(home.GetComponentsInChildren<Button>().Length == 3, "Homepage has exactly three subject entries");
        for (int i = 0; i < 3; i++)
        {
            menu.ShowSubject(i);
            Require(menu.CurrentSubjectIndex == i && !menu.ShowingModes, "Subject selection opens lessons");
            var page = menu.GetComponentsInChildren<Transform>(true).Single(t => t.name == "SubjectPage");
            Require(page.GetComponentsInChildren<Button>().Length == 2, "Each subject offers two lessons");
            menu.ShowModes();
            Require(menu.ShowingModes, "Existing lesson exposes modes");
            var modes = menu.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ModesPage");
            Require(modes.GetComponentsInChildren<Button>().Length == 3, "All existing modes retained");
            menu.NavigateBack();
            Require(!menu.ShowingModes && menu.CurrentSubjectIndex == i, "Back from modes keeps subject");
            menu.NavigateBack();
            Require(menu.CurrentSubjectIndex == -1, "Back from subject restores home");
        }
    }

    static void CheckTriangle()
    {
        var lab = UnityEngine.Object.FindObjectOfType<TriangleAnglesLab>();
        Require(lab != null && lab.IsBuilt && !lab.Completed, "Triangle loads ready for interaction");
        for (float x = -0.48f; x <= 0.48f; x += 0.04f)
        {
            Vector3 angles = TriangleAnglesLab.CalculateAngles(x);
            Require(angles.x > 0 && angles.y > 0 && angles.z > 0 && Mathf.Abs(angles.x + angles.y + angles.z - 180f) < 0.001f, "Non-degenerate triangle totals 180 degrees");
        }
        var slider = lab.GetComponentInChildren<Slider>();
        slider.value = -0.48f;
        slider.value = 0.48f;
        Require(lab.Completed, "Slider interaction completes triangle observation");
        var handle = lab.GetComponentInChildren<TriangleVertexHandle>();
        handle.OnDrag(Vector3.zero, new Ray(new Vector3(-0.2f, 1.3f, -1f), Vector3.forward));
        Require(Mathf.Abs(lab.VertexX + 0.2f) < 0.001f, "World pointer ray moves triangle vertex");
        lab.DragVertexNear(new Vector3(0.3f, 1.3f, 0f));
        Require(Mathf.Abs(lab.VertexX - 0.3f) < 0.001f, "Near hand position moves triangle vertex");
        lab.ResetExperiment();
        Require(!lab.Completed && Mathf.Abs(slider.value) < 0.001f, "Triangle reset clears progress and slider");
        PcPointerRegression.Run(lab);
    }

    static void CheckLever()
    {
        var lab = UnityEngine.Object.FindObjectOfType<LeverBalanceLab>();
        Require(lab != null && !lab.Completed, "Lever starts unbalanced");
        lab.SetDistance(0.4f);
        Require(lab.IsBalanced && Mathf.Abs(lab.LeftTorque - lab.RightTorque) < 0.001f, "Equal torques balance at 40 cm");
        lab.SetDistance(5f);
        Require(lab.DistanceMeters <= 0.5f && !lab.IsBalanced, "Distance clamps and imbalance updates");
        lab.ResetExperiment();
        Require(!lab.Completed && !lab.IsBalanced, "Lever reset clears completion");
        lab.GetComponentInChildren<Slider>().value = 40f;
        Require(lab.IsBalanced, "Lever slider invokes experiment logic");
    }

    static void CheckIndicator()
    {
        var lab = UnityEngine.Object.FindObjectOfType<IndicatorLab>();
        Require(lab != null && !lab.Completed, "Indicator starts incomplete");
        var choices = lab.GetComponentsInChildren<Button>().Where(button => button.name == "ExperimentAction").ToArray();
        Require(choices.Length == 3, "Three sample choices are accessible");
        foreach (var choice in choices) choice.onClick.Invoke();
        Require(lab.Completed, "Observing three samples completes experiment");
        lab.ResetExperiment();
        Require(!lab.Completed, "Indicator reset clears observation progress");
    }

    static void CaptureAfterSeconds(string path, float seconds)
    {
        float target = Time.time + seconds;
        EditorApplication.CallbackFunction callback = null;
        callback = () =>
        {
            if (Application.isPlaying && Time.time < target) return;
            EditorApplication.update -= callback;
            if (Application.isPlaying) ScreenCapture.CaptureScreenshot(path);
        };
        EditorApplication.update += callback;
    }

    static void CheckXRPanel()
    {
        var lab = UnityEngine.Object.FindObjectOfType<MiniLabBase>();
        var method = typeof(MiniLabBase).GetMethod("LayoutXRPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        var canvases = lab.GetComponentsInChildren<Canvas>().Where(canvas => canvas.name.StartsWith("MiniLab ")).ToArray();
        Require(canvases.Length == 2, "Experiment has two compact panels");
        for (int i = 0; i < canvases.Length; i++)
        {
            var panel = (RectTransform)canvases[i].transform.Find("Panel");
            method.Invoke(lab, new object[] { canvases[i], panel, i == 1 });
            Require(canvases[i].renderMode == RenderMode.WorldSpace && canvases[i].GetComponent<TrackedDeviceGraphicRaycaster>() != null, "XR panel uses tracked-device raycasting");
            Require(panel.localScale == Vector3.one, "XR controls retain authored touch target size");
            Require(canvases[i].GetComponent<CanvasScaler>().uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize, "XR canvas uses fixed desk scale");
        }
    }

    static void CheckXRMenu()
    {
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        var left = menu.GetComponentInChildren<Canvas>();
        var right = UiFactory.CreateOverlay("Test Right Menu", menu.transform);
        right.GetComponent<XROverlayCanvas>().enabled = false;
        menu.LayoutForXR(left.transform, right.transform);
        CheckMenu();
    }

    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) Finish(false, message + "\n" + trace);
    }

    static void Finish(bool success, string message)
    {
        if (_failed) return;
        _failed = true;
        SessionState.SetBool(ActiveKey, false);
        Application.logMessageReceived -= Log;
        EditorApplication.update -= Tick;
        PlayerPrefs.SetString("ScienceStudio.Locale", _locale ?? SessionState.GetString("MiniLabSmokeChecks.Locale", Loc.Chinese));
        PlayerPrefs.Save();
        Directory.CreateDirectory("Logs/MiniLabs");
        File.WriteAllText("Logs/MiniLabs/results.txt", message);
        if (success) Debug.Log("MINI_LAB_SMOKE_CHECKS_PASSED");
        else Debug.LogError(message);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }

    static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Mini lab check failed: " + description);
    }
}
