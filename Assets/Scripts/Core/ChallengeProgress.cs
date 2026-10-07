using UnityEngine;

/// <summary>Local best scores, shared by each module's lesson/free/challenge scenes.</summary>
public static class ChallengeProgress
{
    const string Prefix = "ScienceStudio.BestStars.";

    public static string ModuleForScene(string scene)
    {
        switch (scene)
        {
            case SceneLoader.NetFoldScene:
            case SceneLoader.NetFoldFreeScene:
            case SceneLoader.NetFoldChallengeScene: return "NetFold";
            case SceneLoader.ParticleDriftScene:
            case SceneLoader.ParticleDriftFreeScene:
            case SceneLoader.ParticleDriftChallengeScene: return "ParticleDrift";
            case SceneLoader.LensCraftScene:
            case SceneLoader.LensCraftFreeScene:
            case SceneLoader.LensCraftChallengeScene: return "LensCraft";
            default: return null;
        }
    }

    public static int BestStars(string scene)
    {
        string module = ModuleForScene(scene);
        return module == null ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(Prefix + module, 0), 0, 3);
    }

    public static bool Record(string scene, int stars)
    {
        string module = ModuleForScene(scene);
        stars = Mathf.Clamp(stars, 0, 3);
        if (module == null || stars <= BestStars(scene)) return false;
        PlayerPrefs.SetInt(Prefix + module, stars);
        PlayerPrefs.Save();
        return true;
    }

    public static string StarsText(int stars)
    {
        stars = Mathf.Clamp(stars, 0, 3);
        return new string('★', stars) + new string('☆', 3 - stars);
    }
}
