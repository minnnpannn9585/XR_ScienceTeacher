using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

public static class NetFoldBootstrap
{
    const string MenuPath = "NetFold/Build Demo Assets";

    [MenuItem(MenuPath)]
    public static void BuildAll()
    {
        EnsureFolders();
        CreatePrefabs();
        ConfigureBuildSettings();
        ConfigureStandaloneOpenXR();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("NetFold demo assets are ready. Open MainMenu or NetFold and press Play.");
    }

    [InitializeOnLoadMethod]
    static void AutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ConfigureBuildSettings();
        };
    }

    public static void EnsureFolders()
    {
        CreateFolder("Assets/Prefabs");
        CreateFolder("Assets/Prefabs/Shapes");
        CreateFolder("Assets/Prefabs/UI");
        CreateFolder("Assets/Materials");
        CreateFolder("Assets/VFX");
        CreateFolder("Assets/Audio");
        CreateFolder("Assets/Scenes");
    }

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                CreateFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void CreatePrefabs()
    {
        CreateShapePrefab(ShapeType.Cube, "Assets/Prefabs/Shapes/Cube.prefab");
        CreateShapePrefab(ShapeType.Cylinder, "Assets/Prefabs/Shapes/Cylinder.prefab");
        CreateShapePrefab(ShapeType.Cone, "Assets/Prefabs/Shapes/Cone.prefab");
        CreateShapePrefab(ShapeType.TriangularPrism, "Assets/Prefabs/Shapes/TriangularPrism.prefab");

        var btn = new GameObject("ToolButton", typeof(RectTransform));
        PrefabUtility.SaveAsPrefabAsset(btn, "Assets/Prefabs/UI/ToolButton.prefab");
        Object.DestroyImmediate(btn);
        var card = new GameObject("MainMenuCard", typeof(RectTransform));
        PrefabUtility.SaveAsPrefabAsset(card, "Assets/Prefabs/UI/MainMenuCard.prefab");
        Object.DestroyImmediate(card);
    }

    static void CreateShapePrefab(ShapeType type, string path)
    {
        var shape = GeometryFactory.Create(type, null, Vector3.zero);
        PrefabUtility.SaveAsPrefabAsset(shape.gameObject, path);
        Object.DestroyImmediate(shape.gameObject);
    }

    static void ConfigureBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/NetFold.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/NetFoldFree.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/NetFoldChallenge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ParticleDrift.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ParticleDriftFree.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ParticleDriftChallenge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/LensCraft.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/LensCraftFree.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/LensCraftChallenge.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
    }

    static void ConfigureStandaloneOpenXR()
    {
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
        if (buildTargetSettings == null)
        {
            return;
        }

        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (settings == null)
        {
            return;
        }

        settings.InitManagerOnStart = true;
    }
}
