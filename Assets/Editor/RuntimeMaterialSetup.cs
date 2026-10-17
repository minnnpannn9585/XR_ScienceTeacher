using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public class RuntimeMaterialSetup : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    static RuntimeMaterialSetup()
    {
        EditorApplication.delayCall += EnsureAssets;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureAssets();
    }

    [MenuItem("XR/Prepare Runtime Materials")]
    public static void EnsureAssets()
    {
        const string folder = "Assets/Resources/RuntimeMaterials";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "RuntimeMaterials");
        }

        foreach (bool transparent in new[] { false, true })
        {
            foreach (bool emission in new[] { false, true })
            {
                Ensure("Lit" + (transparent ? "Transparent" : "Opaque") + (emission ? "Emissive" : ""),
                    "Universal Render Pipeline/Lit", transparent, emission);
            }

            Ensure(transparent ? "UnlitTransparent" : "UnlitOpaque", "Universal Render Pipeline/Unlit", transparent, false);
        }

        Ensure("ParticlesUnlit", "Universal Render Pipeline/Particles/Unlit", false, false);
        AssetDatabase.SaveAssets();
    }

    static void Ensure(string name, string shaderName, bool transparent, bool emission)
    {
        string path = "Assets/Resources/RuntimeMaterials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null && existing.shader != null && existing.shader.name == shaderName &&
            (!transparent || !existing.HasProperty("_BlendModePreserveSpecular") || existing.GetFloat("_BlendModePreserveSpecular") == 0f))
        {
            return;
        }

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new BuildFailedException("Required runtime shader is missing: " + shaderName);
        }

        var material = existing != null ? existing : new Material(shader);
        material.shader = shader;
        material.name = name;
        material.enableInstancing = true;
        material.SetColor("_BaseColor", Color.white);
        if (transparent)
        {
            UrpMaterialUtil.SetTransparent(material);
        }

        if (emission)
        {
            UrpMaterialUtil.SetEmission(material, Color.white);
        }

        if (existing == null)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }
    }
}
