using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    public string Knowledge = "知识卡\n正方体：6 面 12 棱 8 顶点\n圆柱：3 面 2 棱 0 顶点\n圆锥：2 面 1 棱 1 顶点\n三棱柱：5 面 9 棱 6 顶点\n斜切正方体可以得到三角形到六边形的截面。";
    public System.Action Retry;
    public bool IsOpen => _root != null && _root.activeSelf;

    GameObject _root;
    TMP_Text _stars;
    TMP_Text _reason;
    TMP_Text _card;

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

        var panel = UiFactory.Panel(blocker.transform, "Result", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320, -240), new Vector2(320, 240), NetFoldTheme.Glass);
        _stars = UiFactory.Label(panel.transform, "Stars", "★★★", 56, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -90), new Vector2(-20, -16));
        _reason = UiFactory.Label(panel.transform, "Reason", "", 22, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24, -150), new Vector2(-24, -90));
        _card = UiFactory.Label(panel.transform, "Card", "", 20, TextAlignmentOptions.Top, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28, 80), new Vector2(-28, -160));
        UiFactory.Button(panel.transform, "Again", "再试一次", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(24, 20), new Vector2(-8, 72), () =>
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
        UiFactory.Button(panel.transform, "Menu", "返回主菜单", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8, 20), new Vector2(-24, 72), SceneLoader.LoadMainMenu);
        Hide();
    }

    public void Show(int stars, string reason)
    {
        _root.SetActive(true);
        _stars.text = stars <= 0 ? "☆☆☆" : stars == 1 ? "★☆☆" : stars == 2 ? "★★☆" : "★★★";
        _stars.color = stars >= 3 ? NetFoldTheme.Hairline : stars == 2 ? NetFoldTheme.Accent : NetFoldTheme.Error;
        _reason.text = reason;
        _card.text = Knowledge;
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }
}
