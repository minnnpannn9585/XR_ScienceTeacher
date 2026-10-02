using UnityEngine;

public class ToolbarController : MonoBehaviour
{
    NetFoldLab _lab;
    GameObject _rays;

    public void Build(Transform parent, NetFoldLab lab, GameMode mode)
    {
        _lab = lab;
        float half = mode == GameMode.Learn ? 260f : mode == GameMode.Free ? 230f : 320f;
        var panel = UiFactory.Panel(parent, "Toolbar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24, -half), new Vector2(220, half), NetFoldTheme.Glass);
        string[] names = mode == GameMode.Learn
            ? new[] { "重置", "自动演示", "展开", "折叠", "三视图", "截面" }
            : mode == GameMode.Free
                ? new[] { "重置", "展开", "折叠", "三视图", "截面" }
                : new[] { "旋转", "缩放", "重置", "自动演示", "展开", "折叠", "三视图", "截面", "提示" };
        for (int i = 0; i < names.Length; i++)
        {
            string action = names[i];
            float top = -16 - i * 66;
            UiFactory.Button(panel.transform, action, action, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14, top - 54), new Vector2(-14, top), () => Invoke(action));
        }

        _rays = new GameObject("ProjectionRays", typeof(RectTransform));
        var raysRt = _rays.GetComponent<RectTransform>();
        raysRt.SetParent(panel.transform, false);
        raysRt.anchorMin = new Vector2(0f, 0f);
        raysRt.anchorMax = new Vector2(1f, 0f);
        raysRt.offsetMin = new Vector2(0f, 8f);
        raysRt.offsetMax = new Vector2(0f, 56f);
        UiFactory.Button(raysRt, "FrontRay", "主视投影", new Vector2(0f, 0f), new Vector2(0.33f, 1f), new Vector2(8, 0), new Vector2(-2, 0), () => ToggleRay(0));
        UiFactory.Button(raysRt, "TopRay", "俯视投影", new Vector2(0.33f, 0f), new Vector2(0.66f, 1f), new Vector2(2, 0), new Vector2(-2, 0), () => ToggleRay(1));
        UiFactory.Button(raysRt, "SideRay", "左视投影", new Vector2(0.66f, 0f), new Vector2(1f, 1f), new Vector2(2, 0), new Vector2(-8, 0), () => ToggleRay(2));
        _rays.SetActive(mode == GameMode.Challenge);
    }

    public void SetProjectionRays(bool visible)
    {
        if (_rays != null)
        {
            _rays.SetActive(visible);
        }
    }

    void Invoke(string action)
    {
        if (_lab == null)
        {
            return;
        }

        switch (action)
        {
            case "旋转": _lab.SetTool(InteractTool.Rotate); break;
            case "缩放": _lab.SetTool(InteractTool.Scale); break;
            case "重置": _lab.ResetSelected(); break;
            case "自动演示": _lab.PlayAutoDemo(); break;
            case "展开": _lab.Unfold(); break;
            case "折叠": _lab.Fold(); break;
            case "三视图": _lab.ToggleProjection(); break;
            case "截面": _lab.ToggleSection(); break;
            case "提示": _lab.Hint(); break;
        }
    }

    void ToggleRay(int i)
    {
        _lab.ToggleProjectionRay(i);
    }
}
