using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParticleDriftUI : MonoBehaviour
{
    public ResultPanel Result { get; private set; }
    public TMP_Text GuideTitle;
    public TMP_Text GuideBody;

    TMP_Text _mode;
    TMP_Text _temp;
    TMP_Text _speed;
    TMP_Text _time;
    TMP_Text _conc;
    TMP_Text _count;
    TMP_Text _state;
    TMP_Text _volume;
    Slider _tempSlider;
    Slider _volumeSlider;
    GameObject _curveGo;
    Transform _choices;
    ChallengeScreenQuiz _quiz;
    bool _mute;

    public void Build(ExperimentController lab, Texture curve, GameMode mode)
    {
        if (mode == GameMode.Challenge)
        {
            var quizCanvas = UiFactory.CreateOverlay("ParticleDriftHUD", transform);
            _quiz = gameObject.AddComponent<ChallengeScreenQuiz>();
            _quiz.Build(quizCanvas.transform);
            GuideTitle = _quiz.TitleText;
            GuideBody = _quiz.BodyText;
            _quiz.BuildNavigation(quizCanvas.transform, Loc.Get("menu.drift"), () => lab.Toolbar("提示"), lab.RestartChallenge);
            _state = _quiz.BuildStatus(quizCanvas.transform);
            BuildResult(quizCanvas.transform, lab);
            return;
        }

        var canvas = UiFactory.CreateOverlay("ParticleDriftHUD", transform);
        var root = canvas.transform;

        var top = UiFactory.Panel(root, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -92f), new Vector2(-16f, -12f), NetFoldTheme.Glass);
        UiFactory.Label(top.transform, "Title", Loc.Branded("ParticleDrift", mode), 26, TextAlignmentOptions.Left, new Vector2(0f, 0.45f), new Vector2(0.62f, 1f), new Vector2(18f, 0f), new Vector2(-8f, -4f));
        UiFactory.Label(top.transform, "Sub", Loc.Get("drift.sub"), 18, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.4f, 0.48f), new Vector2(18f, 4f), new Vector2(-8f, 0f), NetFoldTheme.TextDim);
        _mode = UiFactory.Label(top.transform, "Mode", mode == GameMode.Free ? Loc.Get("common.free") : mode == GameMode.Learn ? Loc.Get("common.lesson") : Loc.Get("common.challengeMode"), 20, TextAlignmentOptions.Center, new Vector2(0.62f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero, NetFoldTheme.Hairline);
        UiFactory.Button(top.transform, "Back", Loc.Get("common.back"), new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero, SceneLoader.LoadMainMenu);

        if (mode != GameMode.Learn)
        {
            string[] tools = mode == GameMode.Free
                ? new[] { "重置", "宏观/微观", "浓度热力图" }
                : new[] { "重置", "自动演示", "宏观/微观", "浓度热力图", "温度曲线", "提示" };
            float half = mode == GameMode.Free ? 130f : 250f;
            var left = UiFactory.Panel(root, "Toolbar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -half), new Vector2(210f, half), NetFoldTheme.Glass);
            for (int i = 0; i < tools.Length; i++)
            {
                string action = tools[i];
                float y = -16f - i * 78f;
                UiFactory.Button(left.transform, action, Loc.Action(action), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, y - 64f), new Vector2(-12f, y), () => lab.Toolbar(action));
            }
        }

        float panelBottom = mode == GameMode.Learn ? -140f : mode == GameMode.Challenge ? -420f : -260f;
        var right = UiFactory.Panel(root, "Data", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-348f, panelBottom), new Vector2(-16f, 300f), NetFoldTheme.Glass);
        _temp = Row(right.transform, "Temp", Loc.Get("drift.temp.empty"), -12f);
        _speed = Row(right.transform, "Speed", Loc.Get("drift.speed.empty"), -48f);
        _time = Row(right.transform, "Time", Loc.Get("drift.time.empty"), -84f);
        _conc = Row(right.transform, "Conc", Loc.Get("drift.conc.empty"), -120f);
        _count = Row(right.transform, "Count", Loc.Format("drift.count", "--"), -156f);
        _state = Row(right.transform, "State", Loc.Get("drift.state.empty"), -192f);
        _state.rectTransform.offsetMin = new Vector2(16f, -268f);
        _state.rectTransform.offsetMax = new Vector2(-16f, -192f);

        _curveGo = new GameObject("Curve", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var crt = _curveGo.GetComponent<RectTransform>();
        crt.SetParent(right.transform, false);
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.offsetMin = new Vector2(16f, -378f);
        crt.offsetMax = new Vector2(-16f, -274f);
        var raw = _curveGo.GetComponent<RawImage>();
        raw.texture = curve;
        raw.color = Color.white;
        UiFactory.Label(right.transform, "CurveKey", Loc.Get("drift.curveKey"), 16, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -400f), new Vector2(-16f, -376f));

        if (mode != GameMode.Learn)
        {
            UiFactory.Label(right.transform, "TempCap", Loc.Get("drift.tempCap"), 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -428f), new Vector2(-16f, -404f));
            _tempSlider = MakeSlider(right.transform, "TempSlider", -468f, -428f, 0f, 100f, 25f, v =>
            {
                if (!_mute)
                {
                    lab.SetTemperatureFromSlider(v);
                }
            });
        }

        if (mode == GameMode.Free)
        {
            ActionButton(right.transform, "Drop", Loc.Get("drift.drop"), 0.04f, 0.96f, -520f, -476f, lab.DropDye, new Color(0.55f, 0.1f, 0.28f, 1f));
        }
        else if (mode == GameMode.Challenge)
        {
            _volume = UiFactory.Label(right.transform, "VolCap", Loc.Format("drift.volume", 60), 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -500f), new Vector2(-16f, -476f));
            _volumeSlider = MakeSlider(right.transform, "VolSlider", -540f, -500f, 0f, 100f, 60f, v => lab.SetVolumeFromSlider(v));
            ActionButton(right.transform, "Heat", Loc.Get("drift.heat"), 0.04f, 0.48f, -588f, -544f, lab.HeatSelected, new Color(0.55f, 0.18f, 0.12f, 1f));
            ActionButton(right.transform, "Cool", Loc.Get("drift.cool"), 0.52f, 0.96f, -588f, -544f, lab.CoolSelected, new Color(0.12f, 0.28f, 0.55f, 1f));
            ActionButton(right.transform, "Drop", Loc.Get("drift.drop"), 0.04f, 0.48f, -640f, -596f, lab.DropDye, new Color(0.55f, 0.1f, 0.28f, 1f));
            ActionButton(right.transform, "Gas", Loc.Get("drift.gas"), 0.52f, 0.96f, -640f, -596f, lab.OpenGas, NetFoldTheme.AccentDeep);
            ActionButton(right.transform, "Run", Loc.Get("drift.run"), 0.04f, 0.48f, -692f, -648f, lab.StartDiffusion, new Color(0.12f, 0.42f, 0.28f, 1f));
            ActionButton(right.transform, "Ion", Loc.Get("drift.ion"), 0.52f, 0.96f, -692f, -648f, lab.StartElectrolysis, new Color(0.28f, 0.22f, 0.48f, 1f));
        }

        bool guided = mode == GameMode.Learn;
        var bottom = UiFactory.Panel(root, "Guide", new Vector2(0f, 0f), new Vector2(1f, 0f), guided ? new Vector2(16f, 14f) : new Vector2(230f, 14f), guided ? new Vector2(-364f, 248f) : new Vector2(-364f, 188f), NetFoldTheme.Glass);
        GuideTitle = UiFactory.Label(bottom.transform, "Step", mode == GameMode.Free ? Loc.Get("common.free") : Loc.Get("drift.beat1.title"), 24, TextAlignmentOptions.Left, new Vector2(0f, guided ? 0.78f : 0.74f), new Vector2(0.72f, 1f), new Vector2(16f, 0f), new Vector2(-8f, -4f));
        GuideBody = UiFactory.Label(bottom.transform, "Body", mode == GameMode.Free ? Loc.Get("drift.free.body") : "", 18, TextAlignmentOptions.TopLeft, new Vector2(0f, guided ? 0.08f : 0.46f), new Vector2(0.72f, guided ? 0.76f : 0.76f), new Vector2(16f, 0f), new Vector2(-8f, 0f));
        _choices = bottom.transform;
        if (guided)
        {
            UiFactory.Button(bottom.transform, "Prev", Loc.Get("common.prev"), new Vector2(0.76f, 0.55f), new Vector2(0.87f, 0.92f), Vector2.zero, Vector2.zero, lab.PrevStep);
            UiFactory.Button(bottom.transform, "Next", Loc.Get("common.next"), new Vector2(0.88f, 0.55f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero, lab.NextStep);
        }

        string keys = guided
            ? Loc.Get("common.keys.lesson")
            : Loc.Get("drift.keys.free");
        UiFactory.Label(bottom.transform, "Keys", keys, 16, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.72f, guided ? 0.1f : 0.18f), new Vector2(16f, 4f), new Vector2(-8f, 0f));

        BuildResult(root, lab);
    }

    void BuildResult(Transform root, ExperimentController lab)
    {
        Result = gameObject.AddComponent<ResultPanel>();
        Result.Build(root);
        Result.KnowledgeKey = "knowledge.drift";
        Result.Retry = lab.RestartChallenge;
    }

    public void SetMode(string mode)
    {
        if (_mode != null)
        {
            _mode.text = mode;
        }
    }

    public void SetLesson(string title, string body)
    {
        SetMode(Loc.Get("common.lesson"));
        if (GuideTitle != null) GuideTitle.text = title;
        if (GuideBody != null) GuideBody.text = body;
    }

    public void SetLearn(DriftStep step)
    {
        SetMode(Loc.Get("common.lesson"));
        if (GuideTitle != null)
        {
            GuideTitle.text = Title(step);
        }

        if (GuideBody != null)
        {
            GuideBody.text = Body(step);
        }
    }

    public void SetFree()
    {
        SetMode(Loc.Get("common.free"));
        if (GuideTitle != null)
        {
            GuideTitle.text = Loc.Get("common.free");
        }

        if (GuideBody != null)
        {
            GuideBody.text = Loc.Get("drift.free.body");
        }
    }

    public void SetChallengeGuide(string title, string body)
    {
        SetMode(Loc.Get("common.challengeMode"));
        if (_quiz != null)
        {
            _quiz.SetQuestion(title, body);
            return;
        }

        if (GuideTitle != null)
        {
            GuideTitle.text = title;
        }

        if (GuideBody != null)
        {
            GuideBody.text = body;
        }
    }

    public void ShowChoices(string[] labels, Action<int> picked)
    {
        if (_quiz != null)
        {
            _quiz.ShowChoices(labels, picked);
            return;
        }

        ClearChoices();
        float width = 0.22f;
        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            float x = 0.02f + i * 0.24f;
            UiFactory.Button(_choices, "Choice" + i, labels[i], new Vector2(x, 0.2f), new Vector2(x + width, 0.44f), Vector2.zero, Vector2.zero, () => picked(idx), new Color(0.42f, 0.12f, 0.28f, 0.96f));
        }
    }

    public void ClearChoices()
    {
        if (_quiz != null)
        {
            _quiz.ClearChoices();
            return;
        }

        if (_choices == null)
        {
            return;
        }

        for (int i = _choices.childCount - 1; i >= 0; i--)
        {
            var child = _choices.GetChild(i);
            if (child.name.StartsWith("Choice"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void SetTempSlider(float celsius)
    {
        if (_tempSlider == null)
        {
            return;
        }

        _mute = true;
        _tempSlider.SetValueWithoutNotify(celsius);
        _mute = false;
    }

    public void SetCurveVisible(bool visible)
    {
        if (_curveGo != null)
        {
            _curveGo.SetActive(visible);
        }
    }

    public void Refresh(string temp, string speed, string time, string conc, string count, string state, string volume)
    {
        if (_temp != null) _temp.text = temp;
        if (_speed != null) _speed.text = speed;
        if (_time != null) _time.text = time;
        if (_conc != null) _conc.text = conc;
        if (_count != null) _count.text = count;
        if (_state != null) _state.text = state;
        if (_volume != null) _volume.text = volume;
    }

    static TMP_Text Row(Transform parent, string name, string text, float top)
    {
        return UiFactory.Label(parent, name, text, 20, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, top - 32f), new Vector2(-16f, top));
    }

    static void ActionButton(Transform parent, string name, string text, float xMin, float xMax, float yMin, float yMax, UnityEngine.Events.UnityAction click, Color color)
    {
        UiFactory.Button(parent, name, text, new Vector2(xMin, 1f), new Vector2(xMax, 1f), new Vector2(0f, yMin), new Vector2(0f, yMax), click, color);
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

    static string Title(DriftStep step)
    {
        switch (step)
        {
            case DriftStep.Motion: return Loc.Get("drift.ui1.title");
            case DriftStep.Temperature: return Loc.Get("drift.ui2.title");
            case DriftStep.Diffusion: return Loc.Get("drift.ui3.title");
            default: return Loc.Get("drift.ui4.title");
        }
    }

    static string Body(DriftStep step)
    {
        switch (step)
        {
            case DriftStep.Motion:
                return Loc.Get("drift.ui1.body");
            case DriftStep.Temperature:
                return Loc.Get("drift.ui2.body");
            case DriftStep.Diffusion:
                return Loc.Get("drift.ui3.body");
            default:
                return Loc.Get("drift.ui4.body");
        }
    }
}
