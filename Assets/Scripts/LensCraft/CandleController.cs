using UnityEngine;

public class CandleController : MonoBehaviour, IInteractable
{
    public OpticalBenchController Bench;
    public bool IsSelected { get; private set; }
    public bool CanDrag => Bench == null || !Bench.LockCandle;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    Transform _flame;
    Light _glow;

    public void Build()
    {
        var metal = OpticalVisuals.Own(transform, MRVisualStyle.Metal(MRVisualStyle.WarmWhite));
        var gold = OpticalVisuals.Own(transform, MRVisualStyle.Metal(MRVisualStyle.Gold));
        var waxMaterial = OpticalVisuals.Own(transform, LabFactory.Lit(new Color(0.97f, 0.94f, 0.83f), false, 0f, 0.48f));
        // Keep the original small visual collider as well as the larger grab volume.
        var waxTarget = LabFactory.Primitive(PrimitiveType.Cube, "Wax", transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.028f, 0.07f, 0.028f), waxMaterial, true);
        waxTarget.GetComponent<MeshRenderer>().enabled = false;
        OpticalVisuals.Part(transform, PrimitiveType.Cube, "Candle Carriage", new Vector3(0f, 0.025f, 0f), new Vector3(0.06f, 0.028f, 0.085f), metal);
        OpticalVisuals.Part(transform, PrimitiveType.Cylinder, "Wax Body", new Vector3(0f, 0.106f, 0f), new Vector3(0.026f, 0.068f, 0.026f), waxMaterial);
        OpticalVisuals.Part(transform, PrimitiveType.Cylinder, "Candle Cup", new Vector3(0f, 0.04f, 0f), new Vector3(0.035f, 0.005f, 0.035f), gold);
        var wickMaterial = OpticalVisuals.Own(transform, MRVisualStyle.Edge(new Color(0.2f, 0.17f, 0.14f)));
        OpticalVisuals.Part(transform, PrimitiveType.Cylinder, "Wick", new Vector3(0f, 0.177f, 0f), new Vector3(0.0025f, 0.007f, 0.0025f), wickMaterial);
        var knob = OpticalVisuals.Part(transform, PrimitiveType.Cylinder, "Candle Adjuster", new Vector3(0f, 0.025f, -0.046f), new Vector3(0.016f, 0.005f, 0.016f), gold);
        knob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var flameGo = new GameObject("Flame");
        flameGo.transform.SetParent(transform, false);
        // Align the visible flame with the same object tip used by the ray model.
        flameGo.transform.localPosition = new Vector3(0f, OpticalBenchController.AxisY + OpticalBenchController.ObjectHeight - 0.02f, 0f);
        _flame = flameGo.transform;
        var flameMesh = OpticalVisuals.FlameMesh(transform);
        var outerMaterial = OpticalVisuals.Own(transform, MRVisualStyle.Edge(new Color(1f, 0.57f, 0.12f, 0.68f)));
        var innerMaterial = OpticalVisuals.Own(transform, MRVisualStyle.Edge(new Color(1f, 0.94f, 0.62f)));
        OpticalVisuals.MeshPart(_flame, "Amber Flame", flameMesh, outerMaterial, Vector3.zero, new Vector3(0.015f, 0.033f, 0.015f));
        OpticalVisuals.MeshPart(_flame, "Bright Flame Core", flameMesh, innerMaterial, new Vector3(0f, 0.002f, 0f), new Vector3(0.007f, 0.024f, 0.007f));
        _glow = flameGo.AddComponent<Light>();
        _glow.type = LightType.Point;
        _glow.range = 0.45f;
        _glow.intensity = 1.1f;
        _glow.color = new Color(1f, 0.72f, 0.28f);
        var box = gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.10f, 0f);
        box.size = new Vector3(0.09f, 0.20f, 0.09f);
    }

    public void Place(float objectDistance)
    {
        transform.localPosition = new Vector3(-objectDistance, 0f, 0f);
    }

    public void SetLit(bool on)
    {
        if (_flame != null)
        {
            _flame.gameObject.SetActive(on);
        }

        if (_glow != null)
        {
            _glow.intensity = on ? 1.1f : 0.05f;
        }
    }

    public void OnSelect()
    {
        IsSelected = true;
    }

    public void OnDeselect()
    {
        IsSelected = false;
    }

    public void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
    }

    public void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        if (!CanDrag || Bench == null)
        {
            return;
        }

        if (!Bench.RayOnBench(pointerRay, out Vector3 local))
        {
            return;
        }

        float u = Mathf.Clamp(-local.x, 0.08f, 0.72f);
        Bench.ObjectDistance = u;
        Place(u);
    }

    public void OnDragEnd()
    {
    }

    public void Rotate(Vector2 delta)
    {
    }

    public void Scale(float delta)
    {
    }
}
