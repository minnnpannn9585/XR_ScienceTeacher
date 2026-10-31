using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A fresh sample is shown on every selection; this is not an acid/base mixing simulation.
/// Concept and schematic colors: American Chemical Society, Red Cabbage Indicator:
/// https://www.acs.org/education/activities/red-cabbage-indicator.html
/// Real colors depend on concentration and composition; no precise pH is inferred from RGB.
/// </summary>
public class IndicatorLab : MiniLabBase
{
    protected override string TitleZh => "酸碱探色";
    protected override string TitleEn => "Acid-base Colors";
    protected override string GoalZh => "选择三种样品，比较紫甘蓝指示剂的颜色。每次切换都会换用全新样品。";
    protected override string GoalEn => "Compare cabbage indicator colors in three samples. Each selection uses a fresh sample.";

    public int SelectedSample { get; private set; } = -1;
    public int ObservedCount => (_observed[0] ? 1 : 0) + (_observed[1] ? 1 : 0) + (_observed[2] ? 1 : 0);

    static readonly string[] NamesZh = { "白醋", "纯水", "肥皂水" };
    static readonly string[] NamesEn = { "Vinegar", "Pure water", "Soap solution" };
    static readonly string[] StatesZh = { "酸性 · 粉红色", "中性 · 紫色", "碱性 · 绿色" };
    static readonly string[] StatesEn = { "Acidic / pink", "Neutral / purple", "Basic / green" };
    static readonly string[] PhZh = { "pH < 7", "pH 约为 7", "pH > 7" };
    static readonly string[] PhEn = { "pH < 7", "pH about 7", "pH > 7" };
    static readonly Color[] SampleColors =
    {
        new Color(0.93f, 0.21f, 0.43f),
        new Color(0.56f, 0.32f, 0.86f),
        new Color(0.22f, 0.76f, 0.49f)
    };

    readonly bool[] _observed = new bool[3];
    readonly Button[] _buttons = new Button[3];
    readonly Transform[] _bottles = new Transform[3];
    readonly TMP_Text[] _bottleLabels = new TMP_Text[3];
    readonly Renderer[] _markers = new Renderer[3];
    Material _liquid;
    TMP_Text _sampleLabel;
    Color _targetColor;

    protected override void BuildExperiment()
    {
        var slate = new Color(0.12f, 0.17f, 0.22f);
        Part(PrimitiveType.Cube, "Sample Tray", ExperimentRoot,
            new Vector3(0f, 0.012f, 0.18f), new Vector3(1.02f, 0.024f, 0.24f), slate);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            _buttons[i] = AddAction(NamesZh[i], NamesEn[i], () => SelectSample(index));
            var bottle = new GameObject("Sample " + i).transform;
            bottle.SetParent(ExperimentRoot, false);
            bottle.localPosition = new Vector3((i - 1) * 0.36f, 0.024f, 0.19f);
            _bottles[i] = bottle;
            // Capped samples remain separate, making the replacement operation explicit.
            Part(PrimitiveType.Cylinder, "Bottle", bottle, new Vector3(0f, 0.082f, 0f),
                new Vector3(0.12f, 0.082f, 0.12f), new Color(0.75f, 0.84f, 0.88f, 0.32f));
            Part(PrimitiveType.Cylinder, "Cap", bottle, new Vector3(0f, 0.178f, 0f),
                new Vector3(0.086f, 0.018f, 0.086f), slate);
            Part(PrimitiveType.Cube, "Bottle Label", bottle, new Vector3(0f, 0.083f, -0.058f),
                new Vector3(0.09f, 0.072f, 0.012f), new Color(0.88f, 0.85f, 0.76f));
            _bottleLabels[i] = WorldLabel("Sample Name", bottle, new Vector3(0f, 0.232f, 0f), "", 0.035f);
            var marker = Part(PrimitiveType.Cylinder, "Observed Marker", bottle, new Vector3(0f, 0.006f, 0f),
                new Vector3(0.164f, 0.006f, 0.164f), slate);
            _markers[i] = marker.GetComponent<Renderer>();
        }

