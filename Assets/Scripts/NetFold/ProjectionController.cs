using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ProjectionController : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public bool FrontRays = true;
    public bool TopRays = true;
    public bool SideRays = true;

    ShapeController _target;
    Transform _anchor;
    Transform _root;
    ProjectionCard _front;
    ProjectionCard _top;
    ProjectionCard _side;
    readonly List<LineRenderer> _frontLines = new List<LineRenderer>();
    readonly List<LineRenderer> _topLines = new List<LineRenderer>();
    readonly List<LineRenderer> _sideLines = new List<LineRenderer>();

    public void SetAnchor(Transform anchor)
    {
        _anchor = anchor;
        if (_root != null && _anchor != null)
        {
            _root.SetParent(_anchor, false);
        }
    }

    public void Bind(ShapeController shape)
    {
        _target = shape;
        if (IsActive)
        {
            RebuildViews();
        }
    }

    public void SetActive(bool on)
    {
        IsActive = on;
        if (_root == null)
        {
            Build();
        }

        _root.gameObject.SetActive(on);
        if (on)
        {
            RebuildViews();
            _root.localScale = Vector3.one * 0.01f;
            _root.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
    }

    public void Toggle()
    {
        SetActive(!IsActive);
    }

    public void SetRay(int index, bool on)
    {
        if (index == 0) FrontRays = on;
        if (index == 1) TopRays = on;
        if (index == 2) SideRays = on;
        RefreshRayVisibility();
    }

    void LateUpdate()
    {
        if (!IsActive || _target == null)
        {
            return;
        }

        UpdateRays();
    }

    void Build()
    {
        _root = new GameObject("ProjectionRig").transform;
        _root.SetParent(_anchor != null ? _anchor : transform, false);
        _front = CreateCard("主视图", new Vector3(0f, 0.22f, -0.5f), Quaternion.LookRotation(Vector3.back, Vector3.up), NetFoldTheme.FrontView, true);
        _top = CreateCard("俯视图", new Vector3(0f, 0.72f, 0f), Quaternion.Euler(-90f, 0f, 0f), NetFoldTheme.TopView, false);
        _side = CreateCard("左视图", new Vector3(-0.5f, 0.22f, 0f), Quaternion.LookRotation(Vector3.left, Vector3.up), NetFoldTheme.SideView, true);
        CreateRayPool(_frontLines, NetFoldTheme.FrontView, 8);
        CreateRayPool(_topLines, NetFoldTheme.TopView, 8);
        CreateRayPool(_sideLines, NetFoldTheme.SideView, 8);
    }

    ProjectionCard CreateCard(string title, Vector3 pos, Quaternion rotation, Color color, bool flipX)
    {
        var go = new GameObject(title);
        go.transform.SetParent(_root, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rotation;
        var card = go.AddComponent<ProjectionCard>();
        card.Build(title, color, flipX);
        return card;
    }

    void CreateRayPool(List<LineRenderer> pool, Color color, int count)
    {
        Material mat = UrpMaterialUtil.CreateLit(color, true, 0f, 0.1f, true, color * 1.6f);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Ray");
            go.transform.SetParent(_root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = 0.006f;
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pool.Add(lr);
        }
    }

    void RebuildViews()
    {
        if (_target == null)
        {
            return;
        }

        _front.SetSilhouette(_target.Type, ViewKind.Front);
        _top.SetSilhouette(_target.Type, ViewKind.Top);
        _side.SetSilhouette(_target.Type, ViewKind.Side);
    }

    void UpdateRays()
    {
        Vector3[] corners = BoundCorners(GetBounds(_target.transform));
        DrawToPlane(_frontLines, corners, _front.transform, FrontRays);
        DrawToPlane(_topLines, corners, _top.transform, TopRays);
        DrawToPlane(_sideLines, corners, _side.transform, SideRays);
    }

    static void DrawToPlane(List<LineRenderer> lines, Vector3[] corners, Transform card, bool visible)
    {
        Vector3 normal = card.forward;
        if (normal.sqrMagnitude < 1e-6f)
        {
            normal = Vector3.up;
        }

        normal.Normalize();
        Vector3 planePoint = card.position;
        for (int i = 0; i < lines.Count; i++)
        {
            bool on = visible && i < corners.Length;
            lines[i].enabled = on;
            if (!on)
            {
                continue;
            }

            Vector3 start = corners[i];
            float dist = Vector3.Dot(planePoint - start, normal);
            lines[i].SetPosition(0, start);
            lines[i].SetPosition(1, start + normal * dist);
        }
    }

    static Vector3[] BoundCorners(Bounds b)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;
        return new[]
        {
            c + new Vector3(e.x, e.y, e.z),
            c + new Vector3(e.x, e.y, -e.z),
            c + new Vector3(e.x, -e.y, e.z),
            c + new Vector3(e.x, -e.y, -e.z),
            c + new Vector3(-e.x, e.y, e.z),
            c + new Vector3(-e.x, e.y, -e.z),
            c + new Vector3(-e.x, -e.y, e.z),
            c + new Vector3(-e.x, -e.y, -e.z)
        };
    }

    void RefreshRayVisibility()
    {
        for (int i = 0; i < _frontLines.Count; i++) _frontLines[i].enabled = IsActive && FrontRays;
        for (int i = 0; i < _topLines.Count; i++) _topLines[i].enabled = IsActive && TopRays;
        for (int i = 0; i < _sideLines.Count; i++) _sideLines[i].enabled = IsActive && SideRays;
    }

    static Bounds GetBounds(Transform t)
    {
        var rends = t.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(t.position, Vector3.one * 0.3f);
        bool any = false;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] is LineRenderer)
            {
                continue;
            }

            if (!any)
            {
                b = rends[i].bounds;
                any = true;
            }
            else
            {
                b.Encapsulate(rends[i].bounds);
            }
        }

        return b;
    }
}

