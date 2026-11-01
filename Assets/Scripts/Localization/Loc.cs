using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Reads the Unity Localization "UI" string table and falls back to <see cref="UiCopy"/>
/// until that table is available. Restores the saved language at startup and
/// keeps it synchronized with Unity's locale selector during play.
/// </summary>
public static class Loc
{
    public const string Table = "UI";
    public const string Chinese = "zh-Hans";
    public const string English = "en";
    const string PrefKey = "ScienceStudio.Locale";

    static bool _tablesUnavailable;
    static bool _tableRequested;
    static bool _initialized;
    static int _tableRequestVersion;
    static StringTable _table;
    static string _tableLocale;

    public static event Action Changed;

    public static bool IsEnglish => PreferredCode().StartsWith("en");

    public static string PreferredCode()
    {
        return PlayerPrefs.GetString(PrefKey, Chinese);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        _initialized = false;
        InvalidateTable();
        try
        {
            // A named, idempotent subscription also supports disabled domain reload.
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            var op = LocalizationSettings.InitializationOperation;
            if (op.IsDone)
            {
                FinishInitialization(op);
            }
            else
            {
                op.Completed -= FinishInitialization;
                op.Completed += FinishInitialization;
            }
        }
        catch (Exception)
        {
            // String tables are optional until the editor import has run.
        }
    }

    static void FinishInitialization(AsyncOperationHandle<LocalizationSettings> operation)
    {
        // Do not let the startup selector overwrite the user's saved choice.
        InvalidateTable();
        ApplyPreferred();
        _initialized = true;
        Changed?.Invoke();
    }

    static void OnSelectedLocaleChanged(Locale locale)
    {
        if (!_initialized || locale == null) return;

        // Unity can deliver this event after async table preloading. Ignore an
        // earlier selection if another language has already been chosen.
        var current = LocalizationSettings.SelectedLocaleAsync;
        if (!current.IsValid() || !current.IsDone || current.Result != locale) return;

        string code = locale.Identifier.Code;
        if (code != Chinese && code != English) return;
        if (PreferredCode() != code)
        {
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
        }

        InvalidateTable();
        Changed?.Invoke();
    }

    static void InvalidateTable()
    {
        _tablesUnavailable = false;
        _tableRequested = false;
        _table = null;
        _tableLocale = null;
        ++_tableRequestVersion;
    }

    public static void SetLocale(string code)
    {
        if (code != Chinese && code != English)
        {
            code = Chinese;
        }

        PlayerPrefs.SetString(PrefKey, code);
        PlayerPrefs.Save();
        InvalidateTable();
        ApplyPreferred();
        Changed?.Invoke();
    }

    public static string Get(string key)
    {
        if (TryTable(key, null, out string value))
        {
            return value;
        }

        return UiCopy.Fallback(key, IsEnglish);
    }

    public static string Format(string key, params object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Get(key);
        }

        if (TryTable(key, args, out string value))
        {
            return value;
        }

        return string.Format(UiCopy.Fallback(key, IsEnglish), args);
    }

    public static string Action(string id)
    {
        string key;
        switch (id)
        {
            case "重置": key = "common.reset"; break;
            case "自动演示": key = "common.demo"; break;
            case "旋转": key = "tool.rotate"; break;
            case "缩放": key = "tool.scale"; break;
            case "展开": key = "tool.unfold"; break;
            case "折叠": key = "tool.fold"; break;
            case "三视图": key = "tool.views"; break;
            case "截面": key = "tool.section"; break;
            case "宏观/微观": key = "drift.tool.view"; break;
            case "浓度热力图": key = "drift.tool.heat"; break;
            case "温度曲线": key = "drift.tool.curve"; break;
            case "显示光路": key = "lens.tool.rays"; break;
            case "辅助线": key = "lens.tool.guides"; break;
            case "景深": key = "lens.tool.dof"; break;
            default: return id;
        }

        return Get(key);
    }

    public static string ModeName(GameMode mode)
    {
        if (mode == GameMode.Free) return Get("common.free");
        if (mode == GameMode.Learn) return Get("common.lesson");
        return Get("common.challengeMode");
    }

    public static string Branded(string brand, GameMode mode)
    {
        string modeKey = mode == GameMode.Free ? "common.free" : mode == GameMode.Learn ? "common.lesson" : "common.challenge";
        return Format("title.branded", brand, Get(modeKey));
    }

    static void ApplyPreferred()
    {
        try
        {
            var provider = LocalizationSettings.AvailableLocales;
            if (provider == null)
            {
                return;
            }

            // Locales.GetLocale waits on Addressables. On Quest that wait never
            // returns from the main thread, so the system loading screen stays up.
            if (provider is IPreloadRequired preload)
            {
                var operation = preload.PreloadOperation;
                if (!operation.IsValid() || !operation.IsDone)
                {
                    return;
                }
            }

            var locale = provider.GetLocale(PreferredCode());
            if (locale == null)
            {
                return;
            }

            var current = LocalizationSettings.SelectedLocaleAsync;
            if (current.IsValid() && current.IsDone && current.Result == locale)
            {
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
        }
        catch (Exception)
        {
        }
    }

    static bool TryTable(string key, object[] args, out string value)
    {
        value = null;
        if (_tablesUnavailable)
        {
            return false;
        }

        try
        {
            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
            {
                return false;
            }

            var localeOp = LocalizationSettings.SelectedLocaleAsync;
            if (!localeOp.IsValid() || !localeOp.IsDone || localeOp.Result == null)
            {
                return false;
            }

            Locale locale = localeOp.Result;
            string code = locale.Identifier.Code;
            if (IsEnglish)
            {
                if (!code.StartsWith("en")) return false;
            }
            else if (!code.StartsWith("zh"))
            {
                return false;
            }

            if (_table == null || _tableLocale != code)
            {
                if (!_tableRequested)
                {
                    _tableRequested = true;
                    string requested = code;
                    int version = _tableRequestVersion;
                    var handle = LocalizationSettings.StringDatabase.GetTableAsync(Table, locale);
                    handle.Completed += operation => FinishTableLoad(operation, requested, version);
                }

                if (_table == null || _tableLocale != code)
                {
                    return false;
                }
            }

            StringTableEntry entry = _table.GetEntry(key);
            if (entry == null || string.IsNullOrEmpty(entry.Value))
            {
                return false;
            }

            value = args == null || args.Length == 0 ? entry.Value : string.Format(entry.Value, args);
            return true;
        }
        catch (Exception)
        {
            _tablesUnavailable = true;
            return false;
        }
    }

    static void FinishTableLoad(AsyncOperationHandle<StringTable> operation, string requested, int version)
    {
        if (version != _tableRequestVersion) return;
        _tableRequested = false;
        try
        {
            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
            {
                _tablesUnavailable = true;
                return;
            }

            _table = operation.Result;
            _tableLocale = requested;
            Changed?.Invoke();
        }
        catch (Exception)
        {
            _tablesUnavailable = true;
        }
    }
}
