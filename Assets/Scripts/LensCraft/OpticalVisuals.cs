using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Owns the runtime-only meshes and materials used by the optical instruments.</summary>
public sealed class OpticalVisuals : MonoBehaviour
{
    readonly HashSet<Object> _resources = new HashSet<Object>();

    public static T Own<T>(Transform owner, T resource) where T : Object
    {
        var resources = owner.GetComponent<OpticalVisuals>();
        if (resources == null) resources = owner.gameObject.AddComponent<OpticalVisuals>();
        resources._resources.Add(resource);
        return resource;
    }

    public static GameObject Part(Transform owner, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
    {
        return LabFactory.Primitive(type, name, owner, position, scale, material, false);
    }

    public static LineRenderer Line(Transform owner, string name, Material material, float width, params Vector3[] points)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(owner, false);
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // One mesh for all graduation marks, rather than a renderer and material per tick.
    public static void Ruler(Transform owner, string name, float halfLength, float step, float y, float z, Material material)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        int count = Mathf.RoundToInt(halfLength / step);
        for (int i = -count; i <= count; i++)
        {
            float x = i * step;
            float length = i % 10 == 0 ? 0.023f : i % 5 == 0 ? 0.016f : 0.009f;
            float width = i % 5 == 0 ? 0.0018f : 0.001f;
            int start = vertices.Count;
            vertices.Add(new Vector3(x - width * 0.5f, y, z));
            vertices.Add(new Vector3(x + width * 0.5f, y, z));
            vertices.Add(new Vector3(x + width * 0.5f, y, z + length));
            vertices.Add(new Vector3(x - width * 0.5f, y, z + length));
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }
        var mesh = Own(owner, new Mesh { name = name });
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var root = new GameObject(name);
        root.transform.SetParent(owner, false);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    public static Mesh FlameMesh(Transform owner)
    {
        const int segments = 20;
        float[] heights = { 0f, 0.1f, 0.25f, 0.45f, 0.7f, 0.9f, 1f };
        float[] radii = { 0f, 0.32f, 0.5f, 0.4f, 0.24f, 0.09f, 0f };
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i < heights.Length; i++)
            for (int j = 0; j < segments; j++)
            {
                float angle = j * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radii[i], heights[i], Mathf.Sin(angle) * radii[i]));
                if (i == heights.Length - 1) continue;
                int a = i * segments + j, b = (i + 1) * segments + j;
                int c = i * segments + (j + 1) % segments, d = (i + 1) * segments + (j + 1) % segments;
                triangles.AddRange(new[] { a, b, d, a, d, c });
            }
        var mesh = Own(owner, new Mesh { name = "Candle Flame" });
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject MeshPart(Transform owner, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var part = new GameObject(name);
        part.transform.SetParent(owner, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part;
    }

    void OnDestroy()
    {
        foreach (var resource in _resources)
            if (resource != null) Destroy(resource);
        _resources.Clear();
    }
}
