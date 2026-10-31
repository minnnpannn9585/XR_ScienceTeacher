using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Subject, lesson and mode navigation, shared by mouse and XR input.</summary>
public class MainMenuUI : MonoBehaviour
{
    GameObject _settings, _homePage, _subjectPage, _modesPage;
    Transform _pages;
    Button _chinese, _english, _back, _quit, _existingLesson, _newLesson;
    TMP_Text _currentLanguage, _subtitle, _hint, _progress, _modeHeading;
    readonly Button[] _subjects = new Button[3];
    readonly Button[] _modes = new Button[3];
    InputAdapter _input;
    bool _xrLayout;
    public int CurrentSubjectIndex { get; private set; } = -1;
    public bool ShowingModes { get; private set; }
    static readonly Color LanguageOn = new Color(0.55f, 0.46f, 0.28f, 1f);
    static readonly Color LanguageOff = new Color(0.12f, 0.13f, 0.15f, 0.92f);
    static readonly string[] ExistingTitleKeys = { "menu.netfold", "menu.lens", "menu.drift" };
    static readonly string[] ExistingBlurbKeys = { "menu.netfold.blurb", "menu.lens.blurb", "menu.drift.blurb" };
    static readonly string[] ExistingIds = { "NetFold", "LensCraft", "ParticleDrift" };

    void OnEnable()
    {
        Loc.Changed += Refresh;
        if (_input != null) _input.BackPressed += NavigateBack;
    }

    void OnDisable()
    {
        Loc.Changed -= Refresh;
        if (_input != null) _input.BackPressed -= NavigateBack;
    }

