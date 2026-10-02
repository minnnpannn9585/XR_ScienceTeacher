using TMPro;
using UnityEngine;

public static class LabFactory
{
    public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        if (!keepCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }
        }

        if (mat != null)
        {
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        return go;
    }

    public static Material Lit(Color color, bool transparent, float metallic = 0.12f, float smoothness = 0.65f, bool emission = false, Color emissionColor = default)
    {
        return UrpMaterialUtil.CreateLit(color, transparent, metallic, smoothness, emission, emissionColor);
    }

    public static Material ParticleMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        return mat;
    }

    public static void WorldLabel(Transform parent, string text, Vector3 localPos)
    {
        if (parent == null || UiFactory.DefaultFont == null)
        {
            return;
        }

        var canvas = UiFactory.CreateWorld("Label", parent, parent.TransformPoint(localPos), new Vector2(280f, 80f), Vector3.zero);
        canvas.transform.localScale = Vector3.one * 0.00055f;
        UiFactory.Label(canvas.transform, "Text", text, 42, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        canvas.gameObject.AddComponent<FaceCamera>();
    }
}

public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 to = cam.transform.position - transform.position;
        if (to.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(to);
    }
}
