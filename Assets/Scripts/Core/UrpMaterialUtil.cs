using UnityEngine;
using UnityEngine.Rendering;

public static class UrpMaterialUtil
{
    public static Material CreateLit(Color color, bool transparent, float metallic = 0.15f, float smoothness = 0.72f, bool emission = false, Color emissionColor = default)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var mat = new Material(shader) { name = "NetFoldLit", color = color };
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallic);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        if (mat.HasProperty("_SpecularHighlights"))
        {
            mat.SetFloat("_SpecularHighlights", 1f);
        }

        if (mat.HasProperty("_EnvironmentReflections"))
        {
            mat.SetFloat("_EnvironmentReflections", 1f);
        }

        if (transparent)
        {
            SetTransparent(mat);
        }

        if (emission)
        {
            SetEmission(mat, emissionColor == default ? color : emissionColor);
        }

        return mat;
    }

    public static void SetTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
        }

        if (mat.HasProperty("_Blend"))
        {
            mat.SetFloat("_Blend", 0f);
        }

        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetFloat("_ZWrite", 0f);
        }

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    public static void SetEmission(Material mat, Color color)
    {
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", color);
        }

        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    public static void SetColor(Material mat, Color color)
    {
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
    }
}
