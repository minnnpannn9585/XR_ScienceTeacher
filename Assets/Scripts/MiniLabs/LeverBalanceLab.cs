using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A single-variable lever: move a 1 kg load to balance a fixed 2 kg load.</summary>
public class LeverBalanceLab : MiniLabBase
{
    protected override string TitleZh => "杠杆平衡";
    protected override string TitleEn => "Lever Balance";
    protected override string GoalZh => "左侧是 2 kg 砝码，距支点 20 cm。移动右侧 1 kg 砝码，让杠杆保持水平。";
    protected override string GoalEn => "A 2 kg load sits 20 cm left of the pivot. Move the 1 kg load on the right until the lever is level.";

    public const float Gravity = 9.8f;
    public const float LeftMass = 2f;
    public const float RightMass = 1f;
    public const float LeftDistance = 0.2f;
    public float DistanceMeters { get; private set; }
    public float LeftTorque => LeftMass * Gravity * LeftDistance;
    public float RightTorque => RightMass * Gravity * DistanceMeters;
    public bool IsBalanced => Mathf.Abs(LeftTorque - RightTorque) < 0.01f;

    static readonly Color FixedColor = new Color(0.47f, 0.67f, 0.88f);
    static readonly Color MovingColor = new Color(0.94f, 0.7f, 0.32f);
    Transform _beam;
    Transform _rightLoad;
    TMP_Text _distanceLabel;
    TMP_Text _balanceLabel;
    Slider _slider;
    float _tilt;
    Material _metal;
    Material _gold;
    Material _edge;
    Material _glass;

    protected override void BuildExperiment()
    {
        Accent = MovingColor;
        _metal = OpticalVisuals.Own(transform, MRVisualStyle.Metal(MRVisualStyle.WarmWhite, 0.5f, 0.82f));
        _gold = OpticalVisuals.Own(transform, MRVisualStyle.Metal(MRVisualStyle.Gold, 0.6f, 0.82f));
        _edge = OpticalVisuals.Own(transform, MRVisualStyle.Edge(MRVisualStyle.Cyan));
        _glass = OpticalVisuals.Own(transform, MRVisualStyle.Glass(MRVisualStyle.Cyan, 0.18f));
        Visual(PrimitiveType.Cube, "Pivot Base", ExperimentRoot, new Vector3(0f, 0.025f, 0.04f),
            new Vector3(0.2f, 0.05f, 0.28f), _metal);
        Visual(PrimitiveType.Cube, "Pivot Stand", ExperimentRoot, new Vector3(0f, 0.18f, 0.04f),
            new Vector3(0.048f, 0.3f, 0.06f), _metal);
        var axle = Visual(PrimitiveType.Cylinder, "Pivot Axle", ExperimentRoot, new Vector3(0f, 0.34f, 0f),
            new Vector3(0.065f, 0.07f, 0.065f), _gold);
        axle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var cap = Visual(PrimitiveType.Cylinder, "Axle Cap", ExperimentRoot, new Vector3(0f, 0.34f, -0.074f),
            new Vector3(0.049f, 0.003f, 0.049f), _metal);
        cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        OpticalVisuals.Line(ExperimentRoot, "Base Highlight", _edge, 0.0018f,
            new Vector3(-0.1f, 0.051f, -0.1f), new Vector3(0.1f, 0.051f, -0.1f),
            new Vector3(0.1f, 0.051f, 0.18f), new Vector3(-0.1f, 0.051f, 0.18f), new Vector3(-0.1f, 0.051f, -0.1f));

        _beam = new GameObject("Lever Beam").transform;
        _beam.SetParent(ExperimentRoot, false);
        _beam.localPosition = new Vector3(0f, 0.34f, 0f);
        Visual(PrimitiveType.Cube, "Ruler", _beam, Vector3.zero,
            new Vector3(1.1f, 0.028f, 0.07f), _glass);
        OpticalVisuals.Line(_beam, "Ruler Outline", _edge, 0.0018f,
            new Vector3(-0.55f, -0.014f, -0.036f), new Vector3(-0.55f, 0.014f, -0.036f),
            new Vector3(0.55f, 0.014f, -0.036f), new Vector3(0.55f, -0.014f, -0.036f), new Vector3(-0.55f, -0.014f, -0.036f));
        var tickMaterial = OpticalVisuals.Own(transform, MRVisualStyle.Edge(new Color(0.8f, 0.89f, 0.91f)));
        var targetMaterial = OpticalVisuals.Own(transform, MRVisualStyle.Edge(MovingColor));
        OpticalVisuals.Ruler(_beam, "Centimetre Marks", 0.5f, 0.01f, 0.0145f, -0.035f, tickMaterial);
        // Marks are exactly 10 cm apart in the same world units as the torque calculation.
        for (int i = -5; i <= 5; i++)
        {
            float x = i * 0.1f;
            OpticalVisuals.Line(_beam, "Distance Tick", i == 4 ? targetMaterial : tickMaterial, i == 4 ? 0.003f : 0.0018f,
                new Vector3(x, 0.016f, -0.036f), new Vector3(x, -0.016f, -0.036f));
        }
        BuildLoad("Fixed 2 kg", -LeftDistance, FixedColor, 0.105f, "2 kg");
        _rightLoad = BuildLoad("Moving 1 kg", 0.2f, MovingColor, 0.072f, "1 kg");

        Line("Level Reference", ExperimentRoot,
            new[] { new Vector3(-0.55f, 0.34f, 0.08f), new Vector3(0.55f, 0.34f, 0.08f) },
            new Color(0.32f, 0.43f, 0.42f), 0.002f);
        WorldLabel("Fixed Distance", ExperimentRoot, new Vector3(-0.22f, 0.075f, -0.1f), "20 cm", 0.032f);
        _distanceLabel = WorldLabel("Moving Distance", ExperimentRoot, new Vector3(0.32f, 0.075f, -0.1f), "", 0.032f);
        _balanceLabel = WorldLabel("Balance State", ExperimentRoot, new Vector3(0f, 0.59f, 0f), "", 0.038f);
        _slider = AddSlider("右侧砝码距支点 / cm", "Right load distance / cm", 10f, 50f, 20f,
            centimeters => SetDistance(centimeters * 0.01f), true);
    }

