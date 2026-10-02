using UnityEngine;

public static class GeometryFactory
{
    public const float DefaultSize = 0.42f;

    public static ShapeController Create(ShapeType type, Transform parent, Vector3 localPosition, float size = DefaultSize, bool withCollider = true)
    {
        var root = new GameObject(ShapeCatalog.DisplayName(type));
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.layer = 0;

        var controller = root.AddComponent<ShapeController>();
        Material faceMat = UrpMaterialUtil.CreateLit(NetFoldTheme.Shape, true, 0.12f, 0.74f, true, NetFoldTheme.EdgeGlow * 0.35f);
        Material hiMat = UrpMaterialUtil.CreateLit(Color.Lerp(NetFoldTheme.Shape, Color.white, 0.35f), true, 0.08f, 0.8f, true, NetFoldTheme.Accent);
        Material edgeMat = UrpMaterialUtil.CreateLit(NetFoldTheme.EdgeGlow, true, 0f, 0.2f, true, NetFoldTheme.EdgeGlow * 2f);

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

        controller.Initialize(type, faceMat, hiMat);
        return controller;
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
        lr.widthMultiplier = 0.008f;
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
        Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
        AddFace(root, "底面", 0, q, face, hi, edge, new Vector3(0f, 0.001f, 0f), flat, new Vector3(0f, 0.002f, 0f), flat, col);
        AddFace(root, "前面", 1, q, face, hi, edge, new Vector3(0f, s * 0.5f, s * 0.5f), Quaternion.identity, new Vector3(0f, 0.002f, s), flat, col);
        AddFace(root, "后面", 2, q, face, hi, edge, new Vector3(0f, s * 0.5f, -s * 0.5f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0f, 0.002f, -s), flat, col);
        AddFace(root, "左面", 3, q, face, hi, edge, new Vector3(-s * 0.5f, s * 0.5f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(-s, 0.002f, 0f), flat, col);
        AddFace(root, "右面", 4, q, face, hi, edge, new Vector3(s * 0.5f, s * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(s, 0.002f, 0f), flat, col);
        AddFace(root, "顶面", 5, q, face, hi, edge, new Vector3(0f, s, 0f), Quaternion.Euler(-90f, 0f, 0f), new Vector3(0f, 0.002f, 2f * s), flat, col);
    }

    static void BuildCylinder(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float r = s * 0.42f;
        float h = s;
        int segs = 12;
        float chord = 2f * r * Mathf.Sin(Mathf.PI / segs);
        float arc = 2f * Mathf.PI * r;
        Mesh panel = Quad(chord, h);
        Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Sin(a) * r, h * 0.5f, Mathf.Cos(a) * r);
            Quaternion rot = Quaternion.LookRotation(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), Vector3.up);
            float x = (i - (segs - 1) * 0.5f) * (arc / segs);
            AddFace(root, "侧面" + (i + 1), i, panel, face, hi, edge, pos, rot, new Vector3(x, 0.002f, 0f), flat, col);
        }

        Mesh circle = Circle(r, 28);
        AddFace(root, "底面", segs, circle, face, hi, edge, new Vector3(0f, 0.001f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0f, 0.002f, -r * 2.15f), flat, col);
        AddFace(root, "顶面", segs + 1, circle, face, hi, edge, new Vector3(0f, h, 0f), Quaternion.Euler(-90f, 0f, 0f), new Vector3(0f, 0.002f, r * 2.15f), flat, col);
    }

    static void BuildCone(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float r = s * 0.46f;
        float h = s;
        int segs = 14;
        float slant = Mathf.Sqrt(r * r + h * h);
        float theta = Mathf.PI * 2f * r / slant;
        Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
        Mesh wedge = Triangle(2f * r * Mathf.Sin(Mathf.PI / segs), slant);
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            Vector3 baseP = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            Vector3 tip = new Vector3(0f, h, 0f);
            Vector3 mid = (baseP + tip) * 0.5f;
            Vector3 outward = new Vector3(baseP.x, r * r / h, baseP.z);
            Quaternion rot = Quaternion.LookRotation(outward.normalized, (tip - baseP).normalized);
            float ang = -theta * 0.5f + (i + 0.5f) / segs * theta;
            Vector3 u = new Vector3(Mathf.Sin(ang) * slant * 0.45f, 0.002f, Mathf.Cos(ang) * slant * 0.45f);
            AddFace(root, "侧面" + (i + 1), i, wedge, face, hi, edge, mid, rot, u, flat, col);
        }

        AddFace(root, "底面", segs, Circle(r, 28), face, hi, edge, new Vector3(0f, 0.001f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0f, 0.002f, -r * 2.3f), flat, col);
    }

    static void BuildPrism(Transform root, float s, Material face, Material hi, Material edge, bool col)
    {
        float w = s;
        float h = s;
        float d = s * 0.86f;
        Quaternion flat = Quaternion.Euler(90f, 0f, 0f);
        Mesh rectA = Quad(w, h);
        Mesh rectB = Quad(d, h);
        Mesh rectC = Quad(w, h);
        Mesh tri = Triangle(w, d);
        AddFace(root, "前面", 0, rectA, face, hi, edge, new Vector3(0f, h * 0.5f, d * 0.5f), Quaternion.identity, new Vector3(0f, 0.002f, d), flat, col);
        AddFace(root, "后面", 1, rectC, face, hi, edge, new Vector3(0f, h * 0.5f, -d * 0.5f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0f, 0.002f, -d), flat, col);
        AddFace(root, "底面", 2, Quad(w, d), face, hi, edge, new Vector3(0f, 0.001f, 0f), flat, new Vector3(0f, 0.002f, 0f), flat, col);
        AddFace(root, "左斜面", 3, rectB, face, hi, edge, new Vector3(-w * 0.28f, h * 0.5f, 0f), Quaternion.Euler(0f, -55f, 0f), new Vector3(-w, 0.002f, 0f), flat, col);
        AddFace(root, "右斜面", 4, rectB, face, hi, edge, new Vector3(w * 0.28f, h * 0.5f, 0f), Quaternion.Euler(0f, 55f, 0f), new Vector3(w, 0.002f, 0f), flat, col);
        AddFace(root, "左底三角", 5, tri, face, hi, edge, new Vector3(-w * 0.5f, h * 0.5f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(-w * 1.7f, 0.002f, 0f), flat, col);
        AddFace(root, "右底三角", 6, tri, face, hi, edge, new Vector3(w * 0.5f, h * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(w * 1.7f, 0.002f, 0f), flat, col);
    }
}
