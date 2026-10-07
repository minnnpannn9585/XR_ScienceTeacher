using UnityEngine;

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

    public void Build(BeakerRole role)
    {
        Role = role;
        Action = LabAction.Beaker;
        DisplayName = NameOf(role);
        var glassMat = LabFactory.Lit(new Color(0.93f, 0.96f, 0.98f, 0.14f), true, 0f, 0.97f, true, new Color(0.55f, 0.68f, 0.8f, 0.05f));
        glassMat.renderQueue = 3000;
        var glass = LabFactory.Primitive(PrimitiveType.Cylinder, "Glass", transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.124f, 0.06f, 0.124f), glassMat, true);
        _glass = glass.GetComponent<MeshRenderer>();

        _liquidMat = LabFactory.Lit(BaseTint(), true, 0.02f, 0.55f, true, BaseTint() * 0.2f);
        _liquidMat.renderQueue = 3100;
        var liquid = LabFactory.Primitive(PrimitiveType.Cylinder, "Liquid", transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.1f, 0.035f, 0.1f), _liquidMat, false);
        _liquid = liquid.transform;

        var outlineMat = LabFactory.Lit(new Color(1f, 0.16f, 0.16f, 0.38f), true, 0f, 0.2f, true, new Color(1f, 0.2f, 0.15f));
        outlineMat.renderQueue = 3180;
        var outline = LabFactory.Primitive(PrimitiveType.Cylinder, "Outline", transform, new Vector3(0f, 0.062f, 0f), new Vector3(0.14f, 0.065f, 0.14f), outlineMat, false);
        _outline = outline.GetComponent<MeshRenderer>();
        _outline.enabled = false;

        _blobMat = LabFactory.Lit(new Color(0.8f, 0.02f, 0.16f, 0.5f), true, 0f, 0.3f, true, new Color(0.8f, 0.05f, 0.15f));
        _blobMat.renderQueue = 3150;
        var blob = LabFactory.Primitive(PrimitiveType.Sphere, "DyeBlob", transform, new Vector3(0f, 0.07f, 0f), Vector3.one * 0.012f, _blobMat, false);
        _blob = blob.transform;
        _blobRenderer = blob.GetComponent<MeshRenderer>();
        _blobRenderer.enabled = false;

        var backMat = LabFactory.Lit(NetFoldTheme.Ivory, false, 0.08f, 0.35f);
        LabFactory.Primitive(PrimitiveType.Cube, "ThermoBack", transform, new Vector3(0.09f, 0.05f, 0f), new Vector3(0.012f, 0.1f, 0.012f), backMat, false);
        _thermoMat = LabFactory.Lit(new Color(0.3f, 0.55f, 1f), false, 0.1f, 0.45f, true, new Color(0.3f, 0.55f, 1f));
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
            _glass.enabled = !micro;
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
        Color dye = new Color(0.78f, 0.03f, 0.16f, 0.8f);
        Color c = _green ? new Color(0.18f, 0.82f, 0.36f, 0.78f) : Color.Lerp(BaseTint(), dye, Dye01);
        if (_micro)
        {
            c.a = 0.16f;
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
                return new Color(0.95f, 0.62f, 0.48f, 0.62f);
            case BeakerRole.Cold:
                return new Color(0.42f, 0.68f, 0.98f, 0.6f);
            case BeakerRole.Alcohol:
                return new Color(0.96f, 0.88f, 0.42f, 0.55f);
            default:
                return new Color(0.5f, 0.75f, 0.96f, 0.58f);
        }
    }

    static string NameOf(BeakerRole role)
    {
        switch (role)
        {
            case BeakerRole.Cold: return "冷水";
            case BeakerRole.Hot: return "热水";
            case BeakerRole.Alcohol: return "酒精";
            case BeakerRole.Water: return "水";
            default: return "水";
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
}
