using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>XR-only presentation of existing UI callbacks and live data bindings.</summary>
public class XRDeskUILayout : MonoBehaviour
{
    Transform _left;
    Transform _right;
    CanvasGroup _rightGroup;
    GameObject _answer;
    TMP_Text _answerHint;
    float _controlY = 110f;
    readonly List<ReadingPage> _readingPages = new List<ReadingPage>();

    sealed class ReadingPage
    {
        public TMP_Text Text;
        public ScrollRect Scroll;
        public string LastText;
    }

    public void Build(Canvas left, Canvas right, XRDeskStation station)
    {
        _left = left.transform;
        _right = right.transform;
        _rightGroup = right.gameObject.AddComponent<CanvasGroup>();
        Background(_left);
        Background(_right);
        if (_left.name == "MainMenuCanvas") BuildMenu();
        else if (_left.Find("Quiz") != null) BuildChallenge();
        else BuildExperiment();
        var recenter = UiFactory.Button(_right, "ReturnToDesk", Loc.Get("xr.recenter"), Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, station.ReturnToDesk);
        Box(recenter.transform, _right, 24f, 658f, 552f, 64f);
        LocalizedBinding.Bind(recenter.GetComponentInChildren<TMP_Text>(), "xr.recenter");
        var settings = _right.Find("SettingsBlocker");
        if (settings != null) settings.SetAsLastSibling();
        LayoutResult();
        foreach (var selectable in _left.GetComponentsInChildren<Selectable>(true).Concat(_right.GetComponentsInChildren<Selectable>(true)))
        {
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
            var slider = selectable as Slider;
            if (slider != null && slider.handleRect != null) slider.handleRect.sizeDelta = new Vector2(52f, 52f);
        }
    }

    void Update()
    {
        // The result occupies the left panel; prevent actions on the other desk
        // panel while it is open, without disabling the result's own buttons.
        if (_rightGroup != null) _rightGroup.interactable = !ResultPanel.HasOpenPanel;
        if (_answerHint != null) _answerHint.enabled = _answer == null || !_answer.activeSelf;
        foreach (var page in _readingPages)
        {
            if (page.Text.text == page.LastText) continue;
            page.LastText = page.Text.text;
            page.Scroll.StopMovement();
            page.Scroll.content.anchoredPosition = Vector2.zero;
        }
    }

