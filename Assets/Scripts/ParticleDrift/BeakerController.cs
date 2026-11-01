using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum BeakerRole
{
    Single,
    Cold,
    Hot,
    Water,
    Alcohol
}

public class BeakerController : LabProp
{
    public BeakerRole Role;
    public float Temperature = 25f;
    public float Dye01;
    public bool Diffusing;
    public bool Uniform;
    public float DiffusionElapsed;
    public float DiffusionDuration = 8f;
    public MolecularMotionController Motion { get; private set; }
    public ConcentrationHeatmap Heatmap { get; private set; }
    public string DisplayName { get; private set; }
    public const float InnerRadius = 0.05f;

    bool _green;
    bool _micro;
    bool _uniformConsumed;
    float _liquidHeight = 0.07f;
    Material _liquidMat;
    Material _blobMat;
    Material _thermoMat;
    Transform _liquid;
    Transform _blob;
    Transform _thermoFill;
    Renderer _glass;
    Renderer _outline;
    Renderer _blobRenderer;
    Transform _meniscus;
    readonly List<Material> _materials = new List<Material>();
    Mesh _vesselMesh;

    public void Build(BeakerRole role)
    {
        Role = role;
        Action = LabAction.Beaker;
        DisplayName = NameOf(role);
        var glassMat = Own(MRVisualStyle.Glass(new Color(0.72f, 0.94f, 0.97f), 0.12f));
        glassMat.SetFloat("_Cull", 0f);
        glassMat.renderQueue = 3140;
        _vesselMesh = ChemistryVesselGeometry.OpenWall(0.062f, 0.12f);
        var glass = new GameObject("Glass", typeof(MeshFilter), typeof(MeshRenderer), typeof(CapsuleCollider));
        glass.transform.SetParent(transform, false);
        glass.GetComponent<MeshFilter>().sharedMesh = _vesselMesh;
        var collider = glass.GetComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.06f, 0f);
        collider.radius = 0.062f;
        collider.height = 0.12f;
        _glass = glass.GetComponent<MeshRenderer>();
        _glass.sharedMaterial = glassMat;
        _glass.shadowCastingMode = ShadowCastingMode.Off;
        _glass.receiveShadows = false;
        var rimMaterial = Own(MRVisualStyle.Edge(new Color(0.68f, 0.94f, 0.98f)));
        Ring("Glass Lip", 0.062f, 0.12f, rimMaterial, 0.0013f);
        Ring("Glass Base", 0.061f, 0.003f, rimMaterial, 0.0012f);
        var glassBase = LabFactory.Primitive(PrimitiveType.Cylinder, "Glass Bottom", transform,
            new Vector3(0f, 0.002f, 0f), new Vector3(0.124f, 0.002f, 0.124f), glassMat, false);
        glassBase.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var etching = Own(MRVisualStyle.Edge(new Color(0.78f, 0.86f, 0.88f)));
        for (int i = 1; i <= 4; i++)
        {
            float y = 0.02f + i * 0.018f;
            Line("Glass Graduation", new[] { new Vector3(-0.015f, y, -0.0605f), new Vector3(i % 2 == 0 ? 0.013f : 0.003f, y, -0.0605f) }, etching, 0.0008f);
        }

