using System.Collections.Generic;
using UnityEngine;

public static class GeometryFactory
{
    public const float DefaultSize = 0.42f;
    public const float SurfaceGap = 0.04f;

    public static ShapeController Create(ShapeType type, Transform parent, Vector3 localPosition, float size = DefaultSize, bool withCollider = true)
    {
        var root = new GameObject(ShapeCatalog.DisplayName(type));
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.layer = 0;

        var controller = root.AddComponent<ShapeController>();
        Material faceMat = UrpMaterialUtil.CreateLit(NetFoldTheme.Shape, true, 0.02f, 0.92f, true, new Color(0.55f, 0.68f, 0.78f, 0.1f));
        Material hiMat = UrpMaterialUtil.CreateLit(NetFoldTheme.ShapeAlt, true, 0.04f, 0.96f, true, NetFoldTheme.Hairline * 0.25f);
        Material edgeMat = UrpMaterialUtil.CreateLit(NetFoldTheme.EdgeGlow, true, 0.72f, 0.55f, true, NetFoldTheme.EdgeGlow * 0.4f);

        switch (type)
        {
            case ShapeType.Cube:
                BuildCube(root.transform, size, faceMat, hiMat, edgeMat, withCollider);
                break;
            case ShapeType.Cylinder:
                BuildCylinder(root.transform, size, faceMat, hiMat, edgeMat, withCollider);
                break;
            case ShapeType.Cone:
                BuildCone(root.transform, size, faceMat, hiMat, edgeMat, withCollider);
                break;
            case ShapeType.TriangularPrism:
                BuildPrism(root.transform, size, faceMat, hiMat, edgeMat, withCollider);
                break;
        }

        CenterPivotAndRaise(root.transform);
        controller.Initialize(type, faceMat, hiMat);
        return controller;
    }