        var vessel = new GameObject("Indicator Vessel").transform;
        vessel.SetParent(ExperimentRoot, false);
        vessel.localPosition = new Vector3(0f, 0f, -0.135f);
        Part(PrimitiveType.Cylinder, "Beaker Foot", vessel, new Vector3(0f, 0.014f, 0f),
            new Vector3(0.33f, 0.014f, 0.33f), slate);
        Part(PrimitiveType.Cylinder, "Beaker Glass", vessel, new Vector3(0f, 0.164f, 0f),
            new Vector3(0.304f, 0.14f, 0.304f), new Color(0.79f, 0.9f, 0.94f, 0.1f));
        var liquid = Part(PrimitiveType.Cylinder, "Indicator Liquid", vessel, new Vector3(0f, 0.127f, 0f),
            new Vector3(0.286f, 0.098f, 0.286f), SampleColors[1]);
        _liquid = Unlit(SampleColors[1]);
        liquid.GetComponent<Renderer>().sharedMaterial = _liquid;
        Ring("Beaker Rim", vessel, 0.305f, 0.153f, new Color(0.68f, 0.8f, 0.86f));
        Ring("Liquid Surface", vessel, 0.226f, 0.141f, new Color(0.85f, 0.83f, 0.92f));
        for (int i = 0; i < 4; i++)
        {
            float y = 0.075f + i * 0.05f;
            Line("Volume Tick", vessel, new[] { new Vector3(-0.04f, y, -0.151f), new Vector3(0.025f, y, -0.151f) },
                new Color(0.85f, 0.89f, 0.92f), 0.003f);
        }
        _sampleLabel = WorldLabel("Current Sample", vessel, new Vector3(0f, 0.382f, 0f), "", 0.046f);
    }

    void Ring(string name, Transform parent, float height, float radius, Color color)
    {
        var points = new Vector3[49];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / (points.Length - 1);
            points[i] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }
        Line(name, parent, points, color, 0.004f);
    }

    public void SelectSample(int index)
    {
        if (index < 0 || index >= _observed.Length || _liquid == null) return;
        SelectedSample = index;
        _observed[index] = true;
        _targetColor = SampleColors[index];
        RefreshExperimentText();
        PaintSelections();
    }

    public override void ResetExperiment()
    {
        SelectedSample = -1;
        for (int i = 0; i < _observed.Length; i++) _observed[i] = false;
        _targetColor = SampleColors[1];
        if (_liquid != null) UrpMaterialUtil.SetColor(_liquid, _targetColor);
        RefreshExperimentText();
        PaintSelections();
    }

    void Update()
    {
        if (_liquid == null) return;
        // A short visible change connects the sample selection to its color response.
        Color current = Color.Lerp(_liquid.color, _targetColor, 1f - Mathf.Exp(-5f * Time.deltaTime));
        UrpMaterialUtil.SetColor(_liquid, current);
    }

    void PaintSelections()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] != null)
                UiFactory.SetButtonColor(_buttons[i], i == SelectedSample ? new Color(0.3f, 0.36f, 0.4f) : NetFoldTheme.AccentDeep);
            if (_markers[i] != null)
                UrpMaterialUtil.SetColor(_markers[i].sharedMaterial, _observed[i] ? SampleColors[i] : new Color(0.16f, 0.2f, 0.24f));
            if (_bottles[i] != null)
                _bottles[i].localPosition = new Vector3((i - 1) * 0.36f, i == SelectedSample ? 0.044f : 0.024f, 0.19f);
        }
    }

    protected override void RefreshExperimentText()
    {
        bool english = Loc.IsEnglish;
        for (int i = 0; i < _bottleLabels.Length; i++)
            if (_bottleLabels[i] != null) _bottleLabels[i].text = english ? NamesEn[i] : NamesZh[i];
        if (_sampleLabel != null)
            _sampleLabel.text = SelectedSample < 0 ? (english ? "Cabbage indicator" : "紫甘蓝指示剂") :
                (english ? NamesEn[SelectedSample] : NamesZh[SelectedSample]);

        if (SelectedSample < 0)
        {
            SetReadout("已观察 0 / 3\n选择一份样品，看看颜色如何变化。",
                "Observed 0 / 3\nChoose a sample to see its color response.");
            SetFeedback("杯中已备好紫甘蓝指示剂。\n颜色为示意，实际会受浓度与成分影响。",
                "The cabbage indicator is ready.\nColors are schematic; concentration and composition affect real results.");
            return;
        }

        int sample = SelectedSample;
        SetReadout(NamesZh[sample] + " · " + PhZh[sample] + "\n" + StatesZh[sample] + "\n已观察 " + ObservedCount + " / 3",
            NamesEn[sample] + " / " + PhEn[sample] + "\n" + StatesEn[sample] + "\nObserved " + ObservedCount + " / 3");
        bool complete = ObservedCount == 3;
        SetFeedback(complete ? "观察完成！指示剂可以帮助辨别酸碱性。\n颜色为示意，不能据此精确测定 pH。" :
            "试试其余样品，寻找颜色与酸碱性的联系。\n颜色为示意，不能据此精确测定 pH。",
            complete ? "Complete! Indicators help identify acids and bases.\nThese schematic colors do not measure exact pH." :
            "Try the remaining samples and compare.\nThese schematic colors do not measure exact pH.", complete);
    }
}
