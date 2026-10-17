using System;
using UnityEngine;

/// <summary>
/// Material assets under Resources keep the shaders and runtime keyword variants
/// in player builds. Shader.Find alone does not create a build dependency.
/// </summary>
public static class RuntimeMaterials
{
    public const string ResourceFolder = "RuntimeMaterials/";

    public static Material Lit(bool transparent, bool emission)
    {
        return Create("Lit" + (transparent ? "Transparent" : "Opaque") + (emission ? "Emissive" : ""));
    }

    public static Material Unlit(bool transparent)
    {
        return Create(transparent ? "UnlitTransparent" : "UnlitOpaque");
    }

    public static Material Particle()
    {
        return Create("ParticlesUnlit");
    }

    static Material Create(string name)
    {
        var template = Resources.Load<Material>(ResourceFolder + name);
        if (template == null || template.shader == null)
        {
            throw new InvalidOperationException("Missing runtime material: " + name +
                ". Run XR/Prepare Runtime Materials before building.");
        }

        return new Material(template);
    }
}
