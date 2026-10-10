using TMPro;
using UnityEngine;

/// <summary>Rewrites a label when the player changes language.</summary>
public class LocalizedBinding : MonoBehaviour
{
    string _key;
    object[] _args;
    TMP_Text _text;

    public static void Bind(TMP_Text text, string key, params object[] args)
    {
        if (text == null || string.IsNullOrEmpty(key))
        {
            return;
        }

        var binding = text.GetComponent<LocalizedBinding>();
        if (binding == null)
        {
            binding = text.gameObject.AddComponent<LocalizedBinding>();
        }

        binding._text = text;
        binding._key = key;
        binding._args = args != null && args.Length > 0 ? args : null;
        binding.Apply();
    }

    void OnEnable()
    {
        if (_text == null)
        {
            _text = GetComponent<TMP_Text>();
        }

        Loc.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        Loc.Changed -= Apply;
    }

    void Apply()
    {
        if (_text == null || string.IsNullOrEmpty(_key))
        {
            return;
        }

        _text.text = _args == null ? Loc.Get(_key) : Loc.Format(_key, _args);
    }
}
