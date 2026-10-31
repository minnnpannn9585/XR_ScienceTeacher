using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Small experiments share one set of controls across desktop and XR desk panels.</summary>
public abstract class MiniLabBase : MonoBehaviour
{
    protected abstract string TitleZh { get; }
    protected abstract string TitleEn { get; }
    protected abstract string GoalZh { get; }
    protected abstract string GoalEn { get; }
    protected Transform ExperimentRoot { get; private set; }
    protected Color Accent = new Color(0.45f, 0.78f, 0.75f);
    public bool Completed { get; private set; }
    public bool IsBuilt { get; private set; }
    protected abstract void BuildExperiment();
    public abstract void ResetExperiment();
    protected virtual void RefreshExperimentText() { }

    readonly List<Material> _materials = new List<Material>();
    readonly List<System.Action> _translations = new List<System.Action>();
    Canvas _left, _right;
    RectTransform _info, _controls;
    TMP_Text _readout, _feedback;
    string _readoutZh, _readoutEn, _feedbackZh, _feedbackEn;
    InputAdapter _input;
    XRDeskStation _station;
    GameObject _recenter;
    float _controlTop = 180f;
    bool _xrLayout;

    protected virtual void Awake() => Bootstrap();

    public void Bootstrap()
    {
        if (IsBuilt) return;
        IsBuilt = true;
        XRExperience.ConfigureEventSystem();
        if (FindObjectOfType<FeedbackService>() == null)
            new GameObject("Feedback").AddComponent<FeedbackService>();
        var rig = new GameObject("PC Rig");
        rig.transform.SetParent(transform, false);
        var cameraObject = new GameObject("ExperimentCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(rig.transform, false);
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.nearClipPlane = 0.03f;
        camera.fieldOfView = 36f;
        camera.transform.position = new Vector3(0f, 1.72f, -2.65f);
        camera.transform.LookAt(new Vector3(0f, XRDeskStation.SurfaceHeight + 0.2f, 0f));
        _input = gameObject.AddComponent<InputAdapter>();
        _input.PcCamera = camera;
        _input.PcRig = rig;
        var surface = new Vector3(0f, XRDeskStation.SurfaceHeight, 0f);
        StudioSet.Install(transform, camera, surface);
        var desk = Part(PrimitiveType.Cube, "Experiment Desk", transform,
            surface - Vector3.up * XRDeskStation.TopThickness * 0.5f,
            new Vector3(1.3f, XRDeskStation.TopThickness, 0.78f), NetFoldTheme.Stone);
        StudioSet.DressDesk(desk.transform);
        ExperimentRoot = new GameObject("Experiment").transform;
        ExperimentRoot.SetParent(transform, false);
        ExperimentRoot.position = surface;
        XRExperience.Attach(_input, transform, surface, 0.78f);
        _station = GetComponent<XRDeskStation>();
        _input.BackPressed += SceneLoader.LoadMainMenu;
        BuildInterface();
        BuildExperiment();
        ResetExperiment();
        Loc.Changed += Translate;
        Translate();
    }

    void BuildInterface()
    {
        _left = MakeCanvas("MiniLab Information");
        _right = MakeCanvas("MiniLab Controls");
        _info = Panel(_left, false);
        _controls = Panel(_right, true);
        Text(_info, "Subject", "轻实验 / 约 1 分钟", "MINI LAB / ABOUT 1 MIN", 28, 24, 544, 40, 20, Accent);
        Text(_info, "Title", TitleZh, TitleEn, 28, 82, 544, 84, 38);
        Text(_info, "Goal", GoalZh, GoalEn, 28, 180, 544, 130, 26);
        _readout = Text(_info, "Readout", "", "", 28, 330, 544, 174, 28, Accent, false);
        _feedback = Text(_info, "Feedback", "", "", 28, 526, 544, 178, 25, null, false);
        Text(_controls, "ControlsTitle", "动手试一试", "TRY IT", 28, 24, 544, 60, 32);
        Text(_controls, "InputHint", "拖动滑块或点按按钮，观察桌面变化。\nPC：鼠标操作 · XR：射线或手指点按", "Drag a slider or press a button.\nPC: mouse · XR: ray or finger touch", 28, 94, 544, 76, 23, NetFoldTheme.TextDim);
        ActionButton(_controls, "Reset", "重新实验", "Reset experiment", 28, 548, 544, 64, ResetExperiment);
        ActionButton(_controls, "Menu", "返回学科菜单", "Subject menu", 28, 626, 264, 64, SceneLoader.LoadMainMenu);
        _recenter = ActionButton(_controls, "Recenter", "回到桌前", "Return to desk", 308, 626, 264, 64, () => _station.ReturnToDesk()).gameObject;
        _recenter.SetActive(false);
    }

    Canvas MakeCanvas(string name)
    {
        var canvas = UiFactory.CreateOverlay(name, transform);
        // These compact panels have a fixed shared layout, not the full lesson HUD.
        var legacyLayout = canvas.GetComponent<XROverlayCanvas>();
        legacyLayout.enabled = false;
        return canvas;
    }

    RectTransform Panel(Canvas canvas, bool right)
    {
        var panel = UiFactory.Panel(canvas.transform, "Panel", Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, new Color(0.055f, 0.065f, 0.08f, 0.94f)).rectTransform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(right ? 1f : 0f, 0.5f);
        panel.sizeDelta = XRDeskStation.PanelPixels;
        panel.anchoredPosition = new Vector2(right ? -28f : 28f, 0f);
        panel.localScale = Vector3.one * 0.85f;
        return panel;
    }

    protected virtual void LateUpdate()
    {
        if (!XRSession.IsActive || _station == null) return;
        if (!_xrLayout)
        {
            LayoutXRPanel(_left, _info, false);
            LayoutXRPanel(_right, _controls, true);
            _recenter.SetActive(true);
            _xrLayout = true;
        }
        _left.worldCamera = _right.worldCamera = Camera.main;
    }

    void LayoutXRPanel(Canvas canvas, RectTransform panel, bool right)
    {
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvas.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = XRDeskStation.PanelPixels;
        _station.PlacePanel(rect, right);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 20f;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.localScale = Vector3.one;
    }

    protected Slider AddSlider(string zh, string en, float min, float max, float value, UnityAction<float> changed, bool wholeNumbers = false)
    {
        Text(_controls, "SliderCaption", zh, en, 28, _controlTop, 544, 48, 26);
        var go = new GameObject("ExperimentSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
        Box(go.transform, _controls, 40, _controlTop + 58, 520, 72);
        var background = go.GetComponent<Image>();
        background.sprite = UiFactory.RoundSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.14f, 0.19f, 0.21f);
        var area = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(go.transform, false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(28f, 0f);
        area.offsetMax = new Vector2(-28f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(area, false);
        handle.sizeDelta = new Vector2(56f, 0f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = UiFactory.RoundSprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = Accent;
        var slider = go.GetComponent<Slider>();
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.SetValueWithoutNotify(value);
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.onValueChanged.AddListener(changed);
        _controlTop += 150f;
        return slider;
    }

    protected Button AddAction(string zh, string en, UnityAction action)
    {
        var button = ActionButton(_controls, "ExperimentAction", zh, en, 28, _controlTop, 544, 76, action);
        _controlTop += 90f;
        return button;
    }

    Button ActionButton(Transform parent, string name, string zh, string en, float x, float y, float w, float h, UnityAction action)
    {
        var button = UiFactory.Button(parent, name, "", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, action);
        Box(button.transform, parent, x, y, w, h);
        var label = button.GetComponentInChildren<TMP_Text>();
        _translations.Add(() => label.text = Loc.IsEnglish ? en : zh);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    TMP_Text Text(Transform parent, string name, string zh, string en, float x, float y, float w, float h, int size, Color? color = null, bool translate = true)
    {
        var label = UiFactory.Label(parent, name, "", size, TextAlignmentOptions.TopLeft,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, color);
        Box(label.transform, parent, x, y, w, h);
        label.enableAutoSizing = true;
        label.fontSizeMin = size - 3;
        label.fontSizeMax = size;
        if (translate) _translations.Add(() => label.text = Loc.IsEnglish ? en : zh);
        return label;
    }

    static void Box(Transform target, Transform parent, float x, float y, float width, float height)
    {
        var rect = (RectTransform)target;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, -y);
    }

    protected void SetReadout(string zh, string en)
    {
        _readoutZh = zh;
        _readoutEn = en;
        if (_readout != null) _readout.text = Loc.IsEnglish ? en : zh;
    }

    protected void SetFeedback(string zh, string en, bool completed = false)
    {
        Completed = completed;
        _feedbackZh = zh;
        _feedbackEn = en;
        if (_feedback == null) return;
        _feedback.text = Loc.IsEnglish ? en : zh;
        _feedback.color = completed ? Accent : NetFoldTheme.TextDim;
    }

    void Translate()
    {
        foreach (var translation in _translations) translation();
        SetReadout(_readoutZh, _readoutEn);
        SetFeedback(_feedbackZh, _feedbackEn, Completed);
        RefreshExperimentText();
    }

    protected GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var material = LabFactory.Lit(color, color.a < 0.99f);
        _materials.Add(material);
        return LabFactory.Primitive(type, name, parent, localPosition, localScale, material, false);
    }

    protected LineRenderer Line(string name, Transform parent, Vector3[] points, Color color, float width = 0.008f)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(parent, false);
        line.useWorldSpace = false;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 5;
        line.sharedMaterial = Unlit(color);
        return line;
    }

    // Color comparisons and reading surfaces should not be washed out by studio lights.
    protected Material Unlit(Color color)
    {
        var material = RuntimeMaterials.Unlit(false);
        UrpMaterialUtil.SetColor(material, color);
        _materials.Add(material);
        return material;
    }

    protected TMP_Text WorldLabel(string name, Transform parent, Vector3 localPosition, string text, float size = 0.08f)
    {
        var canvas = UiFactory.CreateWorld(name, parent, parent.TransformPoint(localPosition), new Vector2(620, 130), Vector3.zero);
        // 80 px text has approximately the requested height in world units.
        canvas.transform.localScale = Vector3.one * (size / 80f);
        var label = UiFactory.Label(canvas.transform, "Text", text, 64, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        canvas.gameObject.AddComponent<FaceCamera>();
        return label;
    }

    protected virtual void OnDestroy()
    {
        Loc.Changed -= Translate;
        if (_input != null) _input.BackPressed -= SceneLoader.LoadMainMenu;
        foreach (var material in _materials) if (material != null) Destroy(material);
    }
}
