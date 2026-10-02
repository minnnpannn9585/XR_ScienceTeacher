using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public const string MainMenuScene = "MainMenu";
    public const string NetFoldScene = "NetFold";
    public const string NetFoldFreeScene = "NetFoldFree";
    public const string NetFoldChallengeScene = "NetFoldChallenge";
    public const string ParticleDriftScene = "ParticleDrift";
    public const string ParticleDriftFreeScene = "ParticleDriftFree";
    public const string ParticleDriftChallengeScene = "ParticleDriftChallenge";
    public const string LensCraftScene = "LensCraft";
    public const string LensCraftFreeScene = "LensCraftFree";
    public const string LensCraftChallengeScene = "LensCraftChallenge";

    public static void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void LoadNetFold()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(NetFoldScene);
    }

    public void GoMainMenu()
    {
        LoadMainMenu();
    }

    public void GoNetFold()
    {
        LoadNetFold();
    }

    public static void LoadNetFoldFree()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(NetFoldFreeScene);
    }

    public static void LoadNetFoldChallenge()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(NetFoldChallengeScene);
    }

    public static void LoadParticleDrift()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ParticleDriftScene);
    }

    public static void LoadParticleDriftFree()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ParticleDriftFreeScene);
    }

    public static void LoadParticleDriftChallenge()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ParticleDriftChallengeScene);
    }

    public void GoParticleDrift()
    {
        LoadParticleDrift();
    }

    public static void LoadLensCraft()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LensCraftScene);
    }

    public static void LoadLensCraftFree()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LensCraftFreeScene);
    }

    public static void LoadLensCraftChallenge()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LensCraftChallengeScene);
    }
}
