using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Run in a separate editor with -batchmode -executeMethod LocaleSwitchChecks.Run (without -quit).</summary>
[InitializeOnLoad]
public static class LocaleSwitchChecks
{
    const string Key = "LocaleSwitchChecks";
    const string Preference = "ScienceStudio.Locale";
    const string Output = "Logs/LocaleSwitchChecks/results.txt";

    [Serializable] class SavedScene { public string path; public bool loaded, active; }
    [Serializable] class SavedScenes { public SavedScene[] items; }

    static LocaleSwitchChecks()
    {
        EditorApplication.playModeStateChanged += PlayState;
        if (SessionState.GetBool(Key + ".Active", false)) Attach();
    }

    [MenuItem("Science Studio/Check Editor Language Switching")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run the language checks outside Play Mode.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save scene changes before running language checks.");

        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        SessionState.SetString(Key + ".Scenes", JsonUtility.ToJson(new SavedScenes
        {
            items = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene
                { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray()
        }));
        SessionState.SetBool(Key + ".HadPreference", PlayerPrefs.HasKey(Preference));
        SessionState.SetString(Key + ".Preference", PlayerPrefs.GetString(Preference, Loc.Chinese));
        SessionState.SetBool(Key + ".Active", true);
        SessionState.SetBool(Key + ".Finishing", false);
        SessionState.SetInt(Key + ".Stage", 0);
        Attach();

        // Set the stored choice without calling Loc: startup must restore it even
        // though this project's SpecificLocaleSelector defaults to Chinese.
        PlayerPrefs.SetString(Preference, Loc.English);
        PlayerPrefs.Save();
        SetDeadline();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.isPlaying = true;
    }

    static void Attach()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }

    static void SetDeadline()
    {
        SessionState.SetFloat(Key + ".NotBefore", (float)EditorApplication.timeSinceStartup + 2f);
        SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 90f);
    }

    static void Advance(int next)
    {
        SessionState.SetInt(Key + ".Stage", next);
        SetDeadline();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key + ".Active", false) || SessionState.GetBool(Key + ".Finishing", false)) return;
        try
        {
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".Deadline", 0))
                throw new TimeoutException("Language checks timed out at stage " + SessionState.GetInt(Key + ".Stage", 0));
            if (!Application.isPlaying || EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".NotBefore", 0)) return;
            var initialization = LocalizationSettings.InitializationOperation;
            if (!initialization.IsDone) return;
            var locale = LocalizationSettings.SelectedLocaleAsync;
            if (!locale.IsValid() || !locale.IsDone || locale.Result == null) return;
            if (UnityEngine.Object.FindObjectOfType<MainMenuUI>() == null) return;

            switch (SessionState.GetInt(Key + ".Stage", 0))
            {
                case 0:
                    CheckLanguage(Loc.English, "Saved English on first startup");
                    SelectInUnity(Loc.Chinese);
                    Advance(1);
                    break;
                case 1:
                    CheckLanguage(Loc.Chinese, "Unity Game view selects Chinese");
                    SelectInUnity(Loc.English);
                    Advance(2);
                    break;
                case 2:
                    CheckLanguage(Loc.English, "Unity Game view selects English");
                    Loc.SetLocale(Loc.Chinese);
                    Advance(3);
                    break;
                case 3:
                    CheckLanguage(Loc.Chinese, "In-game setting selects Chinese");
                    Loc.SetLocale(Loc.English);
                    Advance(4);
                    break;
                case 4:
                    CheckLanguage(Loc.English, "In-game setting selects English");
                    SelectInUnity(Loc.Chinese);
                    SelectInUnity(Loc.English);
                    SelectInUnity(Loc.Chinese);
                    Advance(5);
                    break;
                case 5:
                    CheckLanguage(Loc.Chinese, "Rapid selections finish in Chinese");
                    SelectInUnity(Loc.English);
                    SelectInUnity(Loc.Chinese);
                    SelectInUnity(Loc.English);
                    Advance(6);
                    break;
                case 6:
                    CheckLanguage(Loc.English, "Rapid selections finish in English");
                    // A real stop/start catches startup selectors overwriting the
                    // language that was just selected through Unity's dropdown.
                    Advance(7);
                    EditorApplication.isPlaying = false;
                    break;
                case 8:
                    CheckLanguage(Loc.English, "Saved English after Play Mode restart");
                    Finish(true, "PASS: Real MainMenu startup restores saved English; Unity SelectedLocale changes synchronize saved language, Loc.Get, Loc.IsEnglish, LocalizedBinding labels and custom menu text in both directions; in-game Loc.SetLocale updates Unity's selected locale; rapid alternation keeps the latest language; English survives a Play Mode restart.");
                    break;
            }
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    static void SelectInUnity(string code)
    {
        // This is the same public API used by the Localization Game view menu.
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        Require(locale != null, "Configured locale exists: " + code);
        LocalizationSettings.SelectedLocale = locale;
    }

    static void CheckLanguage(string code, string context)
    {
        bool english = code == Loc.English;
        Require(LocalizationSettings.SelectedLocale.Identifier.Code == code, context + ": Unity locale");
        Require(PlayerPrefs.GetString(Preference) == code, context + ": stored preference");
        Require(Loc.PreferredCode() == code && Loc.IsEnglish == english, context + ": Loc language");
        Require(Loc.Get("common.settings") == (english ? "Settings" : "设置"), context + ": localized lookup");

        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        Require(menu != null && menu.CurrentSubjectIndex == -1, context + ": real main menu");
        var settings = menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "Settings").GetComponentInChildren<TMP_Text>(true);
        Require(settings.GetComponent<LocalizedBinding>() != null, context + ": settings label has binding");
        Require(settings.text == (english ? "Settings" : "设置"), context + ": bound settings label");
        var eyebrow = menu.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Eyebrow");
        Require(eyebrow.text == (english ? "Science Studio" : "科学工作室"), context + ": bound heading");
        var mathematics = menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "Mathematics");
        Require(mathematics.transform.Find("Label").GetComponent<TMP_Text>().text == (english ? "Mathematics" : "数学"), context + ": custom main menu text");
        Debug.Log("LOCALE_SWITCH_CHECK: " + context);
    }

    static void PlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + ".Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { SetDeadline(); return; }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (SessionState.GetBool(Key + ".Finishing", false)) { RestoreAndExit(); return; }
        if (SessionState.GetInt(Key + ".Stage", 0) == 7)
        {
            Advance(8);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }
        else Finish(false, "Language check was stopped before completion.");
    }

    static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) Finish(false, message + "\n" + trace);
    }

    static void Finish(bool success, string message)
    {
        if (SessionState.GetBool(Key + ".Finishing", false)) return;
        SessionState.SetBool(Key + ".Finishing", true);
        SessionState.SetBool(Key + ".Success", success);
        Application.logMessageReceived -= Log;
        File.WriteAllText(Output, message);
        if (success) Debug.Log("LOCALE_SWITCH_CHECKS_PASSED");
        else Debug.LogError(message);
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else RestoreAndExit();
    }

    static void RestoreAndExit()
    {
        bool success = SessionState.GetBool(Key + ".Success", false);
        SessionState.SetBool(Key + ".Active", false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        try
        {
            if (SessionState.GetBool(Key + ".HadPreference", false))
                PlayerPrefs.SetString(Preference, SessionState.GetString(Key + ".Preference", Loc.Chinese));
            else PlayerPrefs.DeleteKey(Preference);
            PlayerPrefs.Save();
            var scenes = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(Key + ".Scenes", "{}"));
            if (scenes.items != null && scenes.items.Any(s => s.loaded && s.active && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(scenes.items.Select(s => new SceneSetup
                    { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
        }
        finally { if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); }
    }

    static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Locale switch check failed: " + description);
    }
}