        _liquidMat = Own(LabFactory.Lit(BaseTint(), true, 0f, 0.88f));
        _liquidMat.renderQueue = 3100;
        var liquid = LabFactory.Primitive(PrimitiveType.Cylinder, "Liquid", transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.1f, 0.035f, 0.1f), _liquidMat, false);
        _liquid = liquid.transform;
        liquid.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        _meniscus = Ring("Liquid Meniscus", 0.0495f, _liquidHeight + 0.0005f, rimMaterial, 0.0009f).transform;

        var outlineMat = Own(MRVisualStyle.Edge(new Color(1f, 0.22f, 0.20f)));
        outlineMat.renderQueue = 3180;
        _outline = Ring("Outline", 0.066f, 0.004f, outlineMat, 0.002f);
        _outline.enabled = false;

        _blobMat = Own(LabFactory.Lit(new Color(0.82f, 0.09f, 0.42f, 0.42f), true, 0f, 0.5f));
        _blobMat.renderQueue = 3150;
        var blob = LabFactory.Primitive(PrimitiveType.Sphere, "DyeBlob", transform, new Vector3(0f, 0.07f, 0f), Vector3.one * 0.012f, _blobMat, false);
        _blob = blob.transform;
        _blobRenderer = blob.GetComponent<MeshRenderer>();
        _blobRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _blobRenderer.enabled = false;

        var backMat = Own(MRVisualStyle.Metal(MRVisualStyle.WarmWhite, 0.2f, 0.72f));
        LabFactory.Primitive(PrimitiveType.Cube, "ThermoBack", transform, new Vector3(0.09f, 0.05f, 0f), new Vector3(0.012f, 0.1f, 0.012f), backMat, false);
        _thermoMat = Own(MRVisualStyle.Edge(new Color(0.3f, 0.55f, 1f)));
        var fill = LabFactory.Primitive(PrimitiveType.Cube, "ThermoFill", transform, new Vector3(0.09f, 0.02f, 0f), new Vector3(0.01f, 0.04f, 0.01f), _thermoMat, false);
        _thermoFill = fill.transform;

        Motion = gameObject.AddComponent<MolecularMotionController>();
        Heatmap = gameObject.AddComponent<ConcentrationHeatmap>();
        Motion.Build(InnerRadius * 0.86f, _liquidHeight);
        Heatmap.Build(InnerRadius);
        LabFactory.WorldLabel(transform, DisplayName, new Vector3(0f, 0.16f, 0f));
        SetTemperature(Temperature);
        ApplyLiquid();
    }

    public void SetTemperature(float celsius)
    {
        Temperature = Mathf.Clamp(celsius, 0f, 100f);
        UpdateThermo();
    }

    public void BeginDiffusion(int count)
    {
        _green = false;
        Uniform = false;
        _uniformConsumed = false;
        Diffusing = true;
        DiffusionElapsed = 0f;
        DiffusionDuration = Mathf.Max(0.8f, TemperatureController.DurationFor(Temperature));
        Dye01 = 0f;
        Motion.BeginDye(count);
        ApplyLiquid();
    }

    public void Accelerate()
    {
        DiffusionDuration = DiffusionElapsed + 1.15f;
        Motion.Boost(1.8f);
    }

    public void ClearDye()
    {
        Diffusing = false;
        Uniform = false;
        _uniformConsumed = false;
        _green = false;
        Dye01 = 0f;
        DiffusionElapsed = 0f;
        if (Motion != null)
        {
            Motion.ClearDye();
        }

        if (Heatmap != null)
        {
            Heatmap.Clear();
        }

        ApplyLiquid();
    }

    public void SetGreen(bool on)
    {
        _green = on;
        ApplyLiquid();
    }

    public void ShowOutline(bool on)
    {
        if (_outline != null)
        {
            _outline.enabled = on;
        }
    }

    public void SetLiquidHeight(float height)
    {
        _liquidHeight = Mathf.Clamp(height, 0.03f, 0.12f);
        if (_liquid != null)
        {
            _liquid.localScale = new Vector3(0.1f, _liquidHeight * 0.5f, 0.1f);
            _liquid.localPosition = new Vector3(0f, _liquidHeight * 0.5f, 0f);
        }

        if (_blob != null)
        {
            _blob.localPosition = new Vector3(0f, _liquidHeight, 0f);
        }
        if (_meniscus != null) _meniscus.localPosition = Vector3.up * (_liquidHeight - 0.07f);

        if (Motion != null)
        {
            Motion.SetHeight(_liquidHeight);
        }

        if (Heatmap != null)
        {
            Heatmap.SetHeight(_liquidHeight);
        }
    }

    public void ApplyView(bool micro)
    {
        _micro = micro;
        if (_glass != null)
        {
            // A faint wall and fine rim retain the vessel boundary in micro view.
            _glass.enabled = true;
        }

        if (Motion != null)
        {
            Motion.SetRendered(micro);
        }

        ApplyLiquid();
    }

    public void SetHeatmap(bool on)
    {
        if (Heatmap != null)
        {
            Heatmap.SetVisible(on && gameObject.activeInHierarchy);
        }
    }

    public bool ConsumeUniform()
    {
        if (!Uniform || _uniformConsumed)
        {
            return false;
        }

        _uniformConsumed = true;
        return true;
    }

    public void Tick(float dt)
    {
        if (Motion != null)
        {
            Motion.Tick(dt, Temperature);
        }

        if (Diffusing)
        {
            DiffusionElapsed += dt;
            float p = DiffusionDuration <= 0.05f ? 1f : Mathf.Clamp01(DiffusionElapsed / DiffusionDuration);
            Dye01 = p;
            Motion.SetSpread(p);
            if (p >= 1f)
            {
                Diffusing = false;
                Uniform = true;
                Dye01 = 1f;
            }
        }

        ApplyLiquid();
        if (Heatmap != null && Heatmap.IsVisible && Motion != null)
        {
            Heatmap.Paint(Motion.DyeXZ, Motion.DyeCount);
        }
    }

    void UpdateThermo()
    {
        if (_thermoFill == null)
        {
            return;
        }

        float h = Mathf.Lerp(0.02f, 0.1f, Temperature / 100f);
        _thermoFill.localScale = new Vector3(0.01f, h, 0.01f);
        _thermoFill.localPosition = new Vector3(0.09f, h * 0.5f, 0f);
        Color c = Color.Lerp(new Color(0.25f, 0.5f, 1f), new Color(1f, 0.28f, 0.12f), Temperature / 100f);
        SetColor(_thermoMat, c);
    }

    void ApplyLiquid()
    {
        // Fuchsin stays magenta; the rim remains neutral to preserve the color lesson.
        Color dye = new Color(0.78f, 0.04f, 0.38f, 0.65f);
        Color c = _green ? new Color(0.18f, 0.82f, 0.36f, 0.62f) : Color.Lerp(BaseTint(), dye, Dye01);
        if (_micro)
        {
            c.a = 0.08f;
        }

        SetColor(_liquidMat, c);
        if (_blobRenderer == null)
        {
            return;
        }

        _blobRenderer.enabled = !_micro && Dye01 > 0.03f && !_green;
        float s = Mathf.Lerp(0.014f, InnerRadius * 2.2f, Dye01);
        _blob.localScale = new Vector3(s, s * 0.28f, s);
        Color blob = dye;
        blob.a = Mathf.Lerp(0.62f, 0.05f, Dye01);
        SetColor(_blobMat, blob);
    }

    Color BaseTint()
    {
        switch (Role)
        {
            case BeakerRole.Hot:
                return new Color(0.95f, 0.71f, 0.58f, 0.30f);
            case BeakerRole.Cold:
                return new Color(0.48f, 0.78f, 0.98f, 0.30f);
            case BeakerRole.Alcohol:
                return new Color(0.96f, 0.88f, 0.52f, 0.28f);
            default:
                return new Color(0.55f, 0.85f, 0.96f, 0.28f);
        }
    }

    static string NameOf(BeakerRole role)
    {
        switch (role)
        {
            case BeakerRole.Cold: return Loc.Get("beaker.cold");
            case BeakerRole.Hot: return Loc.Get("beaker.hot");
            case BeakerRole.Alcohol: return Loc.Get("beaker.alcohol");
            case BeakerRole.Water: return Loc.Get("beaker.water");
            default: return Loc.Get("beaker.water");
        }
    }

    static void SetColor(Material mat, Color color)
    {
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
    }

    Material Own(Material material) { _materials.Add(material); return material; }

    LineRenderer Ring(string name, float radius, float height, Material material, float width)
    {
        var points = new Vector3[64];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i * Mathf.PI * 2f / points.Length;
            points[i] = new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
        }
        var line = Line(name, points, material, width);
        line.loop = true;
        return line;
    }

    LineRenderer Line(string name, Vector3[] points, Material material, float width)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = line.endWidth = width;
        line.numCornerVertices = 2;
        line.numCapVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    void OnDestroy()
    {
        foreach (var material in _materials) if (material != null) Destroy(material);
        if (_vesselMesh != null) Destroy(_vesselMesh);
    }
}

/// <summary>A smooth open vessel wall, without the opaque-looking top cap of a cylinder.</summary>
public static class ChemistryVesselGeometry
{
    public static Mesh OpenWall(float radius, float height, int segments = 64)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            var n = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            vertices[i * 2] = n * radius;
            vertices[i * 2 + 1] = n * radius + Vector3.up * height;
            normals[i * 2] = normals[i * 2 + 1] = n;
            uv[i * 2] = new Vector2((float)i / segments, 0f);
            uv[i * 2 + 1] = new Vector2((float)i / segments, 1f);
            if (i == segments) continue;
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        var mesh = new Mesh { name = "Open Laboratory Glass", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }
}
