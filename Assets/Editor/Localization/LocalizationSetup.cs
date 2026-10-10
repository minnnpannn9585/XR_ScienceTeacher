using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class LocalizationSetup
{
    const string Root = "Assets/Localization";
    const string SettingsPath = Root + "/Localization Settings.asset";
    const string Pref = "ScienceStudio.LocRevision";

    [MenuItem("Science Studio/Build Localization")]
    public static void Build()
    {
        EnsureFolder("Assets", "Localization");
        EnsureFolder(Root, "Tables");
        EnsureSettings();
        Locale zh = EnsureLocale("zh-Hans", "Chinese (Simplified) (zh-Hans)");
        Locale en = EnsureLocale("en", "English (en)");
        AddIfMissing(zh);
        AddIfMissing(en);

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("UI");
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection("UI", Root + "/Tables");
        }

        StringTable zhTable = EnsureTable(collection, zh);
        StringTable enTable = EnsureTable(collection, en);
        Sync(collection, zhTable, enTable);
        collection.SetPreloadTableFlag(true);

        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings != null)
        {
            var selectors = settings.GetStartupLocaleSelectors();
            if (selectors != null)
            {
                selectors.Clear();
                selectors.Add(new CommandLineLocaleSelector());
                selectors.Add(new SpecificLocaleSelector { LocaleId = zh.Identifier });
            }

            EditorUtility.SetDirty(settings);
        }

        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(zhTable);
        EditorUtility.SetDirty(enTable);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetInt(Pref, UiCopy.Revision);
        Debug.Log("UI string tables are ready for Chinese and English.");
    }

    static bool _autoFailed;

    [InitializeOnLoadMethod]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (_autoFailed || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            if (EditorPrefs.GetInt(Pref, 0) == UiCopy.Revision && LocalizationEditorSettings.GetStringTableCollection("UI") != null)
            {
                return;
            }

            try
            {
                Build();
            }
            catch (System.Exception ex)
            {
                _autoFailed = true;
                Debug.LogException(ex);
            }
        };
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(parent))
        {
            string grand = Path.GetDirectoryName(parent)?.Replace("\\", "/");
            string leaf = Path.GetFileName(parent);
            if (!string.IsNullOrEmpty(grand) && !string.IsNullOrEmpty(leaf))
            {
                EnsureFolder(grand, leaf);
            }
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    static LocalizationSettings EnsureSettings()
    {
        LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings != null)
        {
            return settings;
        }

        settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        if (settings.GetAvailableLocales() == null)
        {
            settings.SetAvailableLocales(new LocalesProvider());
        }

        return settings;
    }

    static Locale EnsureLocale(string code, string fileName)
    {
        string path = Root + "/" + fileName + ".asset";
        Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
        if (locale != null)
        {
            return locale;
        }

        locale = Locale.CreateLocale(code);
        AssetDatabase.CreateAsset(locale, path);
        return locale;
    }

    static void AddIfMissing(Locale locale)
    {
        foreach (Locale existing in LocalizationEditorSettings.GetLocales())
        {
            if (existing != null && existing.Identifier.Code == locale.Identifier.Code)
            {
                return;
            }
        }

        LocalizationEditorSettings.AddLocale(locale);
    }

    static StringTable EnsureTable(StringTableCollection collection, Locale locale)
    {
        var table = collection.GetTable(locale.Identifier) as StringTable;
        if (table == null)
        {
            table = collection.AddNewTable(locale.Identifier) as StringTable;
        }

        return table;
    }

    static void Sync(StringTableCollection collection, StringTable zhTable, StringTable enTable)
    {
        for (int i = 0; i < UiCopy.All.Length; i++)
        {
            UiCopy.Entry entry = UiCopy.All[i];
            SharedTableData.SharedTableEntry shared = collection.SharedData.GetEntry(entry.Key);
            if (shared == null)
            {
                shared = collection.SharedData.AddKey(entry.Key);
            }

            Write(zhTable, shared.Id, entry.Zh);
            Write(enTable, shared.Id, entry.En);
        }
    }

    static void Write(StringTable table, long id, string value)
    {
        StringTableEntry row = table.GetEntry(id);
        if (row == null)
        {
            row = table.AddEntry(id, value);
        }
        else
        {
            row.Value = value;
        }

        row.IsSmart = value != null && value.IndexOf('{') >= 0;
    }
}
