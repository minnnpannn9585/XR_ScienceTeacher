using UnityEngine;

/// <summary>Shared, mobile-friendly finishes. Glass remains legible without HDR or bloom.</summary>
public static class MRVisualStyle
{
    public static readonly Color Cyan = new Color(0.48f, 0.91f, 0.96f, 1f);
    public static readonly Color WarmWhite = new Color(0.9f, 0.95f, 0.94f, 1f);
    public static readonly Color Gold = new Color(0.91f, 0.74f, 0.43f, 1f);

    public static Material Glass(Color tint, float opacity = 0.16f)
    {
        var material = RuntimeMaterials.Glass();
        tint.a = Mathf.Clamp01(opacity);
        UrpMaterialUtil.SetColor(material, tint);
        material.SetColor("_RimColor", Color.Lerp(tint, Cyan, 0.55f));
        return material;
    }

    public static Material Edge(Color color)
    {
        var material = RuntimeMaterials.Unlit(color.a < 0.999f);
        UrpMaterialUtil.SetColor(material, color);
        return material;
    }

    public static Material Metal(Color color, float metallic = 0.55f, float smoothness = 0.8f)
    {
        return UrpMaterialUtil.CreateLit(color, false, metallic, smoothness);
    }

    // Explicit registration prevents hiding teaching content by name or material.
    public static void HideInMR(GameObject target)
    {
        if (target == null) return;
        var visibility = target.GetComponent<MRDecorVisibility>();
        if (visibility == null) visibility = target.AddComponent<MRDecorVisibility>();
        visibility.Configure();
    }
}
