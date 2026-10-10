using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LensCraftUI : MonoBehaviour
{
    public ResultPanel Result { get; private set; }
    public string AnswerText => _quiz != null ? _quiz.AnswerText : _answer != null ? _answer.text : string.Empty;

    TMP_Text _mode;
    TMP_Text _guideTitle;
    TMP_Text _guideBody;
    TMP_Text _u;
    TMP_Text _v;
    TMP_Text _f;
    TMP_Text _mag;
    TMP_Text _nature;
    TMP_Text _rule;
    TMP_Text _status;
    Slider _focal;
    TMP_InputField _answer;
    GameObject _answerRoot;
    Transform _choices;
    ChallengeScreenQuiz _quiz;
    readonly List<Button> _choiceButtons = new List<Button>();
    bool _mute;

    public void Build(OpticalBenchController lab, GameMode mode)
    {
        if (mode == GameMode.Challenge)
        {
            var quizCanvas = UiFactory.CreateOverlay("LensCraftHUD", transform);
            _quiz = gameObject.AddComponent<ChallengeScreenQuiz>();
            _quiz.Build(quizCanvas.transform);
            _quiz.BindSubmit(lab.SubmitAnswer);
            _guideTitle = _quiz.TitleText;
            _guideBody = _quiz.BodyText;
            _quiz.BuildNavigation(quizCanvas.transform, Loc.Get("menu.lens"), () => lab.Toolbar("提示"), lab.RestartChallenge);
            _status = _quiz.BuildStatus(quizCanvas.transform);
            BuildResult(quizCanvas.transform, lab);
            return;
        }

        var canvas = UiFactory.CreateOverlay("LensCraftHUD", transform);
        var root = canvas.transform;
        var top = UiFactory.Panel(root, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -92f), new Vector2(-16f, -12f), NetFoldTheme.Glass);
        UiFactory.Label(top.transform, "Title", Loc.Branded("LensCraft", mode), 26, TextAlignmentOptions.Left, new Vector2(0f, 0.42f), new Vector2(0.62f, 1f), new Vector2(18f, 0f), new Vector2(-8f, -4f));
        UiFactory.Label(top.transform, "Sub", Loc.Get("lens.sub"), 18, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.46f, 0.48f), new Vector2(18f, 4f), new Vector2(-8f, 0f), NetFoldTheme.TextDim);
        _mode = UiFactory.Label(top.transform, "Mode", mode == GameMode.Free ? Loc.Get("common.free") : mode == GameMode.Learn ? Loc.Get("common.lesson") : Loc.Get("common.challengeMode"), 20, TextAlignmentOptions.Center, new Vector2(0.62f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero, NetFoldTheme.Hairline);
        UiFactory.Button(top.transform, "Back", Loc.Get("common.back"), new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero, SceneLoader.LoadMainMenu);

        if (mode != GameMode.Learn)
        {
            string[] tools = mode == GameMode.Free
                ? new[] { "重置", "显示光路" }
                : new[] { "重置", "自动演示", "显示光路", "辅助线", "景深", "提示" };
            float half = tools.Length <= 2 ? 100f : tools.Length <= 3 ? 130f : 250f;
            var left = UiFactory.Panel(root, "Toolbar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -half), new Vector2(210f, half), NetFoldTheme.Glass);
            for (int i = 0; i < tools.Length; i++)
            {
                float y = -16f - i * 78f;
                string action = tools[i];
                UiFactory.Button(left.transform, action, Loc.Action(action), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, y - 64f), new Vector2(-12f, y), () => lab.Toolbar(action));
            }
        }

        float dataBottom = mode == GameMode.Learn ? -50f : -390f;
        float dataTop = mode == GameMode.Learn ? 250f : 300f;
        var right = UiFactory.Panel(root, "Data", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-360f, dataBottom), new Vector2(-16f, dataTop), NetFoldTheme.Glass);
        _u = Row(right.transform, "U", Loc.Get("lens.u.empty"), -8f);
        _v = Row(right.transform, "V", Loc.Get("lens.v.empty"), -40f);
        _f = Row(right.transform, "F", Loc.Get("lens.f.empty"), -72f);
        _mag = Row(right.transform, "M", Loc.Get("lens.mag.empty"), -104f);
        _nature = Row(right.transform, "Nature", Loc.Get("lens.nature.empty"), -136f);
        _rule = Row(right.transform, "Rule", Loc.Get("lens.rule.empty"), -176f);
        _rule.rectTransform.offsetMin = new Vector2(16f, -230f);
        _rule.rectTransform.offsetMax = new Vector2(-16f, -168f);
        _status = Row(right.transform, "Status", "", -236f);
        if (mode != GameMode.Learn)
        {
            UiFactory.Label(right.transform, "FCap", Loc.Get("lens.fcap"), 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -292f), new Vector2(-16f, -268f));
            _focal = MakeSlider(right.transform, "Focal", -332f, -292f, 10f, 28f, 15f, cm =>
            {
                if (!_mute)
                {
                    lab.SetFocalFromSlider(cm);
                }
            });
            UiFactory.Button(right.transform, "Lamp", Loc.Get("lens.lamp"), new Vector2(0.04f, 1f), new Vector2(0.48f, 1f), new Vector2(0f, -384f), new Vector2(0f, -340f), lab.ToggleLight, new Color(0.55f, 0.32f, 0.08f, 1f));
            UiFactory.Button(right.transform, "Ray1", Loc.Get("lens.ray1"), new Vector2(0.52f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -384f), new Vector2(0f, -340f), () => lab.ToggleRay(0), new Color(0.55f, 0.16f, 0.16f, 1f));
            UiFactory.Button(right.transform, "Ray2", Loc.Get("lens.ray2"), new Vector2(0.04f, 1f), new Vector2(0.48f, 1f), new Vector2(0f, -436f), new Vector2(0f, -392f), () => lab.ToggleRay(1), new Color(0.12f, 0.42f, 0.22f, 1f));
            UiFactory.Button(right.transform, "Ray3", Loc.Get("lens.ray3"), new Vector2(0.52f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -436f), new Vector2(0f, -392f), () => lab.ToggleRay(2), new Color(0.16f, 0.28f, 0.55f, 1f));
        }

        bool guided = mode == GameMode.Learn;
        var bottom = UiFactory.Panel(root, "Guide", new Vector2(0f, 0f), new Vector2(1f, 0f), guided ? new Vector2(16f, 14f) : new Vector2(230f, 14f), guided ? new Vector2(-376f, 248f) : new Vector2(-376f, 168f), NetFoldTheme.Glass);
        _guideTitle = UiFactory.Label(bottom.transform, "Step", mode == GameMode.Free ? Loc.Get("common.free") : Loc.Get("lens.beat1.title"), 24, TextAlignmentOptions.Left, new Vector2(0f, guided ? 0.78f : 0.62f), new Vector2(0.72f, 1f), new Vector2(16f, 0f), new Vector2(-8f, -4f));
        _guideBody = UiFactory.Label(bottom.transform, "Body", mode == GameMode.Free ? Loc.Get("lens.free.body") : "", 18, TextAlignmentOptions.TopLeft, new Vector2(0f, guided ? 0.08f : 0.28f), new Vector2(0.72f, guided ? 0.76f : 0.64f), new Vector2(16f, 0f), new Vector2(-8f, 0f));
        _choices = bottom.transform;
        if (guided)
        {
            UiFactory.Button(bottom.transform, "Prev", Loc.Get("common.prev"), new Vector2(0.76f, 0.55f), new Vector2(0.87f, 0.92f), Vector2.zero, Vector2.zero, lab.PrevStep);
            UiFactory.Button(bottom.transform, "Next", Loc.Get("common.next"), new Vector2(0.88f, 0.55f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero, lab.NextStep);
        }

        string keys = mode == GameMode.Challenge
            ? Loc.Get("lens.keys.challenge")
            : mode == GameMode.Learn
                ? Loc.Get("common.keys.lesson")
                : Loc.Get("lens.keys.free");
        UiFactory.Label(bottom.transform, "Keys", keys, 16, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.74f, 0.24f), new Vector2(16f, 4f), new Vector2(-8f, 0f));
        BuildAnswer(bottom.transform);

        BuildResult(root, lab);
    }

    void BuildResult(Transform root, OpticalBenchController lab)
    {
        Result = gameObject.AddComponent<ResultPanel>();
        Result.Build(root);
        Result.KnowledgeKey = "knowledge.lens";
        Result.Retry = lab.RestartChallenge;
    }

    public void SetLesson(string title, string body)
    {
        if (_mode != null) _mode.text = Loc.Get("common.lesson");
        if (_guideTitle != null) _guideTitle.text = title;
        if (_guideBody != null) _guideBody.text = body;
    }

    public void SetLearn(LensStep step)
    {
        if (_mode != null) _mode.text = Loc.Get("common.lesson");
        if (_guideTitle != null) _guideTitle.text = Title(step);
        if (_guideBody != null) _guideBody.text = Body(step);
    }

    public void SetFree()
    {
        if (_mode != null) _mode.text = Loc.Get("common.free");
        if (_guideTitle != null) _guideTitle.text = Loc.Get("common.free");
        if (_guideBody != null) _guideBody.text = Loc.Get("lens.free.body");
    }

    public void SetChallenge(string title, string body)
    {
        if (_quiz != null)
        {
            _quiz.SetQuestion(title, body);
            return;
        }

        if (_mode != null) _mode.text = Loc.Get("common.challengeMode");
        if (_guideTitle != null) _guideTitle.text = title;
        if (_guideBody != null) _guideBody.text = body;
    }

    public void ShowChoices(string[] labels, Action<int> picked)
    {
        if (_quiz != null)
        {
            _quiz.ShowChoices(labels, picked, new Color(0.12f, 0.28f, 0.48f, 0.96f));
            return;
        }

        ClearChoices();
        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            float x = 0.02f + (i % 2) * 0.36f;
            float y = i < 2 ? 0.34f : 0.08f;
            Button button = UiFactory.Button(_choices, "Choice" + i, labels[i], new Vector2(x, y), new Vector2(x + 0.34f, y + 0.22f), Vector2.zero, Vector2.zero, () => picked(idx), new Color(0.12f, 0.28f, 0.48f, 0.96f));
            _choiceButtons.Add(button);
        }
    }

    public void MarkChoice(int index)
    {
        if (_quiz != null)
        {
            _quiz.MarkChoice(index);
            return;
        }

        if (index < 0 || index >= _choiceButtons.Count)
        {
            return;
        }

        var image = _choiceButtons[index].GetComponent<Image>();
        if (image != null)
        {
            image.color = NetFoldTheme.Error;
        }
    }

    public void ClearChoices()
    {
        if (_quiz != null)
        {
            _quiz.ClearChoices();
            return;
        }

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (_choiceButtons[i] != null)
            {
                Destroy(_choiceButtons[i].gameObject);
            }
        }

        _choiceButtons.Clear();
    }

    public void SetAnswerVisible(bool visible)
    {
        if (_quiz != null)
        {
            _quiz.SetAnswerVisible(visible);
            return;
        }

        if (_answerRoot != null)
        {
            _answerRoot.SetActive(visible);
        }

        if (visible && _answer != null)
        {
            _answer.text = string.Empty;
        }
    }

    public void SetFocal(float centimeters, bool interactable)
    {
        if (_focal == null)
        {
            return;
        }

        _mute = true;
        _focal.SetValueWithoutNotify(centimeters);
        _focal.interactable = interactable;
        _mute = false;
    }

    public void Refresh(string u, string v, string f, string mag, string nature, string rule, string status)
    {
        if (_u != null) _u.text = u;
        if (_v != null) _v.text = v;
        if (_f != null) _f.text = f;
        if (_mag != null) _mag.text = mag;
        if (_nature != null) _nature.text = nature;
        if (_rule != null) _rule.text = rule;
        if (_status != null) _status.text = status;
    }

    void BuildAnswer(Transform parent)
    {
        _answerRoot = new GameObject("Answer", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = _answerRoot.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.74f, 0.08f);
        rt.anchorMax = new Vector2(0.98f, 0.42f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = _answerRoot.GetComponent<Image>();
        bg.sprite = UiFactory.RoundSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.12f);
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rt, false);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.font = UiFactory.DefaultFont;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 4f);
        textRt.offsetMax = new Vector2(-10f, -4f);
        _answer = _answerRoot.GetComponent<TMP_InputField>();
        _answer.textViewport = rt;
        _answer.textComponent = text;
        _answer.pointSize = 22;
        _answer.contentType = TMP_InputField.ContentType.DecimalNumber;
        _answerRoot.SetActive(false);
    }

    static TMP_Text Row(Transform parent, string name, string text, float top)
    {
        return UiFactory.Label(parent, name, text, 20, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, top - 28f), new Vector2(-16f, top));
    }

    static string Title(LensStep step)
    {
        switch (step)
        {
            case LensStep.Bench: return Loc.Get("lens.ui1.title");
            case LensStep.Rays: return Loc.Get("lens.ui2.title");
            case LensStep.Distance: return Loc.Get("lens.ui3.title");
            default: return Loc.Get("lens.ui4.title");
        }
    }

    static string Body(LensStep step)
    {
        switch (step)
        {
            case LensStep.Bench: return Loc.Get("lens.ui1.body");
            case LensStep.Rays: return Loc.Get("lens.ui2.body");
            case LensStep.Distance: return Loc.Get("lens.ui3.body");
            default: return Loc.Get("lens.ui4.body");
        }
    }

    static Slider MakeSlider(Transform parent, string name, float yMin, float yMax, float min, float max, float value, UnityEngine.Events.UnityAction<float> changed)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(16f, yMin);
        rt.offsetMax = new Vector2(-16f, yMax);
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = new Vector2(0f, 0.3f);
        bgRt.anchorMax = new Vector2(1f, 0.7f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = UiFactory.RoundSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.14f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fa = fillArea.GetComponent<RectTransform>();
        fa.SetParent(rt, false);
        fa.anchorMin = new Vector2(0f, 0.3f);
        fa.anchorMax = new Vector2(1f, 0.7f);
        fa.offsetMin = new Vector2(8f, 0f);
        fa.offsetMax = new Vector2(-8f, 0f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fr = fill.GetComponent<RectTransform>();
        fr.SetParent(fa, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.sizeDelta = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = UiFactory.RoundSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = NetFoldTheme.Accent;
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var ha = handleArea.GetComponent<RectTransform>();
        ha.SetParent(rt, false);
        ha.anchorMin = Vector2.zero;
        ha.anchorMax = Vector2.one;
        ha.offsetMin = new Vector2(8f, 0f);
        ha.offsetMax = new Vector2(-8f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var hr = handle.GetComponent<RectTransform>();
        hr.SetParent(ha, false);
        hr.sizeDelta = new Vector2(18f, 18f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.sprite = UiFactory.RoundSprite;
        handleImg.color = NetFoldTheme.Ivory;
        var slider = root.GetComponent<Slider>();
        slider.fillRect = fr;
        slider.handleRect = hr;
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.onValueChanged.AddListener(changed);
        slider.value = value;
        return slider;
    }
}
