using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ResultPanel : MonoBehaviour
{
    static ResultPanel _active;
    public static bool HasOpenPanel => _active != null && _active.IsOpen;
    public string KnowledgeKey = "knowledge.netfold";
    public System.Action Retry;
    public bool IsOpen => _root != null && _root.activeSelf;

    GameObject _root;
    TMP_Text _stars;
    TMP_Text _reason;
    TMP_Text _card;
    TMP_Text _progress;
    CanvasGroup _group;
    RectTransform _panel;
    Button _again;
    GameObject _previousSelection;
    Coroutine _reveal;

    public void Build(Transform canvas)
    {
        var blocker = new GameObject("ResultBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var blockerRt = blocker.GetComponent<RectTransform>();
        blockerRt.SetParent(canvas, false);
        blockerRt.anchorMin = Vector2.zero;
        blockerRt.anchorMax = Vector2.one;
        blockerRt.offsetMin = Vector2.zero;
        blockerRt.offsetMax = Vector2.zero;
        blocker.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.05f, 0.62f);
        _root = blocker;
        _group = blocker.AddComponent<CanvasGroup>();

        var panel = UiFactory.Panel(blocker.transform, "Result", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320, -240), new Vector2(320, 240), NetFoldTheme.Glass);
        _panel = panel.rectTransform;
        _stars = UiFactory.Label(panel.transform, "Stars", "★★★", 56, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -90), new Vector2(-20, -16));
        _reason = UiFactory.Label(panel.transform, "Reason", "", 22, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24, -150), new Vector2(-24, -90));
        _progress = UiFactory.Label(panel.transform, "Progress", "", 20, TextAlignmentOptions.Center, new Vector2(0f, 1f), Vector2.one, new Vector2(24, -186), new Vector2(-24, -150), NetFoldTheme.Hairline);
        _card = UiFactory.Label(panel.transform, "Card", "", 20, TextAlignmentOptions.Top, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28, 80), new Vector2(-28, -200));
        _again = UiFactory.Button(panel.transform, "Again", Loc.Get("common.tryAgain"), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(24, 20), new Vector2(-8, 72), () =>
        {
            Hide();
            if (Retry != null)
            {
                Retry();
            }
            else
            {
                var lab = FindObjectOfType<NetFoldLab>();
                if (lab != null)
                {
                    lab.RestartChallenge();
                }
            }
        });
        UiFactory.Button(panel.transform, "Menu", Loc.Get("common.mainMenu"), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8, 20), new Vector2(-24, 72), SceneLoader.LoadMainMenu);
        Hide();
    }

    public void Show(int stars, string reason)
    {
        if (_root == null) return;
        stars = Mathf.Clamp(stars, 0, 3);
        string scene = SceneManager.GetActiveScene().name;
        bool improved = ChallengeProgress.Record(scene, stars);
        if (!IsOpen && EventSystem.current != null) _previousSelection = EventSystem.current.currentSelectedGameObject;
        _root.SetActive(true);
        _active = this;
        _root.transform.SetAsLastSibling();
        _stars.text = ChallengeProgress.StarsText(stars);
        _stars.color = stars > 0 ? NetFoldTheme.Hairline : NetFoldTheme.TextDim;
        _reason.text = reason;
        string starsText = ChallengeProgress.StarsText(ChallengeProgress.BestStars(scene));
        _progress.text = Loc.Format(improved ? "result.newBest" : "result.history", starsText);
        _card.text = Loc.Get(KnowledgeKey);
        if (_reveal != null) StopCoroutine(_reveal);
        _reveal = StartCoroutine(Reveal());
    }

    public void Hide()
    {
        if (_root != null)
        {
            if (_reveal != null) StopCoroutine(_reveal);
            _reveal = null;
            if (IsOpen && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null);
            _root.SetActive(false);
            if (_active == this) _active = null;
        }
    }

    System.Collections.IEnumerator Reveal()
    {
        _group.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        float elapsed = 0f;
        while (elapsed < 0.24f)
        {
            float t = Mathf.Clamp01(elapsed / 0.24f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            _group.alpha = eased;
            _panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _group.alpha = 1f;
        _panel.localScale = Vector3.one;
        _group.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_again.gameObject);
        _reveal = null;
    }
}
