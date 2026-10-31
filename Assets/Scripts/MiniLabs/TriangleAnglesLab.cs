using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TriangleAnglesLab : MiniLabBase
{
    protected override string TitleZh => "三角形内角和";
    protected override string TitleEn => "Triangle Angles";
    protected override string GoalZh => "把顶点 C 从左移到右，观察三个内角。形状改变时，内角和会改变吗？";
    protected override string GoalEn => "Move vertex C from left to right. As the triangle changes shape, does its angle sum change?";

    static readonly Vector3 A = new Vector3(-0.42f, 0.07f, 0f);
    static readonly Vector3 B = new Vector3(0.42f, 0.07f, 0f);
    Transform _vertex;
    LineRenderer _outline;
    readonly LineRenderer[] _arcs = new LineRenderer[3];
    readonly TMP_Text[] _labels = new TMP_Text[3];
    Slider _slider;
    bool _sawLeft, _sawRight;
    public Vector3 Angles { get; private set; }
    public float VertexX { get; private set; }

    protected override void BuildExperiment()
    {
        Accent = new Color(0.41f, 0.78f, 0.81f);
        var boardColor = new Color(0.075f, 0.13f, 0.16f);
        var board = Part(PrimitiveType.Cube, "Geometry Board", ExperimentRoot, new Vector3(0f, 0.28f, 0.07f), new Vector3(1.12f, 0.55f, 0.025f), boardColor);
        board.GetComponent<Renderer>().sharedMaterial = Unlit(boardColor);
        for (int i = 0; i < 10; i++)
        {
            float x = -0.45f + i * 0.1f;
            Line("Grid", ExperimentRoot, new[] { new Vector3(x, 0.04f, 0.052f), new Vector3(x, 0.53f, 0.052f) }, new Color(0.16f, 0.23f, 0.25f), 0.0015f);
        }
        _outline = Line("Triangle", ExperimentRoot, new[] { A, B, Vector3.zero, A }, Accent, 0.01f);
        Part(PrimitiveType.Sphere, "A", ExperimentRoot, A, Vector3.one * 0.025f, Accent);
        Part(PrimitiveType.Sphere, "B", ExperimentRoot, B, Vector3.one * 0.025f, Accent);
        _vertex = Part(PrimitiveType.Sphere, "Draggable Vertex C", ExperimentRoot, Vector3.zero, Vector3.one * 0.055f, new Color(0.96f, 0.74f, 0.36f)).transform;
        // The primitive's visual collider is removed by Part; a larger target is comfortable in XR.
        var hit = new GameObject("Vertex Target", typeof(SphereCollider), typeof(TriangleVertexHandle));
        hit.transform.SetParent(_vertex, false);
        hit.GetComponent<SphereCollider>().radius = 0.9f;
        hit.GetComponent<TriangleVertexHandle>().Lab = this;
        var colors = new[] { Accent, new Color(0.75f, 0.57f, 0.94f), new Color(0.96f, 0.74f, 0.36f) };
        for (int i = 0; i < 3; i++)
        {
            _arcs[i] = Line("Angle Arc", ExperimentRoot, new Vector3[21], colors[i], 0.005f);
            _labels[i] = WorldLabel("Angle Label", ExperimentRoot, Vector3.zero, "", 0.038f);
            _labels[i].color = colors[i];
        }
        _slider = AddSlider("顶点 C 的左右位置", "Vertex C: left / right", -0.48f, 0.48f, 0f, SetVertex);
        WorldLabel("Vertex Hint", ExperimentRoot, new Vector3(0f, 0.59f, 0f), "A + B + C = 180°", 0.045f);
    }

    public void SetVertex(float x)
    {
        VertexX = Mathf.Clamp(x, -0.48f, 0.48f);
        if (VertexX < -0.3f) _sawLeft = true;
        if (VertexX > 0.3f) _sawRight = true;
        if (_slider != null) _slider.SetValueWithoutNotify(VertexX);
        var c = new Vector3(VertexX, 0.46f, 0f);
        Angles = CalculateAngles(VertexX);
        _vertex.localPosition = c;
        _outline.SetPositions(new[] { A, B, c, A });
        Vector3[] vertices = { A, B, c };
        for (int i = 0; i < 3; i++)
        {
            var center = vertices[i];
            var u = (vertices[(i + 1) % 3] - center).normalized;
            var v = (vertices[(i + 2) % 3] - center).normalized;
            for (int p = 0; p <= 20; p++)
                _arcs[i].SetPosition(p, center + Vector3.Slerp(u, v, p / 20f) * 0.065f - Vector3.forward * 0.006f);
            _labels[i].transform.parent.localPosition = center + (i == 2 ? new Vector3(0f, 0.055f, -0.025f) : new Vector3(i == 0 ? 0.055f : -0.055f, -0.045f, -0.025f));
            _labels[i].text = ((char)('A' + i)) + " " + Angles[i].ToString("F1") + "°";
        }
        RefreshExperimentText();
    }

    public static Vector3 CalculateAngles(float x)
    {
        var c = new Vector3(Mathf.Clamp(x, -0.48f, 0.48f), 0.46f, 0f);
        float a = Vector3.Angle(B - A, c - A);
        float b = Vector3.Angle(A - B, c - B);
        return new Vector3(a, b, 180f - a - b);
    }

    public void DragVertex(Ray ray)
    {
        var plane = new Plane(ExperimentRoot.forward, ExperimentRoot.position);
        if (plane.Raycast(ray, out float enter)) SetVertex(ExperimentRoot.InverseTransformPoint(ray.GetPoint(enter)).x);
    }

    public void DragVertexNear(Vector3 worldPoint) => SetVertex(ExperimentRoot.InverseTransformPoint(worldPoint).x);

    protected override void RefreshExperimentText()
    {
        SetReadout($"∠A = {Angles.x:F1}°\n∠B = {Angles.y:F1}°\n∠C = {Angles.z:F1}°\n内角和 = 180°", $"∠A = {Angles.x:F1}°\n∠B = {Angles.y:F1}°\n∠C = {Angles.z:F1}°\nAngle sum = 180°");
        bool done = _sawLeft && _sawRight;
        SetFeedback(done ? "观察完成！三个角此消彼长，平面三角形的内角和始终为 180°。\n角度显示已四舍五入。" : "拖动金色顶点或右侧滑块，分别到达左右两侧。\n角度显示已四舍五入。", done ? "Observed! The angles change, but a plane triangle always totals 180°.\nDisplayed angles are rounded." : "Drag the gold vertex or the slider to both ends.\nDisplayed angles are rounded.", done);
    }

    public override void ResetExperiment()
    {
        _sawLeft = _sawRight = false;
        SetVertex(0f);
    }
}
