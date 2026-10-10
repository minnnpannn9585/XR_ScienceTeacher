using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    GameObject _settings;
    Button _chinese;
    Button _english;
    TMP_Text _currentLanguage;

    static readonly Color LanguageOn = new Color(0.55f, 0.46f, 0.28f, 1f);
    static readonly Color LanguageOff = new Color(0.12f, 0.13f, 0.15f, 0.92f);

    void OnEnable()
    {
        Loc.Changed += PaintLanguages;
    }

    void OnDisable()
    {
        Loc.Changed -= PaintLanguages;
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
        Bind(UiFactory.Label(root, "Sub", Loc.Get("menu.subtitle"), 22, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 148f), new Vector2(820f, 204f), NetFoldTheme.TextDim), "menu.subtitle");
        BindButton(UiFactory.Button(root, "Settings", Loc.Get("common.settings"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(720f, 286f), new Vector2(900f, 340f), OpenSettings, new Color(0.22f, 0.2f, 0.16f, 0.92f)), "common.settings");

        Module(root, "NetFold", "menu.netfold", "menu.netfold.blurb", -6f, 136f, NetFoldTheme.Accent, SceneLoader.LoadNetFold, SceneLoader.LoadNetFoldFree, SceneLoader.LoadNetFoldChallenge);
        Module(root, "ParticleDrift", "menu.drift", "menu.drift.blurb", -164f, -22f, new Color(0.78f, 0.52f, 0.58f, 1f), SceneLoader.LoadParticleDrift, SceneLoader.LoadParticleDriftFree, SceneLoader.LoadParticleDriftChallenge);
        Module(root, "LensCraft", "menu.lens", "menu.lens.blurb", -322f, -180f, NetFoldTheme.Hairline, SceneLoader.LoadLensCraft, SceneLoader.LoadLensCraftFree, SceneLoader.LoadLensCraftChallenge);

        BindButton(UiFactory.Button(root, "Quit", Loc.Get("common.quit"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -394f), new Vector2(280f, -342f), QuitApp, new Color(0.12f, 0.13f, 0.15f, 0.92f)), "common.quit");
        int total = ChallengeProgress.BestStars("NetFold") + ChallengeProgress.BestStars("ParticleDrift") + ChallengeProgress.BestStars("LensCraft");
        Bind(UiFactory.Label(root, "Progress", Loc.Format("menu.progress", total), 20, TextAlignmentOptions.Right, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -394f), new Vector2(884f, -342f), NetFoldTheme.Hairline), "menu.progress", total);
        Bind(UiFactory.Label(root, "Hint", Loc.Get("menu.hint"), 18, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -450f), new Vector2(900f, -402f), NetFoldTheme.TextDim), "menu.hint");
        BuildSettings(root);
    }

    void BuildSettings(Transform root)
    {
        var blocker = new GameObject("SettingsBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var blockerRt = blocker.GetComponent<RectTransform>();
        blockerRt.SetParent(root, false);
        blockerRt.anchorMin = Vector2.zero;
        blockerRt.anchorMax = Vector2.one;
        blockerRt.offsetMin = Vector2.zero;
        blockerRt.offsetMax = Vector2.zero;
        blocker.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
        var blockerButton = blocker.GetComponent<Button>();
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

    static void Module(Transform root, string id, string titleKey, string blurbKey, float y0, float y1, Color accent, UnityAction learn, UnityAction free, UnityAction challenge)
    {
        var card = UiFactory.Panel(root, id, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, y0), new Vector2(900f, y1), NetFoldTheme.Glass);
        var line = card.transform.Find("Hairline");
        if (line != null)
        {
            line.GetComponent<Image>().color = accent;
        }

        Bind(UiFactory.Label(card.transform, "Name", Loc.Get(titleKey), 28, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0.46f, 1f), new Vector2(24f, -44f), new Vector2(0f, -12f)), titleKey);
        Bind(UiFactory.Label(card.transform, "Blurb", Loc.Get(blurbKey), 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -72f), new Vector2(-20f, -44f), NetFoldTheme.TextDim), blurbKey);
        int best = ChallengeProgress.BestStars(id);
        string bestKey = best == 0 ? "menu.waiting" : "menu.best";
        var bestLabel = UiFactory.Label(card.transform, "Best", best == 0 ? Loc.Get(bestKey) : Loc.Format(bestKey, ChallengeProgress.StarsText(best)), 20, TextAlignmentOptions.Right, new Vector2(0.46f, 1f), Vector2.one, new Vector2(0f, -44f), new Vector2(-24f, -12f), best == 0 ? NetFoldTheme.TextDim : NetFoldTheme.Hairline);
        if (best == 0)
        {
            LocalizedBinding.Bind(bestLabel, bestKey);
        }
        else
        {
            LocalizedBinding.Bind(bestLabel, bestKey, ChallengeProgress.StarsText(best));
        }

        BindButton(UiFactory.Button(card.transform, "Learn", Loc.Get("common.lesson"), Vector2.zero, Vector2.zero, new Vector2(20f, 16f), new Vector2(286f, 64f), learn), "common.lesson");
        BindButton(UiFactory.Button(card.transform, "Free", Loc.Get("common.free"), Vector2.zero, Vector2.zero, new Vector2(298f, 16f), new Vector2(564f, 64f), free), "common.free");
        BindButton(UiFactory.Button(card.transform, "Challenge", Loc.Get("common.challenge"), Vector2.zero, Vector2.zero, new Vector2(576f, 16f), new Vector2(808f, 64f), challenge), "common.challenge");
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
        if (_settings == null)
        {
            return;
        }

        _settings.SetActive(true);
        _settings.transform.SetAsLastSibling();
        PaintLanguages();
    }

    void CloseSettings()
    {
        if (_settings != null)
        {
            _settings.SetActive(false);
        }
    }

    void Choose(string code)
    {
        Loc.SetLocale(code);
        PaintLanguages();
    }

    void PaintLanguages()
    {
        if (_chinese == null || _english == null)
        {
            return;
        }

        bool english = Loc.IsEnglish;
        UiFactory.SetButtonColor(_chinese, english ? LanguageOff : LanguageOn);
        UiFactory.SetButtonColor(_english, english ? LanguageOn : LanguageOff);
        if (_currentLanguage != null)
        {
            LocalizedBinding.Bind(_currentLanguage, english ? "menu.current.en" : "menu.current.zh");
        }
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
        cam.fieldOfView = 32f;
        cam.transform.position = new Vector3(0.1f, 1.42f, -2.5f);
        cam.transform.LookAt(new Vector3(0.28f, 1.12f, 0.55f));

        var root = new GameObject("MenuStudio").transform;
        StudioSet.Install(root, cam, hero);
        var desk = LabFactory.Primitive(PrimitiveType.Cube, "MenuPlinth", root, new Vector3(hero.x, 0.78f, hero.z), new Vector3(0.78f, 0.045f, 0.78f), StudioSet.Stone(), false);
        StudioSet.DressDesk(desk.transform);
        var shape = GeometryFactory.Create(ShapeType.Cube, root, new Vector3(hero.x, 0.805f, hero.z), 0.36f, false);
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
