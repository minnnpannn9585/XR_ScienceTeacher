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
    bool _mute;

    public void Build(ExperimentController lab, Texture curve, GameMode mode)
    {
        var canvas = UiFactory.CreateOverlay("ParticleDriftHUD", transform);
        var root = canvas.transform;

        var top = UiFactory.Panel(root, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -92f), new Vector2(-16f, -12f), NetFoldTheme.Glass);
        UiFactory.Label(top.transform, "Title", mode == GameMode.Free ? "ParticleDrift · 自由实验" : mode == GameMode.Learn ? "ParticleDrift · 讲解" : "ParticleDrift · 挑战", 26, TextAlignmentOptions.Left, new Vector2(0f, 0.45f), new Vector2(0.62f, 1f), new Vector2(18f, 0f), new Vector2(-8f, -4f));
        UiFactory.Label(top.transform, "Sub", "分子热运动与扩散", 18, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.4f, 0.48f), new Vector2(18f, 4f), new Vector2(-8f, 0f));
        _mode = UiFactory.Label(top.transform, "Mode", mode == GameMode.Free ? "自由实验" : mode == GameMode.Learn ? "讲解" : "挑战模式", 20, TextAlignmentOptions.Center, new Vector2(0.62f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
        UiFactory.Button(top.transform, "Back", "返回", new Vector2(0.84f, 0.16f), new Vector2(0.985f, 0.84f), Vector2.zero, Vector2.zero, SceneLoader.LoadMainMenu);

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
                UiFactory.Button(left.transform, action, action, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, y - 64f), new Vector2(-12f, y), () => lab.Toolbar(action));
            }
        }

        float panelBottom = mode == GameMode.Learn ? -140f : mode == GameMode.Challenge ? -420f : -260f;
        var right = UiFactory.Panel(root, "Data", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-348f, panelBottom), new Vector2(-16f, 300f), NetFoldTheme.Glass);
        _temp = Row(right.transform, "Temp", "温度  --", -12f);
        _speed = Row(right.transform, "Speed", "粒子平均速度  --", -48f);
        _time = Row(right.transform, "Time", "扩散时间  --", -84f);
        _conc = Row(right.transform, "Conc", "浓度分布  --", -120f);
        _count = Row(right.transform, "Count", "分子数  --", -156f);
        _state = Row(right.transform, "State", "当前状态  --", -192f);
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
        UiFactory.Label(right.transform, "CurveKey", "青线温度    橙线扩散进度", 16, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -400f), new Vector2(-16f, -376f));

        if (mode != GameMode.Learn)
        {
            UiFactory.Label(right.transform, "TempCap", "温度", 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -428f), new Vector2(-16f, -404f));
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
            ActionButton(right.transform, "Drop", "滴入品红", 0.04f, 0.96f, -520f, -476f, lab.DropDye, new Color(0.55f, 0.1f, 0.28f, 1f));
        }
        else if (mode == GameMode.Challenge)
        {
            _volume = UiFactory.Label(right.transform, "VolCap", "滴加体积  60%", 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -500f), new Vector2(-16f, -476f));
            _volumeSlider = MakeSlider(right.transform, "VolSlider", -540f, -500f, 0f, 100f, 60f, v => lab.SetVolumeFromSlider(v));
            ActionButton(right.transform, "Heat", "加热", 0.04f, 0.48f, -588f, -544f, lab.HeatSelected, new Color(0.55f, 0.18f, 0.12f, 1f));
            ActionButton(right.transform, "Cool", "冷却", 0.52f, 0.96f, -588f, -544f, lab.CoolSelected, new Color(0.12f, 0.28f, 0.55f, 1f));
            ActionButton(right.transform, "Drop", "滴入品红", 0.04f, 0.48f, -640f, -596f, lab.DropDye, new Color(0.55f, 0.1f, 0.28f, 1f));
            ActionButton(right.transform, "Gas", "打开气体瓶", 0.52f, 0.96f, -640f, -596f, lab.OpenGas, NetFoldTheme.AccentDeep);
            ActionButton(right.transform, "Run", "开始扩散", 0.04f, 0.48f, -692f, -648f, lab.StartDiffusion, new Color(0.12f, 0.42f, 0.28f, 1f));
            ActionButton(right.transform, "Ion", "开始电解", 0.52f, 0.96f, -692f, -648f, lab.StartElectrolysis, new Color(0.28f, 0.22f, 0.48f, 1f));
        }

        bool guided = mode == GameMode.Learn;
        var bottom = UiFactory.Panel(root, "Guide", new Vector2(0f, 0f), new Vector2(1f, 0f), guided ? new Vector2(16f, 14f) : new Vector2(230f, 14f), guided ? new Vector2(-364f, 248f) : new Vector2(-364f, 188f), NetFoldTheme.Glass);
        GuideTitle = UiFactory.Label(bottom.transform, "Step", mode == GameMode.Free ? "自由实验" : "步骤 1 / 5    分子在运动", 24, TextAlignmentOptions.Left, new Vector2(0f, guided ? 0.78f : 0.74f), new Vector2(0.72f, 1f), new Vector2(16f, 0f), new Vector2(-8f, -4f));
        GuideBody = UiFactory.Label(bottom.transform, "Body", mode == GameMode.Free ? "可切换微观、调节温度、把品红滴进冷水或热水，也可以把酒精烧杯拖进水里。" : "", 18, TextAlignmentOptions.TopLeft, new Vector2(0f, guided ? 0.08f : 0.46f), new Vector2(0.72f, guided ? 0.76f : 0.76f), new Vector2(16f, 0f), new Vector2(-8f, 0f));
        _choices = bottom.transform;
        if (guided)
        {
            UiFactory.Button(bottom.transform, "Prev", "上一步", new Vector2(0.76f, 0.55f), new Vector2(0.87f, 0.92f), Vector2.zero, Vector2.zero, lab.PrevStep);
            UiFactory.Button(bottom.transform, "Next", "下一步", new Vector2(0.88f, 0.55f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero, lab.NextStep);
        }

        string keys = guided
            ? "右键旋转视角    滚轮缩放    上一步 / 下一步    Esc 返回"
            : "右键旋转  滚轮缩放  WASD平移  Tab微观  R重置  Enter确认  Esc返回";
        UiFactory.Label(bottom.transform, "Keys", keys, 16, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.72f, guided ? 0.1f : 0.18f), new Vector2(16f, 4f), new Vector2(-8f, 0f));

        Result = gameObject.AddComponent<ResultPanel>();
        Result.Build(root);
        Result.Knowledge = "知识卡\n分子在不停地做无规则运动。\n温度越高，分子运动越剧烈，扩散越快。\n扩散是分子从高浓度向低浓度运动。\n分子之间有间隔，酒精与水混合后总体积小于两者之和。";
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
        SetMode("讲解");
        if (GuideTitle != null) GuideTitle.text = title;
        if (GuideBody != null) GuideBody.text = body;
    }

    public void SetLearn(DriftStep step)
    {
        SetMode("讲解");
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
        SetMode("自由实验");
        if (GuideTitle != null)
        {
            GuideTitle.text = "自由实验";
        }

        if (GuideBody != null)
        {
            GuideBody.text = "可切换微观、调节温度、把品红滴进冷水或热水，也可以把酒精烧杯拖进水里。";
        }
    }

    public void SetChallengeGuide(string title, string body)
    {
        SetMode("挑战模式");
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
        handleImg.color = Color.white;

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
            case DriftStep.Motion: return "步骤 1 / 4    认识分子运动";
            case DriftStep.Temperature: return "步骤 2 / 4    温度对分子运动的影响";
            case DriftStep.Diffusion: return "步骤 3 / 4    品红扩散实验";
            default: return "步骤 4 / 4    分子间有间隔";
        }
    }

    static string Body(DriftStep step)
    {
        switch (step)
        {
            case DriftStep.Motion:
                return "按 Tab 切换微观视图，观察烧杯里的水分子不停运动。";
            case DriftStep.Temperature:
                return "拖动温度滑块。温度升高时分子运动加快，温度计和速度曲线一起变化。";
            case DriftStep.Diffusion:
                return "把品红滴管拖到冷水或热水烧杯上松开。可打开热力图，对比扩散快慢。";
            default:
                return "把酒精烧杯拖到水烧杯上。混合后总体积小于两者之和。";
        }
    }
}