    static void Background(Transform root)
    {
        var panel = UiFactory.Panel(root, "DeskSurface", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.055f, 0.065f, 0.08f, 0.98f));
        panel.transform.SetAsFirstSibling();
    }

    void BuildExperiment()
    {
        Title(_right, "xr.controls", 24f, 24f, 370f, 52f);
        var top = _left.Find("TopBar");
        Box(top, _left, 16f, 16f, 568f, 96f);
        if (top != null)
        {
            Box(top.Find("Title"), top, 16f, 8f, 536f, 42f);
            Box(top.Find("Mode"), top, 16f, 50f, 536f, 36f);
            Hide(top.Find("Sub"));
            Box(top.Find("Back"), _right, 410f, 20f, 166f, 64f);
        }

        var picker = _left.Find("ShapePicker");
        if (picker != null) MoveGrid(Buttons(picker));
        var toolbar = _left.Find("Toolbar");
        if (toolbar != null)
        {
            MoveGrid(Buttons(toolbar));
            var projection = toolbar.Find("ProjectionRays");
            if (projection != null)
            {
                Box(projection, _right, 24f, _controlY, 552f, 64f);
                _controlY += 76f;
            }
            Hide(toolbar);
        }
        Hide(picker);

        var guide = _left.Find("Guide");
        var data = _left.Find("Data");
        if (guide != null)
        {
            MoveGrid(Buttons(guide));
            Box(guide, _left, 16f, 196f, 568f, 524f);
            Box(guide.Find("Step"), guide, 20f, 20f, 528f, 78f);
            Box(guide.Find("Body"), guide, 20f, 110f, 528f, 390f);
            FitText(guide.Find("Step"), 30f);
            FitText(guide.Find("Body"), 28f);
            if (picker != null) LocalizedBinding.Bind(guide.Find("Body").GetComponent<TMP_Text>(), "xr.netfoldFree");
            MakeReader(guide.Find("Body"));
            Hide(guide.Find("Keys"));
            Hide(guide.Find("ChallengeMsg"));
        }
        if (data != null)
        {
            Box(data, _left, 16f, 196f, 568f, 524f);
            LayoutData(data);
        }
        var guideTab = Tab("xr.guide", 16f, () => ShowPage(guide, data, true));
        var dataTab = Tab("xr.data", 306f, () => ShowPage(guide, data, false));
        guideTab.onClick.AddListener(() => PaintTabs(guideTab, dataTab, true));
        dataTab.onClick.AddListener(() => PaintTabs(guideTab, dataTab, false));
        ShowPage(guide, data, guide != null);
        PaintTabs(guideTab, dataTab, guide != null);
        if (_controlY < 330f) Title(_right, "xr.interactionHint", 24f, _controlY + 24f, 552f, 250f, 26);
    }

    void LayoutData(Transform data)
    {
        var children = Children(data);
        // Keep the data labels on the left; move their actual sliders/buttons
        // to the right. Existing listeners and field references remain intact.
        foreach (var child in children)
        {
            if (child.name == "TempCap" || child.name == "VolCap" || child.name == "FCap")
            {
                Box(child, _right, 24f, _controlY, 552f, 36f);
                FitText(child, 26f);
                _controlY += 40f;
            }
            else if (child.GetComponent<Slider>() != null)
            {
                Box(child, _right, 36f, _controlY, 528f, 72f);
                _controlY += 84f;
            }
        }
        MoveGrid(children.Where(child => child.GetComponent<Button>() != null).ToList());
        float y = 20f;
        foreach (var child in children)
        {
            if (child.parent != data || child.name == "Hairline") continue;
            bool curve = child.GetComponent<RawImage>() != null;
            if (!curve && child.GetComponent<TMP_Text>() == null) continue;
            float height = curve ? 112f : child.name == "State" || child.name == "Rule" ? 68f : 44f;
            Box(child, data, 20f, y, 528f, height);
            if (!curve) FitText(child, 26f);
            y += height + 4f;
        }
    }

    void BuildChallenge()
    {
        Title(_right, "xr.answers", 24f, 24f, 370f, 52f);
        var nav = _left.Find("ChallengeNavigation");
        Box(nav, _left, 16f, 16f, 568f, 108f);
        if (nav != null)
        {
            Box(nav.Find("Module"), nav, 16f, 8f, 536f, 44f);
            Box(nav.Find("Scoring"), nav, 16f, 54f, 536f, 46f);
            Box(nav.Find("Back"), _right, 410f, 20f, 166f, 64f);
            Box(nav.Find("Retry"), _left, 24f, 650f, 552f, 64f);
        }
        var quiz = _left.Find("Quiz");
        Box(quiz, _left, 16f, 136f, 568f, 408f);
        Box(quiz.Find("Question"), quiz, 20f, 16f, 528f, 76f);
        Box(quiz.Find("Body"), quiz, 20f, 104f, 528f, 288f);
        FitText(quiz.Find("Question"), 30f);
        FitText(quiz.Find("Body"), 28f);
        MakeReader(quiz.Find("Body"));
        var choices = quiz.Find("Choices");
        Box(choices, _right, 24f, 208f, 552f, 424f);
        var stack = choices.gameObject.AddComponent<VerticalLayoutGroup>();
        stack.spacing = 12f;
        stack.childControlWidth = stack.childControlHeight = true;
        stack.childForceExpandWidth = stack.childForceExpandHeight = true;
        var answer = quiz.Find("Answer");
        Box(answer, _right, 24f, 110f, 552f, 96f);
        var feedback = _left.Find("ChallengeFeedback");
        Box(feedback, _left, 16f, 556f, 568f, 80f);
        _answer = answer.gameObject;
        _answerHint = Title(_right, "xr.answerHint", 24f, 110f, 552f, 80f, 24);
    }

    void BuildMenu()
    {
        Hide(_left.Find("Wash"));
        Box(_left.Find("Eyebrow"), _left, 24f, 18f, 552f, 32f);
        Box(_left.Find("Title"), _left, 24f, 52f, 552f, 64f);
        Box(_left.Find("Sub"), _left, 24f, 120f, 552f, 70f);
        FitText(_left.Find("Title"), 48f);
        FitText(_left.Find("Sub"), 26f);
        string[] modules = { "NetFold", "ParticleDrift", "LensCraft" };
        for (int i = 0; i < modules.Length; i++)
        {
            var card = _left.Find(modules[i]);
            Box(card, _left, 16f, 204f + i * 170f, 568f, 160f);
            Box(card.Find("Name"), card, 16f, 12f, 536f, 36f);
            Box(card.Find("Blurb"), card, 16f, 50f, 536f, 36f);
            FitText(card.Find("Blurb"), 22f);
            Hide(card.Find("Best"));
            string[] actions = { "Learn", "Free", "Challenge" };
            for (int a = 0; a < actions.Length; a++) Box(card.Find(actions[a]), card, 16f + a * 180f, 90f, 172f, 60f);
        }
        Title(_right, "xr.welcome", 24f, 24f, 552f, 64f, 32);
        Box(_left.Find("Hint"), _right, 24f, 110f, 552f, 212f);
        var hint = _right.Find("Hint").GetComponent<TMP_Text>();
        LocalizedBinding.Bind(hint, "xr.interactionHint");
        FitText(hint.transform, 28f);
        Box(_left.Find("Progress"), _right, 24f, 338f, 552f, 64f);
        Box(_left.Find("Settings"), _right, 24f, 416f, 552f, 80f);
        Box(_left.Find("Quit"), _right, 24f, 548f, 552f, 80f);
        var settings = _left.Find("SettingsBlocker");
        Box(settings, _right, 0f, 0f, 600f, 740f);
        var panel = settings.Find("SettingsPanel");
        Box(panel, settings, 12f, 12f, 576f, 716f);
        Box(panel.Find("Title"), panel, 24f, 24f, 528f, 72f);
        Box(panel.Find("Language"), panel, 24f, 112f, 528f, 56f);
        Box(panel.Find("Chinese"), panel, 24f, 184f, 528f, 80f);
        Box(panel.Find("English"), panel, 24f, 280f, 528f, 80f);
        Box(panel.Find("Current"), panel, 24f, 384f, 528f, 64f);
        Box(panel.Find("Hint"), panel, 24f, 464f, 528f, 124f);
        Box(panel.Find("Close"), panel, 24f, 620f, 528f, 72f);
        // Settings must cover the right panel's footer too when opened.
        settings.SetAsLastSibling();
    }

    void LayoutResult()
    {
        var blocker = _left.Find("ResultBlocker");
        if (blocker == null) return;
        Box(blocker, _left, 0f, 0f, 600f, 740f);
        var panel = blocker.Find("Result");
        Box(panel, blocker, 12f, 12f, 576f, 716f);
        Box(panel.Find("Stars"), panel, 24f, 20f, 528f, 80f);
        Box(panel.Find("Reason"), panel, 24f, 112f, 528f, 92f);
        Box(panel.Find("Progress"), panel, 24f, 214f, 528f, 64f);
        Box(panel.Find("Card"), panel, 24f, 296f, 528f, 292f);
        Box(panel.Find("Again"), panel, 24f, 620f, 258f, 72f);
        Box(panel.Find("Menu"), panel, 294f, 620f, 258f, 72f);
        FitText(panel.Find("Reason"), 28f);
        FitText(panel.Find("Card"), 26f);
        MakeReader(panel.Find("Card"));
        blocker.SetAsLastSibling();
    }

    void MoveGrid(List<Transform> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
            Box(buttons[i], _right, 24f + (i % 2) * 282f, _controlY + (i / 2) * 80f, buttons.Count == 1 ? 552f : 270f, 68f);
        if (buttons.Count > 0) _controlY += Mathf.Ceil(buttons.Count / 2f) * 80f + 8f;
    }

    void MakeReader(Transform target)
    {
        // Standard uGUI scrolling remains driven by XRI poke/ray drag events.
        // Keep the original TMP object so live lesson/question bindings survive.
        var text = target.GetComponent<TMP_Text>();
        var rect = text.rectTransform;
        var area = new GameObject(target.name + " Reading Area", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        Box(area.transform, target.parent, rect.anchoredPosition.x, -rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y);
        area.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.015f);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var view = viewport.GetComponent<RectTransform>();
        view.SetParent(area.transform, false);
        view.anchorMin = Vector2.zero;
        view.anchorMax = Vector2.one;
        view.offsetMin = Vector2.zero;
        view.offsetMax = new Vector2(-52f, 0f);
        rect.SetParent(view, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        var fitter = target.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bar = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var barRect = bar.GetComponent<RectTransform>();
        barRect.SetParent(area.transform, false);
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = Vector2.one;
        barRect.offsetMin = new Vector2(-40f, 0f);
        barRect.offsetMax = Vector2.zero;
        bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.SetParent(bar.transform, false);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = UiFactory.RoundSprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = NetFoldTheme.Hairline;
        var scrollbar = bar.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var scroll = area.GetComponent<ScrollRect>();
        scroll.viewport = view;
        scroll.content = rect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalNormalizedPosition = 1f;
        _readingPages.Add(new ReadingPage { Text = text, Scroll = scroll, LastText = text.text });
    }

    Button Tab(string key, float x, UnityEngine.Events.UnityAction callback)
    {
        var button = UiFactory.Button(_left, key, Loc.Get(key), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, callback);
        Box(button.transform, _left, x, 124f, 278f, 60f);
        LocalizedBinding.Bind(button.GetComponentInChildren<TMP_Text>(), key);
        return button;
    }

    static void ShowPage(Transform guide, Transform data, bool showGuide)
    {
        if (guide != null) guide.gameObject.SetActive(showGuide);
        if (data != null) data.gameObject.SetActive(!showGuide);
    }

    static void PaintTabs(Button guide, Button data, bool showGuide)
    {
        UiFactory.SetButtonColor(guide, showGuide ? NetFoldTheme.Accent : NetFoldTheme.AccentDeep);
        UiFactory.SetButtonColor(data, showGuide ? NetFoldTheme.AccentDeep : NetFoldTheme.Accent);
    }

    static List<Transform> Children(Transform parent) => parent.Cast<Transform>().ToList();
    static List<Transform> Buttons(Transform parent) => Children(parent).Where(child => child.GetComponent<Button>() != null).ToList();
    static void Hide(Transform target) { if (target != null) target.gameObject.SetActive(false); }

    static TMP_Text Title(Transform parent, string key, float x, float y, float width, float height, int font = 28)
    {
        var text = UiFactory.Label(parent, key, Loc.Get(key), font, TextAlignmentOptions.TopLeft,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Box(text.transform, parent, x, y, width, height);
        LocalizedBinding.Bind(text, key);
        return text;
    }

    static void FitText(Transform target, float size)
    {
        if (target == null || !target.TryGetComponent<TMP_Text>(out var text)) return;
        text.enableAutoSizing = true;
        text.fontSize = text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Min(size, 24f);
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    static void Box(Transform target, Transform parent, float x, float top, float width, float height)
    {
        if (target == null) return;
        var rect = (RectTransform)target;
        rect.SetParent(parent, false);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition3D = new Vector3(x, -top, 0f);
    }
}