public enum ViewKind
{
    Front,
    Top,
    Side
}

public class ProjectionCard : MonoBehaviour, IDraggable, ISelectable
{
    public bool CanDrag => false;
    public bool IsSelected { get; private set; }
    MeshFilter _silhouette;

    public void Build(string title, Color color, bool flipX)
    {
        var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frame.name = "Frame";
        frame.transform.SetParent(transform, false);
        frame.transform.localPosition = new Vector3(0f, 0f, -0.006f);
        frame.transform.localScale = new Vector3(0.39f, 0.39f, 1f);
        UnityEngine.Object.Destroy(frame.GetComponent<Collider>());
        frame.GetComponent<MeshRenderer>().sharedMaterial = UrpMaterialUtil.CreateLit(NetFoldTheme.StoneDeep, false, 0.2f, 0.4f);

        var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "Plate";
        plate.transform.SetParent(transform, false);
        plate.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
        UnityEngine.Object.Destroy(plate.GetComponent<Collider>());
        var mat = UrpMaterialUtil.CreateLit(new Color(color.r, color.g, color.b, 0.16f), true, 0f, 0.3f);
        plate.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var sil = new GameObject("Silhouette");
        sil.transform.SetParent(transform, false);
        sil.transform.localPosition = new Vector3(0f, 0f, 0.012f);
        sil.transform.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
        _silhouette = sil.AddComponent<MeshFilter>();
        var rend = sil.AddComponent<MeshRenderer>();
        rend.sharedMaterial = UrpMaterialUtil.CreateLit(color, true, 0f, 0.2f, true, color * 0.55f);
    }

    public void SetSilhouette(ShapeType type, ViewKind view)
    {
        _silhouette.sharedMesh = ViewSilhouette.Create(type, view);
    }

    public void SetError(bool error)
    {
        var rend = _silhouette.GetComponent<MeshRenderer>();
        UrpMaterialUtil.SetColor(rend.material, error ? NetFoldTheme.Error : rend.sharedMaterial.color);
    }

    public void OnSelect() => IsSelected = true;
    public void OnDeselect() => IsSelected = false;
    public void OnDragStart(Vector3 worldPoint, Ray pointerRay) { }
    public void OnDrag(Vector3 worldPoint, Ray pointerRay) { }
    public void OnDragEnd() { }
}

public static class ViewSilhouette
{
    public static Mesh Create(ShapeType type, ViewKind view)
    {
        switch (type)
        {
            case ShapeType.Cube:
                return Quad(0.18f, 0.18f);
            case ShapeType.Cylinder:
                return view == ViewKind.Top ? Circle(0.1f) : Quad(0.14f, 0.2f);
            case ShapeType.Cone:
                return view == ViewKind.Top ? Circle(0.1f) : Triangle(0.18f, 0.2f);
            case ShapeType.TriangularPrism:
                return view == ViewKind.Top ? Triangle(0.18f, 0.16f) : Quad(0.16f, 0.2f);
            default:
                return Quad(0.16f, 0.16f);
        }
    }

    static Mesh Quad(float w, float h)
    {
        float hw = w * 0.5f;
        float hh = h * 0.5f;
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0), new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh Circle(float r)
    {
        const int n = 24;
        var v = new Vector3[n + 1];
        var t = new int[n * 3];
        v[0] = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            v[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }

        for (int i = 0; i < n; i++)
        {
            t[i * 3] = 0;
            t[i * 3 + 1] = i + 1;
            t[i * 3 + 2] = i == n - 1 ? 1 : i + 2;
        }

        var mesh = new Mesh { vertices = v, triangles = t };
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh Triangle(float w, float h)
    {
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-w * 0.5f, -h * 0.5f, 0), new Vector3(w * 0.5f, -h * 0.5f, 0), new Vector3(0, h * 0.5f, 0)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
