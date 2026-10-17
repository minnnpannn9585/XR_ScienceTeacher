using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Headsets have no keyboard. A pokeable pad types into the focused numeric field.
/// </summary>
public class XRNumericPad : MonoBehaviour
{
    TMP_InputField _field;
    GameObject _root;

    bool _dismissed;
    TMP_InputField _closedField;

    void Update()
    {
        if (!XRSession.IsActive || ResultPanel.HasOpenPanel)
        {
            Hide();
            return;
        }

        if (_field != null && !_field.gameObject.activeInHierarchy)
        {
            Hide();
        }

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
        if (field == null || !field.isFocused)
        {
            _dismissed = false;
            return;
        }

        if (_dismissed && field == _closedField)
        {
            return;
        }

        var canvas = field.GetComponentInParent<Canvas>();
        if (_root == null || !_root.activeSelf || field != _field ||
            (canvas != null && _root.transform.parent != canvas.transform))
        {
            Show(field);
        }
    }

    void Show(TMP_InputField field)
    {
        _field = field;
        field.shouldHideMobileInput = true;
        var canvas = field.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (_root == null)
        {
            Build(canvas.transform);
        }
        else
        {
            _root.transform.SetParent(canvas.transform, false);
        }

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    void ClosePad()
    {
        _dismissed = true;
        _closedField = _field;
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    void Hide()
    {
        _field = null;
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    void Build(Transform parent)
    {
        var panel = UiFactory.Panel(parent, "XRNumericPad", Vector2.zero, Vector2.zero,
            new Vector2(24f, 100f), new Vector2(576f, 516f), NetFoldTheme.Glass);
        _root = panel.gameObject;
        string[] keys = { "7", "8", "9", "4", "5", "6", "1", "2", "3", ".", "0", "⌫" };
        for (int i = 0; i < keys.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            string key = keys[i];
            float x0 = 16f + column * 176f;
            float y1 = -16f - row * 80f;
            UiFactory.Button(panel.transform, "Key" + i, key, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, y1 - 68f), new Vector2(x0 + 164f, y1), () => Press(key));
        }

        var done = UiFactory.Button(panel.transform, "ClosePad", Loc.Get("xr.keyboardDone"), Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 80f), ClosePad);
        LocalizedBinding.Bind(done.GetComponentInChildren<TMP_Text>(), "xr.keyboardDone");
    }

    void Press(string key)
    {
        if (_field == null)
        {
            return;
        }

        if (key == "⌫")
        {
            if (_field.text.Length > 0)
            {
                _field.text = _field.text.Substring(0, _field.text.Length - 1);
            }
        }
        else if (key == "." && _field.text.Contains("."))
        {
            return;
        }
        else
        {
            _field.text += key;
        }

        _field.caretPosition = _field.text.Length;
        _field.onValueChanged?.Invoke(_field.text);
    }
}