    static void CenterPivotAndRaise(Transform root)
    {
        var faces = root.GetComponentsInChildren<ShapeFace>(true);
        Bounds bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < faces.Length; i++)
        {
            MeshFilter filter = faces[i].GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Vector3[] vertices = filter.sharedMesh.vertices;
            for (int j = 0; j < vertices.Length; j++)
            {
                Vector3 point = root.InverseTransformPoint(filter.transform.TransformPoint(vertices[j]));
                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        if (!hasBounds)
        {
            root.localPosition += Vector3.up * SurfaceGap;
            return;
        }

        Vector3 center = bounds.center;
        for (int i = 0; i < faces.Length; i++)
        {
            ShapeFace face = faces[i];
            face.transform.localPosition -= center;
            face.FoldedLocalPos -= center;
            face.UnfoldedLocalPos -= center;
            face.HingePoint -= center;
        }

        root.localPosition += center + Vector3.up * SurfaceGap;
    }

    static ShapeFace AddFace(Transform parent, string name, int index, Mesh mesh, Material faceMat, Material hiMat, Material edgeMat, Vector3 foldedPos, Quaternion foldedRot, Vector3 unfoldedPos, Quaternion unfoldedRot, bool withCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = foldedPos;
        go.transform.localRotation = foldedRot;
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = faceMat;
        if (withCollider)
        {
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        var face = go.AddComponent<ShapeFace>();
        face.FaceIndex = index;
        face.FaceName = name;
        face.FoldedLocalPos = foldedPos;
        face.FoldedLocalRot = foldedRot;
        face.UnfoldedLocalPos = unfoldedPos;
        face.UnfoldedLocalRot = unfoldedRot;
        face.BindMaterials(faceMat, hiMat);
        AddEdgeGlow(go.transform, mesh, edgeMat);
        return face;
    }

    static void AddEdgeGlow(Transform face, Mesh mesh, Material edgeMat)
    {
        var go = new GameObject("Edge");
        go.transform.SetParent(face, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = edgeMat;
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.widthMultiplier = 0.0055f;
        lr.positionCount = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (verts.Length >= 4)
        {
            var outline = GuessOutline(verts, tris);
            lr.positionCount = outline.Length;
            lr.SetPositions(outline);
        }
        else if (verts.Length > 0)
        {
            lr.positionCount = verts.Length;
            lr.SetPositions(verts);
        }
    }

    static Vector3[] GuessOutline(Vector3[] verts, int[] tris)
    {
        if (verts.Length == 4)
        {
            return new[] { verts[0], verts[1], verts[2], verts[3] };
        }

        if (verts.Length > 8 && verts[0].sqrMagnitude < 1e-6f)
        {
            var rim = new Vector3[verts.Length - 1];
            for (int i = 1; i < verts.Length; i++)
            {
                rim[i - 1] = verts[i];
            }

            return rim;
        }

        if (verts.Length <= 2)
        {
            return verts;
        }

        Vector3 n = Vector3.zero;
        if (tris.Length >= 3)
        {
            n = Vector3.Cross(verts[tris[1]] - verts[tris[0]], verts[tris[2]] - verts[tris[0]]).normalized;
        }

        Vector3 axisA = Vector3.Cross(n, Vector3.up);
        if (axisA.sqrMagnitude < 0.01f)
        {
            axisA = Vector3.Cross(n, Vector3.right);
        }

        axisA.Normalize();
        Vector3 axisB = Vector3.Cross(n, axisA);
        Vector3 c = Vector3.zero;
        for (int i = 0; i < verts.Length; i++)
        {
            c += verts[i];
        }

        c /= verts.Length;
        var idx = new int[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            idx[i] = i;
        }

        System.Array.Sort(idx, (a, b) =>
        {
            Vector3 da = verts[a] - c;
            Vector3 db = verts[b] - c;
            float aa = Mathf.Atan2(Vector3.Dot(da, axisB), Vector3.Dot(da, axisA));
            float ab = Mathf.Atan2(Vector3.Dot(db, axisB), Vector3.Dot(db, axisA));
            return aa.CompareTo(ab);
        });

        var ordered = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            ordered[i] = verts[idx[i]];
        }

        return ordered;
    }

    static Mesh Quad(float w, float h)
    {
        float hw = w * 0.5f;
        float hh = h * 0.5f;
        var mesh = new Mesh { name = "Quad" };
        mesh.vertices = new[]
        {
            new Vector3(-hw, -hh, 0f),
            new Vector3(hw, -hh, 0f),
            new Vector3(hw, hh, 0f),
            new Vector3(-hw, hh, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 2, 1, 0, 2 };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh Circle(float radius, int seg)
    {
        var verts = new Vector3[seg + 1];
        var tris = new int[seg * 6];
        verts[0] = Vector3.zero;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }

        for (int i = 0; i < seg; i++)
        {
            int a = i + 1;
            int b = i == seg - 1 ? 1 : i + 2;
            int t = i * 6;
            tris[t] = 0;
            tris[t + 1] = a;
            tris[t + 2] = b;
            tris[t + 3] = 0;
            tris[t + 4] = b;
            tris[t + 5] = a;
        }

        var mesh = new Mesh { name = "Circle", vertices = verts, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh Triangle(float w, float h)
    {
        var mesh = new Mesh { name = "Tri" };
        mesh.vertices = new[]
        {
            new Vector3(-w * 0.5f, -h * 0.5f, 0f),
            new Vector3(w * 0.5f, -h * 0.5f, 0f),
            new Vector3(0f, h * 0.5f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void BuildCube(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        Mesh q = Quad(s, s);
        Quaternion up = Quaternion.Euler(-90f, 0f, 0f);
        AddFace(root, "底面", 0, q, face, hi, edge, new Vector3(0f, 0.001f, 0f), up, new Vector3(0f, 0.001f, 0f), up, col);
        var front = AddFace(root, "前面", 1, q, face, hi, edge, new Vector3(0f, s * 0.5f, -s * 0.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.zero, Quaternion.identity, col);
        var back = AddFace(root, "后面", 2, q, face, hi, edge, new Vector3(0f, s * 0.5f, s * 0.5f), Quaternion.identity, Vector3.zero, Quaternion.identity, col);
        var left = AddFace(root, "左面", 3, q, face, hi, edge, new Vector3(-s * 0.5f, s * 0.5f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.zero, Quaternion.identity, col);
        var right = AddFace(root, "右面", 4, q, face, hi, edge, new Vector3(s * 0.5f, s * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.zero, Quaternion.identity, col);
        var top = AddFace(root, "顶面", 5, q, face, hi, edge, new Vector3(0f, s, 0f), up, Vector3.zero, Quaternion.identity, col);
        HingeFlat(front, new Vector3(0f, 0f, -s * 0.5f), Vector3.right, Vector3.back);
        HingeFlat(back, new Vector3(0f, 0f, s * 0.5f), Vector3.right, Vector3.forward);
        HingeFlat(left, new Vector3(-s * 0.5f, 0f, 0f), Vector3.forward, Vector3.left);
        HingeFlat(right, new Vector3(s * 0.5f, 0f, 0f), Vector3.forward, Vector3.right);
        Vector3 topHinge = new Vector3(0f, s, -s * 0.5f);
        Vector3 topAxis = Vector3.right;
        float topAngle = Vector3.SignedAngle(top.FoldedLocalRot * Vector3.forward, front.FoldedLocalRot * Vector3.forward, topAxis);
        top.SetHinge(front.FaceIndex, topHinge, topAxis, topAngle, 1);
    }

    static void BuildCylinder(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float r = s * 0.42f;
        float h = s;
        const int segs = 12;
        float delta = Mathf.PI * 2f / segs;
        const float frontAngle = Mathf.PI;
        int[] offsets = new int[segs];
        offsets[0] = 0;
        for (int k = 1; k <= 5; k++)
        {
            offsets[k] = k;
            offsets[5 + k] = -k;
        }

        offsets[11] = 6;
        var panels = new ShapeFace[segs];
        for (int i = 0; i < segs; i++)
        {
            int k = offsets[i];
            float a0 = frontAngle + (k - 0.5f) * delta;
            float a1 = frontAngle + (k + 0.5f) * delta;
            Vector3 b0 = Ring(a0, 0f, r);
            Vector3 b1 = Ring(a1, 0f, r);
            Vector3 t1 = b1 + Vector3.up * h;
            Vector3 t0 = b0 + Vector3.up * h;
            panels[i] = AddPolygonFace(root, "侧面" + (i + 1), i, new[] { b0, b1, t1, t0 }, face, hi, edge, col, RadialOut(b0, b1));
            if (k == 0)
            {
                Vector3 hinge = (b0 + b1) * 0.5f;
                Vector3 axis = (b1 - b0).normalized;
                HingeFlat(panels[i], hinge, axis, Ring(frontAngle, 0f, 1f));
            }
            else
            {
                int parentOffset = k == 6 ? 5 : k > 0 ? k - 1 : k + 1;
                int parentIndex = System.Array.IndexOf(offsets, parentOffset);
                bool sharedAtStart = k > 0;
                Vector3 e0 = sharedAtStart ? b0 : b1;
                Vector3 e1 = sharedAtStart ? t0 : t1;
                HingeToParent(panels[i], panels[parentIndex], (e0 + e1) * 0.5f, e1 - e0);
            }
        }

        Quaternion up = Quaternion.Euler(-90f, 0f, 0f);
        AddFace(root, "底面", segs, Circle(r, 28), face, hi, edge, new Vector3(0f, 0.001f, 0f), up, new Vector3(0f, 0.001f, 0f), up, col);
        var top = AddFace(root, "顶面", segs + 1, Circle(r, 28), face, hi, edge, new Vector3(0f, h, 0f), up, Vector3.zero, Quaternion.identity, col);
        Vector3 tb0 = Ring(frontAngle - delta * 0.5f, h, r);
        Vector3 tb1 = Ring(frontAngle + delta * 0.5f, h, r);
        HingeToParent(top, panels[0], (tb0 + tb1) * 0.5f, tb1 - tb0);
    }

    static void BuildCone(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float r = s * 0.46f;
        float h = s;
        const int segs = 12;
        float delta = Mathf.PI * 2f / segs;
        const float frontAngle = Mathf.PI;
        int[] offsets = new int[segs];
        offsets[0] = 0;
        for (int k = 1; k <= 5; k++)
        {
            offsets[k] = k;
            offsets[5 + k] = -k;
        }

        offsets[11] = 6;
        Vector3 apex = new Vector3(0f, h, 0f);
        var panels = new ShapeFace[segs];
        for (int i = 0; i < segs; i++)
        {
            int k = offsets[i];
            float a0 = frontAngle + (k - 0.5f) * delta;
            float a1 = frontAngle + (k + 0.5f) * delta;
            Vector3 b0 = Ring(a0, 0f, r);
            Vector3 b1 = Ring(a1, 0f, r);
            panels[i] = AddPolygonFace(root, "侧面" + (i + 1), i, new[] { b0, b1, apex }, face, hi, edge, col, RadialOut(b0, b1));
            if (k == 0)
            {
                HingeFlat(panels[i], (b0 + b1) * 0.5f, b1 - b0, Ring(frontAngle, 0f, 1f));
            }
            else
            {
                int parentOffset = k == 6 ? 5 : k > 0 ? k - 1 : k + 1;
                int parentIndex = System.Array.IndexOf(offsets, parentOffset);
                bool sharedAtStart = k > 0;
                Vector3 foot = sharedAtStart ? b0 : b1;
                HingeToParent(panels[i], panels[parentIndex], (foot + apex) * 0.5f, apex - foot);
            }
        }

        Quaternion up = Quaternion.Euler(-90f, 0f, 0f);
        AddFace(root, "底面", segs, Circle(r, 28), face, hi, edge, new Vector3(0f, 0.001f, 0f), up, new Vector3(0f, 0.001f, 0f), up, col);
    }

    static void BuildPrism(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float w = s * 0.9f;
        float depth = s * 0.72f;
        float h = s * 0.7f;
        Vector3 p0 = new Vector3(-w * 0.5f, 0f, -depth * 0.42f);
        Vector3 p1 = new Vector3(w * 0.5f, 0f, -depth * 0.42f);
        Vector3 p2 = new Vector3(0f, 0f, depth * 0.55f);
        Vector3 up = Vector3.up * h;
        Vector3 centroid = (p0 + p1 + p2) / 3f;
        AddPolygonFace(root, "底面", 0, new[] { p0, p1, p2 }, face, hi, edge, col, Vector3.down);
        var front = AddPolygonFace(root, "前面", 1, new[] { p0, p1, p1 + up, p0 + up }, face, hi, edge, col, HorizontalOut(p0, p1, centroid));
        var left = AddPolygonFace(root, "左面", 2, new[] { p2, p0, p0 + up, p2 + up }, face, hi, edge, col, HorizontalOut(p2, p0, centroid));
        var right = AddPolygonFace(root, "右面", 3, new[] { p1, p2, p2 + up, p1 + up }, face, hi, edge, col, HorizontalOut(p1, p2, centroid));
        var top = AddPolygonFace(root, "顶面", 4, new[] { p0 + up, p1 + up, p2 + up }, face, hi, edge, col, Vector3.up);
        HingeFlat(front, (p0 + p1) * 0.5f, p1 - p0, HorizontalOut(p0, p1, centroid));
        HingeFlat(left, (p2 + p0) * 0.5f, p0 - p2, HorizontalOut(p2, p0, centroid));
        HingeFlat(right, (p1 + p2) * 0.5f, p2 - p1, HorizontalOut(p1, p2, centroid));
        HingeToParent(top, front, (p0 + p1) * 0.5f + up, p1 - p0);
    }

    static Vector3 Ring(float angle, float y, float radius)
    {
        return new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);
    }

    static Vector3 RadialOut(Vector3 a, Vector3 b)
    {
        Vector3 mid = (a + b) * 0.5f;
        mid.y = 0f;
        return mid.sqrMagnitude < 1e-6f ? Vector3.back : mid.normalized;
    }

    static Vector3 HorizontalOut(Vector3 a, Vector3 b, Vector3 centroid)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 outward = mid - centroid;
        outward.y = 0f;
        return outward.sqrMagnitude < 1e-6f ? Vector3.back : outward.normalized;
    }

    static void HingeFlat(ShapeFace face, Vector3 hinge, Vector3 axis, Vector3 outward)
    {
        face.SetHinge(-1, hinge, axis, BestFlatAngle(hinge, axis, face.FoldedLocalPos, outward), 0);
    }

    static void HingeToParent(ShapeFace face, ShapeFace parent, Vector3 hinge, Vector3 axis)
    {
        Vector3 nChild = face.FoldedLocalRot * Vector3.forward;
        Vector3 nParent = parent.FoldedLocalRot * Vector3.forward;
        Vector3 dir = axis.sqrMagnitude < 1e-8f ? Vector3.up : axis.normalized;
        float angle = Vector3.SignedAngle(nChild, nParent, dir);
        face.SetHinge(parent.FaceIndex, hinge, dir, angle, parent.Depth + 1);
    }

    static float BestFlatAngle(Vector3 hinge, Vector3 axis, Vector3 point, Vector3 outward)
    {
        if (axis.sqrMagnitude < 1e-8f)
        {
            return 0f;
        }

        Vector3 offset = point - hinge;
        Vector3 hint = outward.sqrMagnitude < 1e-8f ? Vector3.back : outward.normalized;
        float best = 0f;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < 360; i++)
        {
            float angle = i - 180f;
            Vector3 moved = Quaternion.AngleAxis(angle, axis) * offset;
            float score = Vector3.Dot(new Vector3(moved.x, 0f, moved.z), hint) - Mathf.Abs(moved.y) * 3f;
            if (score > bestScore)
            {
                bestScore = score;
                best = angle;
            }
        }

        return best;
    }

    static ShapeFace AddPolygonFace(Transform parent, string name, int index, Vector3[] verts, Material faceMat, Material hiMat, Material edgeMat, bool withCollider, Vector3 desiredOut)
    {
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < verts.Length; i++)
        {
            centroid += verts[i];
        }

        centroid /= verts.Length;
        Vector3 right = (verts[1] - verts[0]).normalized;
        Vector3 upGuess = verts.Length == 4
            ? (verts[2] + verts[3]) * 0.5f - (verts[0] + verts[1]) * 0.5f
            : verts[verts.Length - 1] - (verts[0] + verts[1]) * 0.5f;
        upGuess -= right * Vector3.Dot(upGuess, right);
        if (upGuess.sqrMagnitude < 1e-8f)
        {
            upGuess = Vector3.up;
        }

        upGuess.Normalize();
        Vector3 normal = Vector3.Cross(right, upGuess).normalized;
        if (desiredOut.sqrMagnitude > 1e-8f && Vector3.Dot(normal, desiredOut) < 0f)
        {
            normal = -normal;
        }

        Quaternion rot = Quaternion.LookRotation(normal, upGuess);
        Quaternion inv = Quaternion.Inverse(rot);
        var local = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            local[i] = inv * (verts[i] - centroid);
        }

        return AddFace(parent, name, index, BuildPoly(local), faceMat, hiMat, edgeMat, centroid, rot, centroid, rot, withCollider);
    }

    static Mesh BuildPoly(Vector3[] local)
    {
        var mesh = new Mesh { name = "Poly" };
        mesh.vertices = local;
        var tris = new List<int>();
        for (int i = 1; i < local.Length - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(0);
            tris.Add(i + 1);
            tris.Add(i);
        }

        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
