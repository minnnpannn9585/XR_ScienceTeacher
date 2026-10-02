using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SectionController : MonoBehaviour, IDraggable, ISelectable, IRotatable
{
    public bool IsActive { get; private set; }
    public bool CanDrag => IsActive;
    public bool IsSelected { get; private set; }
    public int EdgeCount { get; private set; }
    public event System.Action<int> EdgeCountChanged;

    Transform _root;
    Transform _planeVis;
    LineRenderer _loop;
    MeshFilter _poly;
    ShapeController _target;
    Plane _desk = new Plane(Vector3.up, Vector3.zero);
    Vector3 _grab;
    readonly List<Vector3> _points = new List<Vector3>(32);

    public void Bind(ShapeController shape)
    {
        _target = shape;
        if (IsActive)
        {
            Rebuild();
        }
    }

    public void SetActive(bool on)
    {
        if (_root == null)
        {
            Build();
        }

        IsActive = on;
        _root.gameObject.SetActive(on);
        if (on)
        {
            _root.localScale = Vector3.one * 0.05f;
            _root.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            Rebuild();
        }
        else
        {
            EdgeCount = 0;
            EdgeCountChanged?.Invoke(0);
        }
    }

    public void Toggle()
    {
        SetActive(!IsActive);
    }

    void Build()
    {
        _root = new GameObject("SectionRig").transform;
        _root.SetParent(transform, false);
        _root.localPosition = new Vector3(0f, 0.22f, 0f);
        _root.localRotation = Quaternion.Euler(18f, 28f, -12f);

        _planeVis = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        _planeVis.name = "CutPlane";
        _planeVis.SetParent(_root, false);
        _planeVis.localScale = new Vector3(0.7f, 0.7f, 1f);
        UnityEngine.Object.Destroy(_planeVis.GetComponent<MeshCollider>());
        var box = _planeVis.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 1f, 0.02f);
        _planeVis.GetComponent<MeshRenderer>().sharedMaterial = UrpMaterialUtil.CreateLit(NetFoldTheme.Section, true, 0f, 0.25f, true, new Color(1f, 0.85f, 0.4f, 0.35f));
        _planeVis.gameObject.AddComponent<SectionPlaneProxy>().Bind(this);

        var loopGo = new GameObject("SectionLoop");
        loopGo.transform.SetParent(_root, false);
        _loop = loopGo.AddComponent<LineRenderer>();
        _loop.useWorldSpace = true;
        _loop.loop = true;
        _loop.widthMultiplier = 0.01f;
        _loop.sharedMaterial = UrpMaterialUtil.CreateLit(NetFoldTheme.Success, true, 0f, 0.1f, true, NetFoldTheme.Success * 2f);
        _loop.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var polyGo = new GameObject("SectionPoly");
        polyGo.transform.SetParent(_root, false);
        _poly = polyGo.AddComponent<MeshFilter>();
        polyGo.AddComponent<MeshRenderer>().sharedMaterial = UrpMaterialUtil.CreateLit(new Color(1f, 0.9f, 0.4f, 0.55f), true, 0f, 0.4f, true, new Color(1f, 0.85f, 0.3f, 0.8f));
        _poly.mesh = new Mesh { name = "Section" };
    }

    void LateUpdate()
    {
        if (IsActive)
        {
            Rebuild();
        }
    }

    public void Rebuild()
    {
        if (_target == null || _planeVis == null)
        {
            return;
        }

        Plane plane = new Plane(_planeVis.forward, _planeVis.position);
        _points.Clear();
        var filters = _target.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            IntersectMesh(filters[i], plane, _points);
        }

        OrderConvex(_points, plane);
        EdgeCount = _points.Count >= 3 ? _points.Count : 0;
        EdgeCountChanged?.Invoke(EdgeCount);
        DrawPolygon(_points);
    }

    public void GlowSuccess()
    {
        if (_poly == null)
        {
            return;
        }

        var rend = _poly.GetComponent<MeshRenderer>();
        rend.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 6, 0.5f);
        FeedbackService.Instance?.Burst(_planeVis.position, NetFoldTheme.Success, 36);
    }

    public void OnSelect() => IsSelected = true;
    public void OnDeselect() => IsSelected = false;

    public void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
        _desk = new Plane(Vector3.up, _root.position);
        _desk.Raycast(pointerRay, out float enter);
        _grab = _root.position - pointerRay.GetPoint(enter);
    }

    public void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        if (_desk.Raycast(pointerRay, out float enter))
        {
            Vector3 p = pointerRay.GetPoint(enter) + _grab;
            p.y = _root.position.y;
            _root.position = p;
        }
    }

    public void OnDragEnd()
    {
    }

    public void Rotate(Vector2 delta)
    {
        _root.Rotate(Vector3.up, -delta.x, Space.World);
        _root.Rotate(Vector3.right, delta.y, Space.World);
    }

    static void IntersectMesh(MeshFilter filter, Plane plane, List<Vector3> points)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null)
        {
            return;
        }

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        Transform tf = filter.transform;
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = tf.TransformPoint(verts[tris[i]]);
            Vector3 b = tf.TransformPoint(verts[tris[i + 1]]);
            Vector3 c = tf.TransformPoint(verts[tris[i + 2]]);
            TryEdge(plane, a, b, points);
            TryEdge(plane, b, c, points);
            TryEdge(plane, c, a, points);
        }
    }

    static void TryEdge(Plane plane, Vector3 a, Vector3 b, List<Vector3> points)
    {
        float da = plane.GetDistanceToPoint(a);
        float db = plane.GetDistanceToPoint(b);
        if (da * db > 0f || Mathf.Abs(da - db) < 1e-5f)
        {
            return;
        }

        float t = da / (da - db);
        Vector3 p = Vector3.Lerp(a, b, t);
        for (int i = 0; i < points.Count; i++)
        {
            if ((points[i] - p).sqrMagnitude < 0.00012f)
            {
                return;
            }
        }

        points.Add(p);
    }

    static void OrderConvex(List<Vector3> points, Plane plane)
    {
        if (points.Count < 3)
        {
            return;
        }

        Vector3 c = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
        {
            c += points[i];
        }

        c /= points.Count;
        Vector3 n = plane.normal;
        Vector3 axisA = Vector3.Cross(n, Vector3.up);
        if (axisA.sqrMagnitude < 0.01f)
        {
            axisA = Vector3.Cross(n, Vector3.right);
        }

        axisA.Normalize();
        Vector3 axisB = Vector3.Cross(n, axisA);
        points.Sort((p, q) =>
        {
            Vector3 dp = p - c;
            Vector3 dq = q - c;
            float ap = Mathf.Atan2(Vector3.Dot(dp, axisB), Vector3.Dot(dp, axisA));
            float aq = Mathf.Atan2(Vector3.Dot(dq, axisB), Vector3.Dot(dq, axisA));
            return ap.CompareTo(aq);
        });
    }

    void DrawPolygon(List<Vector3> points)
    {
        if (points.Count < 3)
        {
            _loop.positionCount = 0;
            _poly.mesh.Clear();
            return;
        }

        _loop.positionCount = points.Count;
        _loop.SetPositions(points.ToArray());

        Vector3 c = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
        {
            c += points[i];
        }

        c /= points.Count;
        var verts = new Vector3[points.Count + 1];
        var tris = new int[points.Count * 3];
        verts[0] = _poly.transform.InverseTransformPoint(c);
        for (int i = 0; i < points.Count; i++)
        {
            verts[i + 1] = _poly.transform.InverseTransformPoint(points[i]);
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i == points.Count - 1 ? 1 : i + 2;
        }

        var mesh = _poly.mesh;
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
    }
}

public class SectionPlaneProxy : MonoBehaviour, IInteractable
{
    SectionController _owner;
    public bool CanDrag => _owner != null && _owner.CanDrag;
    public bool IsSelected => _owner != null && _owner.IsSelected;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    public void Bind(SectionController owner)
    {
        _owner = owner;
    }

    public void OnSelect() => _owner?.OnSelect();
    public void OnDeselect() => _owner?.OnDeselect();
    public void OnDragStart(Vector3 worldPoint, Ray pointerRay) => _owner?.OnDragStart(worldPoint, pointerRay);
    public void OnDrag(Vector3 worldPoint, Ray pointerRay) => _owner?.OnDrag(worldPoint, pointerRay);
    public void OnDragEnd() => _owner?.OnDragEnd();
    public void Rotate(Vector2 delta) => _owner?.Rotate(delta);
    public void Scale(float delta) => _owner?.Rotate(new Vector2(0f, delta * 18f));
}
