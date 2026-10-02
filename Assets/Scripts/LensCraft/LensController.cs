using UnityEngine;

public class LensController : MonoBehaviour
{
    Transform _glass;
    Transform _focusNear;
    Transform _focusFar;
    Transform _twiceNear;
    Transform _twiceFar;

    public void Build(OpticalBenchController bench)
    {
        _glass = new GameObject("Glass").transform;
        _glass.SetParent(transform, false);
        _glass.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var body = LabFactory.Primitive(PrimitiveType.Cylinder, "Body", _glass, Vector3.zero, Vector3.one, LabFactory.Lit(new Color(0.55f, 0.82f, 1f, 0.38f), true, 0.05f, 0.9f, true, new Color(0.35f, 0.7f, 1f, 0.5f)), false);
        Object.Destroy(body.GetComponent<Collider>());
        var rim = LabFactory.Primitive(PrimitiveType.Cylinder, "Rim", _glass, Vector3.zero, new Vector3(1.08f, 1f, 1.08f), LabFactory.Lit(new Color(0.7f, 0.9f, 1f, 0.9f), true, 0f, 0.4f, true, new Color(0.45f, 0.85f, 1f)), false);
        Object.Destroy(rim.GetComponent<Collider>());
        _focusNear = Mark(bench, "F");
        _focusFar = Mark(bench, "F'");
        _twiceNear = Mark(bench, "2F");
        _twiceFar = Mark(bench, "2F'");
        Apply(0.15f, true);
    }

    public void Apply(float focal, bool guides)
    {
        float thick = Mathf.Lerp(0.055f, 0.02f, Mathf.InverseLerp(0.1f, 0.28f, focal));
        _glass.localScale = new Vector3(0.17f, thick, 0.17f);
        Place(_focusNear, -focal, guides);
        Place(_focusFar, focal, guides);
        Place(_twiceNear, -2f * focal, guides);
        Place(_twiceFar, 2f * focal, guides);
    }

    static Transform Mark(OpticalBenchController bench, string caption)
    {
        var go = LabFactory.Primitive(PrimitiveType.Sphere, caption, bench.transform, Vector3.zero, Vector3.one * 0.018f, LabFactory.Lit(new Color(1f, 0.85f, 0.35f), false, 0f, 0.3f, true, new Color(1f, 0.8f, 0.2f)), false);
        Object.Destroy(go.GetComponent<Collider>());
        LabFactory.WorldLabel(go.transform, caption, new Vector3(0f, 0.04f, 0f));
        return go.transform;
    }

    static void Place(Transform mark, float x, bool visible)
    {
        mark.gameObject.SetActive(visible);
        mark.localPosition = new Vector3(x, 0.13f, 0.1f);
    }
}
