using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

/// <summary>Reproducible editor-only previews. Never represents a physical headset capture.</summary>
[InitializeOnLoad]
public static class MRVisualPreview
{
    const string ActiveKey = "MRVisualPreview.Active";
    const string Output = "Logs/MRVisualChecks";
    static readonly string[] Scenes = { "NetFoldFree", "TriangleAngles", "LensCraftFree", "LeverBalance", "ParticleDriftFree", "IndicatorLab" };
    static int _scene, _phase, _frame;
    static bool _finishing;
    static float _notBefore;
    static Camera _camera;
    static Texture2D _background;
    static Material _backgroundMaterial;

    [Serializable] class SavedScene { public string path; public bool loaded, active; }
    [Serializable] class SavedScenes { public SavedScene[] items; }

    static MRVisualPreview()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += PlayState;
        if (SessionState.GetBool(ActiveKey, false)) Application.logMessageReceived += Log;
    }

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (SessionState.GetBool(ActiveKey, false)) Tick();
    }

    [MenuItem("XR/Capture MR Editor Previews")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play Mode.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene changes before previewing.");
        RuntimeMaterialSetup.EnsureAssets();
        MRVisualChecks.Run();
        Directory.CreateDirectory(Output);
        SessionState.SetString(ActiveKey + ".Scenes", JsonUtility.ToJson(new SavedScenes { items = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
        SessionState.SetString(ActiveKey + ".Locale", Loc.PreferredCode());
        SessionState.SetBool(ActiveKey, true);
        _scene = _phase = _frame = 0;
        _finishing = false;
        Application.logMessageReceived += Log;
        EditorSceneManager.OpenScene("Assets/Scenes/" + Scenes[0] + ".unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!Application.isPlaying || _finishing || Time.frameCount < _frame + 60 || Time.realtimeSinceStartup < _notBefore) return;
        _frame = Time.frameCount;
        try
        {
            if (_phase == 0) { Prepare(); _phase = 1; _notBefore = Time.realtimeSinceStartup + 1.5f; return; }
            Capture();
            _phase = 0;
            if (++_scene == Scenes.Length) { Finish(true, "PASS: Six production scenes rendered with MR furniture visibility and glass materials. These are Unity Editor previews with a photo backdrop, not headset captures."); return; }
            SceneManager.LoadScene(Scenes[_scene]);
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    static void Prepare()
    {
        Loc.SetLocale(Loc.Chinese);
        _camera = Camera.main;
        if (_camera == null) throw new InvalidOperationException("No active experiment camera.");
        var orbit = _camera.GetComponent<PCOrbitCamera>();
        if (orbit != null) orbit.enabled = false;
        // Run the existing interaction/teaching invariants against the new meshes.
        string check = _scene == 1 ? "CheckTriangle" : _scene == 3 ? "CheckLever" : _scene == 5 ? "CheckIndicator" : null;
        if (check != null)
        {
            var settings = InputSystem.settings;
            var inputFocusPolicy = settings.editorInputBehaviorInPlayMode;
            var background = settings.backgroundBehavior;
            try
            {
                // A batch editor has no focused Game view. Route synthetic test
                // devices there explicitly, then restore the editor preference.
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                typeof(MiniLabSmokeChecks).GetMethod(check, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
            finally { settings.editorInputBehaviorInPlayMode = inputFocusPolicy; settings.backgroundBehavior = background; }
        }
        if (_scene == 0)
        {
            var lab = UnityEngine.Object.FindObjectOfType<NetFoldLab>();
            lab.ShowFreeShape(ShapeType.Cube);
            lab.Unfold();
            lab.Unfolding.Unfold(true);
            lab.Input.ClearSelection();
        }
        if (_scene == 2)
        {
            var bench = UnityEngine.Object.FindObjectOfType<OpticalBenchController>();
            bench.ShowRays = true;
            bench.AutoScreen = true;
            bench.ApplyPlacement();
        }
        if (_scene == 4)
        {
            var lab = UnityEngine.Object.FindObjectOfType<ExperimentController>();
            lab.Views.Set(true);
            foreach (var beaker in UnityEngine.Object.FindObjectsOfType<BeakerController>())
                if (beaker.gameObject.activeInHierarchy) beaker.BeginDiffusion(24);
        }
        var data = _camera.GetComponent<UniversalAdditionalCameraData>();
        if (data != null) { data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None; }
        _camera.allowHDR = false;
        _camera.allowMSAA = true;
        foreach (var presentation in UnityEngine.Object.FindObjectsOfType<XRRealityPresentation>()) presentation.Apply(true);
        foreach (var decor in UnityEngine.Object.FindObjectsOfType<MRDecorVisibility>()) decor.Apply(true);
        foreach (var lighting in UnityEngine.Object.FindObjectsOfType<MRStudioLighting>()) lighting.Apply(true);
        foreach (var panel in UnityEngine.Object.FindObjectsOfType<MRPanelStyle>()) panel.Apply(true);
        foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            if (canvas.renderMode != RenderMode.WorldSpace) canvas.gameObject.SetActive(false);
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.clear;
        _camera.fieldOfView = 38;
        Vector3 focus = new Vector3(0, 1.04f, 0);
        Vector3 offset = new Vector3(0.7f, 0.55f, -1.75f);
        if (_scene == 0) { focus = new Vector3(0, 0.98f, 0.35f); offset = new Vector3(1.2f, 1.4f, -2.5f); }
        if (_scene == 1) { focus = new Vector3(0, 1.13f, 0); offset = new Vector3(0.05f, 0.2f, -1.7f); }
        if (_scene == 2) { focus = new Vector3(0, 1.02f, 0); offset = new Vector3(0.45f, 0.5f, -1.5f); }
        if (_scene == 4) { focus = new Vector3(0, 0.94f, 0.06f); offset = new Vector3(0.28f, 0.4f, -1.15f); }
        _camera.transform.position = focus + offset;
        _camera.transform.LookAt(focus);
        _camera.aspect = 16f / 9f;
        if (_background == null)
        {
            _background = new Texture2D(2, 2);
            _background.LoadImage(File.ReadAllBytes("Presentation/assets/ar-classroom-v2/sources/barry-zhou-classroom.jpg"));
            _backgroundMaterial = RuntimeMaterials.Unlit(false);
            _backgroundMaterial.SetTexture("_BaseMap", _background);
        }
        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Editor photo backdrop - NOT device passthrough";
        UnityEngine.Object.Destroy(backdrop.GetComponent<Collider>());
        backdrop.transform.position = _camera.transform.position + _camera.transform.forward * 5f;
        backdrop.transform.rotation = _camera.transform.rotation;
        float h = 10f * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        backdrop.transform.localScale = new Vector3(h * _camera.aspect, h, 1);
        backdrop.GetComponent<Renderer>().sharedMaterial = _backgroundMaterial;
    }

    static void Capture()
    {
        if (_scene == 4)
        {
            foreach (var beaker in UnityEngine.Object.FindObjectsOfType<BeakerController>())
            {
                var particles = beaker.Motion.GetComponentInChildren<ParticleSystem>();
                if (!particles.GetComponent<ParticleSystemRenderer>().enabled || particles.particleCount == 0)
                    throw new InvalidOperationException("Micro view must render the molecule mesh particles.");
            }
        }
        var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
        var priorTarget = _camera.targetTexture;
        var priorActive = RenderTexture.active;
        Texture2D image = null;
        try
        {
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/editor-preview-" + Scenes[_scene] + ".png", image.EncodeToPNG());
        }
        finally
        {
            _camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            RenderTexture.ReleaseTemporary(target);
            if (image != null) UnityEngine.Object.Destroy(image);
        }
    }

    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) Finish(false, message + "\n" + trace);
    }

    static void Finish(bool success, string message)
    {
        if (_finishing) return;
        _finishing = true;
        SessionState.SetBool(ActiveKey + ".Success", success);
        Application.logMessageReceived -= Log;
        File.WriteAllText(Output + "/preview-results.txt", (success ? "" : "FAILED: ") + message);
        if (_backgroundMaterial != null) UnityEngine.Object.Destroy(_backgroundMaterial);
        if (_background != null) UnityEngine.Object.Destroy(_background);
        EditorApplication.isPlaying = false;
    }

    static void PlayState(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
        SessionState.SetBool(ActiveKey, false);
        var saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(ActiveKey + ".Scenes", "{}"));
        try
        {
            if (saved.items != null && saved.items.Any(s => s.loaded && s.active && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(saved.items.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
            PlayerPrefs.SetString("ScienceStudio.Locale", SessionState.GetString(ActiveKey + ".Locale", Loc.Chinese));
            PlayerPrefs.Save();
        }
        finally { if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(ActiveKey + ".Success", false) ? 0 : 1); }
    }
}