    public void Build()
    {
        DressWorld();
        var canvas = UiFactory.CreateOverlay("MainMenuCanvas", transform);
        var root = canvas.transform;
        UiFactory.ScreenWash(root);
        var eyebrow = UiFactory.Label(root, "Eyebrow", Loc.Get("menu.eyebrow"), 18, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 292f), new Vector2(700f, 328f), NetFoldTheme.Hairline);
        eyebrow.fontStyle = FontStyles.Bold;
        LocalizedBinding.Bind(eyebrow, "menu.eyebrow");
        UiFactory.Label(root, "Title", "AR Science", 64, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 200f), new Vector2(860f, 292f));
        _subtitle = UiFactory.Label(root, "Sub", "", 24, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 148f), new Vector2(900f, 204f), NetFoldTheme.TextDim);
        BindButton(UiFactory.Button(root, "Settings", Loc.Get("common.settings"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(720f, 286f), new Vector2(900f, 340f), OpenSettings, new Color(0.22f, 0.2f, 0.16f, 0.92f)), "common.settings");
        _pages = new GameObject("MenuPages", typeof(RectTransform), typeof(CanvasGroup)).transform;
        var rect = (RectTransform)_pages;
        rect.SetParent(root, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(72f, -314f);
        rect.offsetMax = new Vector2(900f, 136f);
        _homePage = Page("HomePage");
        _subjectPage = Page("SubjectPage");
        _modesPage = Page("ModesPage");
        string[] names = { "Mathematics", "Physics", "Chemistry" };
        for (int i = 0; i < 3; i++)
        {
            int subject = i;
            _subjects[i] = Card(_homePage.transform, names[i], i * 146f, 134f, () => ShowSubject(subject));
        }
        _existingLesson = Card(_subjectPage.transform, "ExistingLesson", 0f, 196f, ShowModes);
        _newLesson = Card(_subjectPage.transform, "NewLesson", 212f, 196f, LaunchNewLesson);
        _modeHeading = UiFactory.Label(_modesPage.transform, "ModeHeading", "", 24, TextAlignmentOptions.Left, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -60f), new Vector2(-24f, 0f), NetFoldTheme.Hairline);
        string[] modeNames = { "Learn", "Free", "Challenge" };
        for (int i = 0; i < 3; i++)
        {
            int mode = i;
            _modes[i] = Card(_modesPage.transform, modeNames[i], 72f + i * 116f, 104f, () => LaunchExisting(mode));
        }
        _back = UiFactory.Button(root, "Back", Loc.Get("common.back"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -394f), new Vector2(330f, -342f), NavigateBack, LanguageOff);
        _quit = UiFactory.Button(root, "Quit", Loc.Get("common.quit"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -394f), new Vector2(280f, -342f), QuitApp, LanguageOff);
        BindButton(_quit, "common.quit");
        _progress = UiFactory.Label(root, "Progress", "", 20, TextAlignmentOptions.Right, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(340f, -394f), new Vector2(900f, -342f), NetFoldTheme.Hairline);
        _hint = UiFactory.Label(root, "Hint", "", 18, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -450f), new Vector2(900f, -402f), NetFoldTheme.TextDim);
        BuildSettings(root);
        _input = InputAdapter.Instance;
        if (_input != null) _input.BackPressed += NavigateBack;
        Refresh();
    }

    GameObject Page(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(_pages, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return go;
    }

    static Button Card(Transform parent, string name, float top, float height, UnityEngine.Events.UnityAction onClick)
    {
        var button = UiFactory.Button(parent, name, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -top - height), new Vector2(0f, -top), onClick, NetFoldTheme.Glass);
        var title = button.GetComponentInChildren<TMP_Text>();
        title.alignment = TextAlignmentOptions.Left;
        title.fontSizeMax = 30f;
        title.fontSizeMin = 24f;
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(24f, 0f);
        title.rectTransform.offsetMax = new Vector2(-48f, -8f);
        var description = UiFactory.Label(button.transform, "Description", "", 21, TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(24f, 10f), new Vector2(-48f, 0f), NetFoldTheme.TextDim);
        description.enableAutoSizing = true;
        description.fontSizeMin = 18f;
        description.fontSizeMax = 23f;
        UiFactory.Label(button.transform, "Arrow", ">", 30, TextAlignmentOptions.Right, new Vector2(1f, 0f), Vector2.one, new Vector2(-42f, 0f), new Vector2(-16f, 0f), NetFoldTheme.Hairline);
        return button;
    }

    public void ShowSubject(int index)
    {
        if (index < 0 || index >= 3) return;
        CurrentSubjectIndex = index;
        ShowingModes = false;
        ClearFocus();
        Refresh();
    }

    public void ShowModes()
    {
        if (CurrentSubjectIndex < 0) return;
        ShowingModes = true;
        ClearFocus();
        Refresh();
    }

    public void NavigateBack()
    {
        if (_settings != null && _settings.activeSelf) { CloseSettings(); return; }
        if (ShowingModes) ShowingModes = false;
        else CurrentSubjectIndex = -1;
        ClearFocus();
        Refresh();
    }

    static void ClearFocus()
    {
        var system = UnityEngine.EventSystems.EventSystem.current;
        if (system != null) system.SetSelectedGameObject(null);
    }

    void LaunchExisting(int mode)
    {
        if (CurrentSubjectIndex == 0)
        {
            if (mode == 0) SceneLoader.LoadNetFold();
            else if (mode == 1) SceneLoader.LoadNetFoldFree();
            else SceneLoader.LoadNetFoldChallenge();
        }
        else if (CurrentSubjectIndex == 1)
        {
            if (mode == 0) SceneLoader.LoadLensCraft();
            else if (mode == 1) SceneLoader.LoadLensCraftFree();
            else SceneLoader.LoadLensCraftChallenge();
        }
        else if (CurrentSubjectIndex == 2)
        {
            if (mode == 0) SceneLoader.LoadParticleDrift();
            else if (mode == 1) SceneLoader.LoadParticleDriftFree();
            else SceneLoader.LoadParticleDriftChallenge();
        }
    }

    void LaunchNewLesson()
    {
        if (CurrentSubjectIndex == 0) SceneLoader.LoadTriangleAngles();
        else if (CurrentSubjectIndex == 1) SceneLoader.LoadLeverBalance();
        else if (CurrentSubjectIndex == 2) SceneLoader.LoadIndicatorLab();
    }

    static string Copy(string zh, string en) => Loc.IsEnglish ? en : zh;
    static string SubjectName(int index) => index == 0 ? Copy("数学", "Mathematics") : index == 1 ? Copy("物理", "Physics") : Copy("化学", "Chemistry");
    static string NewTitle(int index) => index == 0 ? Copy("三角形内角和", "Triangle Angles") : index == 1 ? Copy("杠杆平衡", "Lever Balance") : Copy("酸碱探色", "Acid-base Colors");

    static void SetCard(Button card, string title, string description)
    {
        card.transform.Find("Label").GetComponent<TMP_Text>().text = title;
        card.transform.Find("Description").GetComponent<TMP_Text>().text = description;
    }

    void Refresh()
    {
        PaintLanguages();
        if (_homePage == null) return;
        bool home = CurrentSubjectIndex < 0;
        _homePage.SetActive(home);
        _subjectPage.SetActive(!home && !ShowingModes);
        _modesPage.SetActive(!home && ShowingModes);
        _back.gameObject.SetActive(!home);
        _quit.gameObject.SetActive(home);
        _subtitle.text = home ? Copy("选择学科，开始探索 · 6 个关卡", "Choose a subject · 6 experiments") : SubjectName(CurrentSubjectIndex) + (ShowingModes ? " / " + Loc.Get(ExistingTitleKeys[CurrentSubjectIndex]) : Copy(" / 选择关卡", " / Choose an experiment"));
        _back.GetComponentInChildren<TMP_Text>().text = ShowingModes ? Copy("< 返回关卡", "< Experiments") : Copy("< 返回学科", "< Subjects");
        for (int i = 0; i < 3; i++)
            SetCard(_subjects[i], SubjectName(i), Loc.Get(ExistingTitleKeys[i]) + " · " + NewTitle(i));
        if (!home)
        {
            SetCard(_existingLesson, "01  " + Loc.Get(ExistingTitleKeys[CurrentSubjectIndex]), Loc.Get(ExistingBlurbKeys[CurrentSubjectIndex]) + Copy("\n讲解 / 自由实验 / 挑战", "\nLesson / Free Lab / Challenge"));
            string action = CurrentSubjectIndex == 0 ? Copy("拖动顶点，观察三个内角的变化。", "Move a vertex and watch the three angles.") : CurrentSubjectIndex == 1 ? Copy("移动砝码，让两侧力矩相等。", "Move the weight to balance the torques.") : Copy("切换样品，观察指示剂的颜色。", "Switch samples and observe the indicator colors.");
            SetCard(_newLesson, "02  " + NewTitle(CurrentSubjectIndex), Copy("轻量实验 · 一个操作发现规律\n", "Quick experiment · Discover by doing\n") + action);
            _modeHeading.text = Copy("选择体验方式", "Choose how to explore");
            SetCard(_modes[0], Loc.Get("common.lesson"), Copy("跟随讲解，理解原理", "A guided introduction to the idea"));
            SetCard(_modes[1], Loc.Get("common.free"), Copy("自由调整，观察变化", "Adjust the experiment at your own pace"));
            SetCard(_modes[2], Loc.Get("common.challenge"), Copy("运用所学，挑战三星", "Apply what you learned and earn stars"));
        }
        int stars = home ? ChallengeProgress.BestStars("NetFold") + ChallengeProgress.BestStars("LensCraft") + ChallengeProgress.BestStars("ParticleDrift") : ChallengeProgress.BestStars(ExistingIds[CurrentSubjectIndex]);
        _progress.text = home ? Copy("挑战记录  ", "Challenge progress  ") + stars + " / 9 ★" : Copy("挑战最佳成绩  ", "Best challenge score  ") + ChallengeProgress.StarsText(stars);
        _hint.text = _xrLayout ? Loc.Get("xr.interactionHint") : Copy("鼠标点击选择   ·   Esc 返回上一级", "Click to select   ·   Esc to go back");
    }

    /// <summary>Move entire pages, keeping hidden pages and callbacks intact.</summary>
    public void LayoutForXR(Transform left, Transform right)
    {
        _xrLayout = true;
        left.Find("Wash").gameObject.SetActive(false);
        Box(left.Find("Eyebrow"), left, 24f, 18f, 552f, 32f);
        Box(left.Find("Title"), left, 24f, 52f, 552f, 64f);
        Box(_subtitle.transform, left, 24f, 120f, 552f, 80f);
        left.Find("Title").GetComponent<TMP_Text>().fontSize = 48f;
        _subtitle.enableAutoSizing = true;
        _subtitle.fontSizeMin = 22f;
        _subtitle.fontSizeMax = 28f;
        Box(_pages, left, 16f, 220f, 568f, 450f);
        Bind(UiFactory.Label(right, "Welcome", Loc.Get("xr.welcome"), 32, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), "xr.welcome");
        Box(right.Find("Welcome"), right, 24f, 24f, 552f, 64f);
        Box(_hint.transform, right, 24f, 112f, 552f, 210f);
        _hint.fontSize = 28f;
        Box(_progress.transform, right, 24f, 338f, 552f, 64f);
        _progress.fontSize = 24f;
        _progress.alignment = TextAlignmentOptions.Left;
        Box(left.Find("Settings"), right, 24f, 416f, 552f, 80f);
        Box(_back.transform, right, 24f, 548f, 552f, 80f);
        Box(_quit.transform, right, 24f, 548f, 552f, 80f);
        Box(_settings.transform, right, 0f, 0f, 600f, 740f);
        var panel = _settings.transform.Find("SettingsPanel");
        Box(panel, _settings.transform, 12f, 12f, 576f, 716f);
        Box(panel.Find("Title"), panel, 24f, 24f, 528f, 72f);
        Box(panel.Find("Language"), panel, 24f, 112f, 528f, 56f);
        Box(panel.Find("Chinese"), panel, 24f, 184f, 528f, 80f);
        Box(panel.Find("English"), panel, 24f, 280f, 528f, 80f);
        Box(panel.Find("Current"), panel, 24f, 384f, 528f, 64f);
        Box(panel.Find("Hint"), panel, 24f, 464f, 528f, 124f);
        Box(panel.Find("Close"), panel, 24f, 620f, 528f, 72f);
        _settings.transform.SetAsLastSibling();
        Refresh();
    }

    static void Box(Transform target, Transform parent, float x, float top, float width, float height)
    {
        var rect = (RectTransform)target;
        rect.SetParent(parent, false);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition3D = new Vector3(x, -top, 0f);
    }

    void BuildSettings(Transform root)
    {
        var blocker = new GameObject("SettingsBlocker", typeof(RectTransform));
        var blockerRt = blocker.GetComponent<RectTransform>();
        blockerRt.SetParent(root, false);
        blockerRt.anchorMin = Vector2.zero;
        blockerRt.anchorMax = Vector2.one;
        blockerRt.offsetMin = Vector2.zero;
        blockerRt.offsetMax = Vector2.zero;
        // The dismiss button is a sibling of the panel. Putting it on the panel's
        // ancestor also treats clicks on the title or empty panel space as dismissals.
        var backdrop = new GameObject("SettingsBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.SetParent(blocker.transform, false);
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = backdropRt.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
        var blockerButton = backdrop.GetComponent<Button>();
        blockerButton.transition = Selectable.Transition.None;
        blockerButton.onClick.AddListener(CloseSettings);

        var panel = UiFactory.Panel(blocker.transform, "SettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, -240f), new Vector2(380f, 240f), NetFoldTheme.Glass);
        Bind(UiFactory.Label(panel.transform, "Title", Loc.Get("menu.settings.title"), 36, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -92f), new Vector2(-36f, -28f)), "menu.settings.title");
        Bind(UiFactory.Label(panel.transform, "Language", Loc.Get("menu.settings.language"), 22, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -144f), new Vector2(-36f, -104f), NetFoldTheme.Hairline), "menu.settings.language");

        _chinese = UiFactory.Button(panel.transform, "Chinese", "中文", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -232f), new Vector2(350f, -160f), () => Choose(Loc.Chinese));
        _english = UiFactory.Button(panel.transform, "English", "English", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(370f, -232f), new Vector2(724f, -160f), () => Choose(Loc.English));
        _currentLanguage = UiFactory.Label(panel.transform, "Current", "", 20, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -284f), new Vector2(-36f, -244f), NetFoldTheme.TextDim);
        Bind(UiFactory.Label(panel.transform, "Hint", Loc.Get("menu.settings.hint"), 18, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(36f, 100f), new Vector2(-36f, 168f), NetFoldTheme.TextDim), "menu.settings.hint");
        BindButton(UiFactory.Button(panel.transform, "Close", Loc.Get("common.close"), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 28f), new Vector2(220f, 84f), CloseSettings, LanguageOff), "common.close");

        _settings = blocker;
        PaintLanguages();
        _settings.SetActive(false);
    }

    static TMP_Text Bind(TMP_Text text, string key, params object[] args)
    {
        LocalizedBinding.Bind(text, key, args);
        return text;
    }

    static void BindButton(Button button, string key)
    {
        LocalizedBinding.Bind(button.GetComponentInChildren<TMP_Text>(), key);
    }

    void OpenSettings()
    {
        if (_settings == null) return;
        _settings.SetActive(true);
        _settings.transform.SetAsLastSibling();
        _pages.GetComponent<CanvasGroup>().interactable = false;
        PaintLanguages();
    }

    void CloseSettings()
    {
        if (_settings == null) return;
        _settings.SetActive(false);
        _pages.GetComponent<CanvasGroup>().interactable = true;
    }

    void Choose(string code)
    {
        Loc.SetLocale(code);
        Refresh();
    }

    void PaintLanguages()
    {
        if (_chinese == null || _english == null) return;
        bool english = Loc.IsEnglish;
        UiFactory.SetButtonColor(_chinese, english ? LanguageOff : LanguageOn);
        UiFactory.SetButtonColor(_english, english ? LanguageOn : LanguageOff);
        if (_currentLanguage != null)
            LocalizedBinding.Bind(_currentLanguage, english ? "menu.current.en" : "menu.current.zh");
    }

    static void DressWorld()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("MenuCamera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        Vector3 hero = new Vector3(1.05f, 1.02f, 0.15f);
        if (!InputAdapter.DetectXR())
        {
            cam.fieldOfView = 32f;
            cam.transform.position = new Vector3(0.1f, 1.42f, -2.5f);
            cam.transform.LookAt(new Vector3(0.28f, 1.12f, 0.55f));
        }

        var root = new GameObject("MenuStudio").transform;
        StudioSet.Install(root, cam, hero);
        var desk = LabFactory.Primitive(PrimitiveType.Cube, "MenuPlinth", root, new Vector3(hero.x, XRDeskStation.SurfaceHeight - XRDeskStation.TopThickness * 0.5f, hero.z), new Vector3(0.78f, XRDeskStation.TopThickness, 0.78f), StudioSet.Stone(), false);
        StudioSet.DressDesk(desk.transform);
        var shape = GeometryFactory.Create(ShapeType.Cube, root, new Vector3(hero.x, XRDeskStation.SurfaceHeight + 0.0025f, hero.z), 0.36f, false);
        shape.AllowIdleSpin = true;
        shape.IdleSpinSpeed = 8f;
    }

    static void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
