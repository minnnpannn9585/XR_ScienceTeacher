using System.IO;
using UnityEditor;
using UnityEngine;

public static class ParticleDriftBootstrap
{
    const string VfxPath = "Assets/Resources/VFX/MoleculeMotion.vfx";

    [MenuItem("ParticleDrift/Build Lab Assets")]
    public static void BuildAll()
    {
        EnsureFolders();
        CopyVfxTemplate();
        CreatePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ParticleDrift lab assets are ready.");
    }

    public static void EnsureFolders()
    {
        CreateFolder("Assets/Prefabs/Lab");
        CreateFolder("Assets/Resources/VFX");
        CreateFolder("Assets/Audio");
        CreateFolder("Assets/Materials");
    }

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            CreateFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    public static void CopyVfxTemplate()
    {
        if (File.Exists(VfxPath))
        {
            return;
        }

        EnsureFolders();
        string template = null;
        string[] hits = Directory.GetDirectories("Library/PackageCache", "com.unity.visualeffectgraph@*", SearchOption.TopDirectoryOnly);
        if (hits.Length > 0)
        {
            template = Path.Combine(hits[0], "Editor/Templates/SimpleParticleSystem.vfx");
        }

        if (string.IsNullOrEmpty(template) || !File.Exists(template))
        {
            Debug.LogWarning("VFX template was not found. Molecular motion still uses the CPU particle simulation.");
            return;
        }

        File.Copy(template, VfxPath);
        AssetDatabase.ImportAsset(VfxPath);
    }

    static void CreatePrefabs()
    {
        SavePrimitive(PrimitiveType.Cylinder, "Assets/Prefabs/Lab/Beaker.prefab", new Vector3(0.12f, 0.08f, 0.12f));
        SavePrimitive(PrimitiveType.Capsule, "Assets/Prefabs/Lab/Dropper.prefab", new Vector3(0.03f, 0.08f, 0.03f));
        SavePrimitive(PrimitiveType.Cylinder, "Assets/Prefabs/Lab/Thermometer.prefab", new Vector3(0.02f, 0.1f, 0.02f));
        SavePrimitive(PrimitiveType.Cylinder, "Assets/Prefabs/Lab/GasBottle.prefab", new Vector3(0.05f, 0.08f, 0.05f));
        SavePrimitive(PrimitiveType.Cylinder, "Assets/Prefabs/Lab/GlassCover.prefab", new Vector3(0.2f, 0.06f, 0.2f));
    }

    static void SavePrimitive(PrimitiveType type, string path, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = Path.GetFileNameWithoutExtension(path);
        go.transform.localScale = scale;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