    Transform BuildLoad(string name, float x, Color color, float height, string mass)
    {
        var load = new GameObject(name).transform;
        load.SetParent(_beam, false);
        load.localPosition = new Vector3(x, 0f, 0f);
        var finish = OpticalVisuals.Own(transform, MRVisualStyle.Metal(Color.Lerp(color, MRVisualStyle.WarmWhite, 0.38f), 0.6f, 0.84f));
        var edge = OpticalVisuals.Own(transform, MRVisualStyle.Edge(color));
        Visual(PrimitiveType.Cube, "Sliding Collar", load, Vector3.zero,
            new Vector3(0.046f, 0.043f, 0.09f), finish);
        OpticalVisuals.Line(load, "Hanger", edge, 0.004f, new Vector3(0f, -0.014f, 0f), new Vector3(0f, -0.064f, 0f));
        Visual(PrimitiveType.Cylinder, "Mass", load, new Vector3(0f, -0.064f - height * 0.5f, 0f),
            new Vector3(0.09f, height * 0.5f, 0.09f), finish);
        Visual(PrimitiveType.Cylinder, "Mass Top Rim", load, new Vector3(0f, -0.064f, 0f),
            new Vector3(0.093f, 0.002f, 0.093f), _metal);
        Visual(PrimitiveType.Cylinder, "Mass Bottom Rim", load, new Vector3(0f, -0.064f - height, 0f),
            new Vector3(0.093f, 0.002f, 0.093f), _metal);
        var dial = Visual(PrimitiveType.Cylinder, "Collar Dial", load, new Vector3(0f, 0f, -0.049f), new Vector3(0.018f, 0.004f, 0.018f), _gold);
        dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        WorldLabel("Mass Label", load, new Vector3(0f, 0.065f, -0.015f), mass, 0.033f);
        return load;
    }

    static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        => OpticalVisuals.Part(parent, type, name, position, scale, material);

    /// <summary>Distance is in metres; the interactive ruler snaps to whole centimetres.</summary>
    public void SetDistance(float meters)
    {
        DistanceMeters = Mathf.Round(Mathf.Clamp(meters, 0.1f, 0.5f) * 100f) * 0.01f;
        if (_slider != null) _slider.SetValueWithoutNotify(DistanceMeters * 100f);
        if (_rightLoad != null) _rightLoad.localPosition = new Vector3(DistanceMeters, 0f, 0f);
        RefreshExperimentText();
    }

    void Update()
    {
        if (_beam == null) return;
        // A bounded visual cue, not a rigid-body simulation. Greater right torque lowers the right end.
        float target = Mathf.Clamp((LeftTorque - RightTorque) * 8f, -14f, 14f);
        _tilt = Mathf.Lerp(_tilt, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
        _beam.localRotation = Quaternion.Euler(0f, 0f, _tilt);
    }

    protected override void RefreshExperimentText()
    {
        if (_distanceLabel != null) _distanceLabel.text = $"{DistanceMeters * 100f:F0} cm";
        bool balanced = IsBalanced;
        if (_balanceLabel != null)
        {
            _balanceLabel.text = Loc.IsEnglish ? (balanced ? "BALANCED" : "2 kg × 20 cm = 1 kg × ?") : (balanced ? "平衡！" : "2 kg × 20 cm = 1 kg × ?");
            _balanceLabel.color = balanced ? NetFoldTheme.Success : MovingColor;
        }
        SetReadout($"右侧距离：{DistanceMeters * 100f:F0} cm\n水平时力矩（g = 9.8）\n左：{LeftTorque:F2} N·m\n右：{RightTorque:F2} N·m",
            $"Right distance: {DistanceMeters * 100f:F0} cm\nTorques when level (g = 9.8)\nLeft: {LeftTorque:F2} N·m\nRight: {RightTorque:F2} N·m");
        if (balanced)
        {
            SetFeedback("平衡成功！\n2 × 20 = 1 × 40；较轻的砝码需要更长的力臂。\n忽略杆重和摩擦，倾斜为示意。",
                "Balanced!\n2 × 20 = 1 × 40. The lighter load needs a longer arm.\nBeam mass and friction ignored; tilt is illustrative.", true);
        }
        else
        {
            bool leftWins = LeftTorque > RightTorque;
            SetFeedback(leftWins ? "左侧力矩更大：把右侧砝码向外移动。\n平衡条件：F左 × d左 = F右 × d右。\n忽略杆重和摩擦，倾斜为示意。" : "右侧力矩更大：把右侧砝码向支点移动。\n平衡条件：F左 × d左 = F右 × d右。\n忽略杆重和摩擦，倾斜为示意。",
                leftWins ? "Left torque is larger: move the right load outward.\nBalance: F(left) × d(left) = F(right) × d(right).\nBeam mass and friction ignored; tilt is illustrative." : "Right torque is larger: move the right load toward the pivot.\nBalance: F(left) × d(left) = F(right) × d(right).\nBeam mass and friction ignored; tilt is illustrative.");
        }
    }

    public override void ResetExperiment()
    {
        SetDistance(0.2f);
        _tilt = 14f;
        if (_beam != null) _beam.localRotation = Quaternion.Euler(0f, 0f, _tilt);
    }
}
