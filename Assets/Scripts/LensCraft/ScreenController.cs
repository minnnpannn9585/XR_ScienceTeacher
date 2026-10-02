using DG.Tweening;
using UnityEngine;

public class ScreenController : MonoBehaviour, IInteractable
{
    public OpticalBenchController Bench;
    public bool IsSelected { get; private set; }
    public bool CanDrag => Bench == null || (!Bench.LockScreen && !Bench.AutoScreen);
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    Transform _glyph;
    Transform _blurA;
    Transform _blurB;
    MeshRenderer _plate;
    Material _plateMat;
    Color _plateColor;

    public void Build()
    {
        _plateColor = new Color(0.92f, 0.94f, 0.96f, 0.82f);
        var plate = LabFactory.Primitive(PrimitiveType.Cube, "Plate", transform, Vector3.zero, new Vector3(0.02f, 0.24f, 0.18f), LabFactory.Lit(_plateColor, true, 0f, 0.2f), true);
        _plate = plate.GetComponent<MeshRenderer>();
        _plateMat = _plate.material;
        _glyph = BuildGlyph("Image");
        _blurA = BuildGlyph("BlurA");
        _blurB = BuildGlyph("BlurB");
        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(0.08f, 0.26f, 0.2f);
    }

    public void Place(float screenDistance)
    {
        transform.localPosition = new Vector3(screenDistance, OpticalBenchController.AxisY, 0f);
    }

    public void ShowImage(float height, float magnification, float sharpness, bool visible)
    {
        bool show = visible && magnification > 0.01f;
        _glyph.gameObject.SetActive(show);
        _blurA.gameObject.SetActive(show && sharpness < 0.82f);
        _blurB.gameObject.SetActive(show && sharpness < 0.82f);
        if (!show)
        {
            return;
        }

        float sign = height < 0f ? -1f : 1f;
        float scale = Mathf.Clamp(magnification, 0.15f, 3.2f);
        _glyph.localPosition = new Vector3(0.02f, height, 0f);
        _glyph.localScale = new Vector3(scale, sign * scale, scale);
        float spread = (1f - sharpness) * 0.03f;
        _blurA.localPosition = _glyph.localPosition + new Vector3(0f, spread, 0.01f);
        _blurB.localPosition = _glyph.localPosition + new Vector3(0f, -spread, -0.01f);
        _blurA.localScale = _glyph.localScale * 1.08f;
        _blurB.localScale = _glyph.localScale * 1.08f;
        SetGlyphAlpha(_glyph, Mathf.Lerp(0.25f, 0.95f, sharpness));
        SetGlyphAlpha(_blurA, (1f - sharpness) * 0.35f);
        SetGlyphAlpha(_blurB, (1f - sharpness) * 0.35f);
        if (_plateMat != null)
        {
            Color c = Color.Lerp(new Color(0.75f, 0.78f, 0.82f, 0.55f), new Color(0.95f, 0.98f, 1f, 0.9f), sharpness);
            UrpMaterialUtil.SetColor(_plateMat, c);
        }
    }

    public void Flash()
    {
        if (_plate != null)
        {
            _plate.transform.DOPunchScale(Vector3.one * 0.08f, 0.35f, 6, 0.5f);
        }
    }

    public void OnSelect() => IsSelected = true;
    public void OnDeselect() => IsSelected = false;
    public void OnDragStart(Vector3 worldPoint, Ray pointerRay) { }
    public void OnDragEnd() { }
    public void Rotate(Vector2 delta) { }
    public void Scale(float delta) { }

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

        float v = Mathf.Clamp(local.x, 0.08f, 0.78f);
        Bench.ScreenDistance = v;
        Place(v);
    }

    Transform BuildGlyph(string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(transform, false);
        root.gameObject.AddComponent<FaceCamera>();
        LabFactory.Primitive(PrimitiveType.Cube, "Body", root, new Vector3(0f, -0.02f, 0f), new Vector3(0.012f, 0.04f, 0.012f), LabFactory.Lit(new Color(0.95f, 0.55f, 0.15f), true, 0f, 0.2f, true, new Color(1f, 0.4f, 0.05f)), false);
        LabFactory.Primitive(PrimitiveType.Sphere, "Flame", root, new Vector3(0f, 0.02f, 0f), new Vector3(0.02f, 0.028f, 0.02f), LabFactory.Lit(new Color(1f, 0.85f, 0.2f, 0.9f), true, 0f, 0.15f, true, new Color(1f, 0.7f, 0.1f)), false);
        var renders = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renders.Length; i++)
        {
            renders[i].material = new Material(renders[i].sharedMaterial);
        }

        return root;
    }

    static void SetGlyphAlpha(Transform glyph, float alpha)
    {
        var renders = glyph.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renders.Length; i++)
        {
            Material mat = renders[i].material;
            Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            c.a = alpha;
            UrpMaterialUtil.SetColor(mat, c);
        }
    }
}
