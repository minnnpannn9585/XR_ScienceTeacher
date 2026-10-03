using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChallengeScreenQuiz : MonoBehaviour
{
    public TMP_Text TitleText => _title;
    public TMP_Text BodyText => _body;
    public string AnswerText => _answer != null ? _answer.text : string.Empty;

    TMP_Text _title;
    TMP_Text _body;
    Transform _choiceRoot;
    GameObject _answerRoot;
    TMP_InputField _answer;
    UnityAction _submit;
    readonly List<Button> _choiceButtons = new List<Button>();

    public void Build(Transform canvasRoot)
    {
        var panel = UiFactory.Panel(canvasRoot, "Quiz", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-560f, 20f), new Vector2(560f, 268f), NetFoldTheme.Glass);
        _title = UiFactory.Label(panel.transform, "Question", "", 28, TextAlignmentOptions.Left, new Vector2(0f, 0.72f), new Vector2(1f, 1f), new Vector2(24f, 8f), new Vector2(-24f, -8f));
        _body = UiFactory.Label(panel.transform, "Body", "", 22, TextAlignmentOptions.TopLeft, new Vector2(0f, 0.42f), new Vector2(1f, 0.72f), new Vector2(24f, 0f), new Vector2(-24f, 0f));

        var choices = new GameObject("Choices", typeof(RectTransform));
        var choiceRt = choices.GetComponent<RectTransform>();
        choiceRt.SetParent(panel.transform, false);
        choiceRt.anchorMin = new Vector2(0f, 0f);
        choiceRt.anchorMax = new Vector2(1f, 0.42f);
        choiceRt.offsetMin = new Vector2(16f, 12f);
        choiceRt.offsetMax = new Vector2(-16f, -4f);
        _choiceRoot = choices.transform;
        BuildAnswer(panel.transform);
    }

    public void BindSubmit(UnityAction submit)
    {
        _submit = submit;
    }

    public void SetQuestion(string title, string body)
    {
        if (_title != null)
        {
            _title.text = title ?? string.Empty;
        }

        if (_body != null)
        {
            _body.text = body ?? string.Empty;
        }
    }

    public void ShowChoices(string[] labels, Action<int> picked, Color? color = null)
    {
        ClearChoices();
        if (_answerRoot != null)
        {
            _answerRoot.SetActive(false);
        }

        if (labels == null || labels.Length == 0 || _choiceRoot == null)
        {
            return;
        }

        float pad = 0.012f;
        float width = (1f - pad * (labels.Length + 1)) / labels.Length;
        Color buttonColor = color ?? NetFoldTheme.AccentDeep;
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            float x = pad + i * (width + pad);
            Button button = UiFactory.Button(_choiceRoot, "Choice" + i, labels[i], new Vector2(x, 0.08f), new Vector2(x + width, 0.92f), Vector2.zero, Vector2.zero, () => picked?.Invoke(index), buttonColor);
            _choiceButtons.Add(button);
        }
    }

    public void MarkChoice(int index)
    {
        if (index < 0 || index >= _choiceButtons.Count || _choiceButtons[index] == null)
        {
            return;
        }

        var image = _choiceButtons[index].GetComponent<Image>();
        if (image != null)
        {
            image.color = NetFoldTheme.Error;
        }
    }

    public void ClearChoices()
    {
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (_choiceButtons[i] != null)
            {
                Destroy(_choiceButtons[i].gameObject);
            }
        }

        _choiceButtons.Clear();
    }

    public void SetAnswerVisible(bool visible)
    {
        if (visible)
        {
            ClearChoices();
        }

        if (_answerRoot != null)
        {
            _answerRoot.SetActive(visible);
        }

        if (visible && _answer != null)
        {
            _answer.text = string.Empty;
            _answer.ActivateInputField();
        }
    }

    void BuildAnswer(Transform panel)
    {
        _answerRoot = new GameObject("Answer", typeof(RectTransform));
        var root = _answerRoot.GetComponent<RectTransform>();
        root.SetParent(panel, false);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0.42f);
        root.offsetMin = new Vector2(16f, 12f);
        root.offsetMax = new Vector2(-16f, -4f);

        var fieldGo = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var fieldRt = fieldGo.GetComponent<RectTransform>();
        fieldRt.SetParent(root, false);
        fieldRt.anchorMin = new Vector2(0.02f, 0.12f);
        fieldRt.anchorMax = new Vector2(0.72f, 0.88f);
        fieldRt.offsetMin = Vector2.zero;
        fieldRt.offsetMax = Vector2.zero;
        var bg = fieldGo.GetComponent<Image>();
        bg.sprite = UiFactory.RoundSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.12f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(fieldRt, false);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.font = UiFactory.DefaultFont;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 4f);
        textRt.offsetMax = new Vector2(-14f, -4f);

        _answer = fieldGo.GetComponent<TMP_InputField>();
        _answer.textViewport = fieldRt;
        _answer.textComponent = text;
        _answer.pointSize = 24;
        _answer.contentType = TMP_InputField.ContentType.DecimalNumber;

        UiFactory.Button(root, "Submit", "提交", new Vector2(0.74f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero, () => _submit?.Invoke());
        _answerRoot.SetActive(false);
    }
}
