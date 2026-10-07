using System.Collections.Generic;
using UnityEngine;

public class LensController : MonoBehaviour
{
    const float Aperture = 0.105f;
    const float RimInner = 0.1f;
    const float RimOuter = 0.118f;
    const int Segments = 48;
    const int Rings = 12;

    Transform _glass;
    Mesh _lensMesh;
    Transform _focusNear;
    Transform _focusFar;
    Transform _twiceNear;
    Transform _twiceFar;

    public void Build(OpticalBenchController bench)
    {
        _glass = new GameObject("Glass").transform;
        _glass.SetParent(transform, false);
        _glass.localPosition = new Vector3(0f, OpticalBenchController.AxisY, 0f);

        _lensMesh = new Mesh { name = "ConvexLens" };
        var body = new GameObject("Body");
        body.transform.SetParent(_glass, false);
        body.AddComponent<MeshFilter>().sharedMesh = _lensMesh;
        var glassMat = LabFactory.Lit(new Color(0.78f, 0.9f, 0.96f, 0.34f), true, 0.02f, 0.96f, true, new Color(0.45f, 0.62f, 0.75f, 0.12f));
        SetCullOff(glassMat);
        body.AddComponent<MeshRenderer>().sharedMaterial = glassMat;

        var rimMesh = new Mesh { name = "LensRim" };
        var rim = new GameObject("Rim");
        rim.transform.SetParent(_glass, false);
        rim.AddComponent<MeshFilter>().sharedMesh = rimMesh;
        var rimMat = LabFactory.Lit(NetFoldTheme.Brass, false, 0.9f, 0.62f);
        SetCullOff(rimMat);
        rim.AddComponent<MeshRenderer>().sharedMaterial = rimMat;
        FillRing(rimMesh, RimInner, RimOuter, 0.006f);

        _focusNear = Mark(bench, "F");
        _focusFar = Mark(bench, "F'");
        _twiceNear = Mark(bench, "2F");
        _twiceFar = Mark(bench, "2F'");
        Apply(0.15f, true);
    }

    public void Apply(float focal, bool guides)
    {
        float center = Mathf.Lerp(0.052f, 0.022f, Mathf.InverseLerp(0.1f, 0.28f, focal));
        FillConvex(_lensMesh, center, 0.008f);
        Place(_focusNear, -focal, guides);
        Place(_focusFar, focal, guides);
        Place(_twiceNear, -2f * focal, guides);
        Place(_twiceFar, 2f * focal, guides);
    }

    static Transform Mark(OpticalBenchController bench, string caption)
    {
        var go = LabFactory.Primitive(PrimitiveType.Sphere, caption, bench.transform, Vector3.zero, Vector3.one * 0.014f, LabFactory.Lit(NetFoldTheme.Brass, false, 0.4f, 0.45f, true, NetFoldTheme.Brass * 0.35f), false);
        Object.Destroy(go.GetComponent<Collider>());
        LabFactory.WorldLabel(go.transform, caption, new Vector3(0f, 0.04f, 0f));
        return go.transform;
    }

    static void Place(Transform mark, float x, bool visible)
    {
        mark.gameObject.SetActive(visible);
        mark.localPosition = new Vector3(x, OpticalBenchController.AxisY, 0f);
    }

    static void SetCullOff(Material mat)
    {
        if (mat != null && mat.HasProperty("_Cull"))
        {
            mat.SetFloat("_Cull", 0f);
        }
    }

    static void FillConvex(Mesh mesh, float centerThick, float edgeThick)
    {
        float halfCenter = centerThick * 0.5f;
        float sag = Mathf.Max(0.001f, halfCenter - edgeThick * 0.5f);
        float curve = (Aperture * Aperture + sag * sag) / (2f * sag);
        int faceVerts = (Rings + 1) * Segments;
        var verts = new Vector3[faceVerts * 2];
        var tris = new List<int>((Rings * Segments * 2 + Segments) * 6);

        for (int i = 0; i <= Rings; i++)
        {
            float r = Aperture * i / Rings;
            float x = halfCenter - (curve - Mathf.Sqrt(Mathf.Max(0f, curve * curve - r * r)));
            for (int j = 0; j < Segments; j++)
            {
                float a = j * Mathf.PI * 2f / Segments;
                int idx = i * Segments + j;
                verts[idx] = new Vector3(x, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                verts[faceVerts + idx] = new Vector3(-x, verts[idx].y, verts[idx].z);
            }
        }

        for (int i = 0; i < Rings; i++)
        {
            for (int j = 0; j < Segments; j++)
            {
                int j2 = (j + 1) % Segments;
                int a = i * Segments + j;
                int b = i * Segments + j2;
                int c = (i + 1) * Segments + j;
                int d = (i + 1) * Segments + j2;
                AddQuad(tris, a, b, d, c);
                AddQuad(tris, faceVerts + a, faceVerts + c, faceVerts + d, faceVerts + b);
            }
        }

        int outer = Rings * Segments;
        for (int j = 0; j < Segments; j++)
        {
            int j2 = (j + 1) % Segments;
            AddQuad(tris, outer + j, faceVerts + outer + j, faceVerts + outer + j2, outer + j2);
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (mesh.normals.Length > 0 && mesh.normals[0].x < 0f)
        {
            Flip(mesh);
        }
    }

    static void FillRing(Mesh mesh, float inner, float outer, float halfThick)
    {
        var verts = new Vector3[Segments * 4];
        var tris = new List<int>(Segments * 24);
        for (int j = 0; j < Segments; j++)
        {
            float a = j * Mathf.PI * 2f / Segments;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            verts[j] = new Vector3(halfThick, c * inner, s * inner);
            verts[Segments + j] = new Vector3(halfThick, c * outer, s * outer);
            verts[Segments * 2 + j] = new Vector3(-halfThick, c * inner, s * inner);
            verts[Segments * 3 + j] = new Vector3(-halfThick, c * outer, s * outer);
        }

        for (int j = 0; j < Segments; j++)
        {
            int n = (j + 1) % Segments;
            int fi = j;
            int fo = Segments + j;
            int bi = Segments * 2 + j;
            int bo = Segments * 3 + j;
            int fi2 = n;
            int fo2 = Segments + n;
            int bi2 = Segments * 2 + n;
            int bo2 = Segments * 3 + n;
            AddQuad(tris, fi, fo, fo2, fi2);
            AddQuad(tris, bi, bi2, bo2, bo);
            AddQuad(tris, fo, bo, bo2, fo2);
            AddQuad(tris, fi, fi2, bi2, bi);
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    static void AddQuad(List<int> tris, int a, int b, int c, int d)
    {
        tris.Add(a);
        tris.Add(b);
        tris.Add(c);
        tris.Add(a);
        tris.Add(c);
        tris.Add(d);
    }

    static void Flip(Mesh mesh)
    {
        var t = mesh.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            int tmp = t[i];
            t[i] = t[i + 1];
            t[i + 1] = tmp;
        }

        mesh.triangles = t;
        mesh.RecalculateNormals();
    }
}
