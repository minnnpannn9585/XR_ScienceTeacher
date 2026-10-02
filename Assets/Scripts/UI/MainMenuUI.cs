using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public void Build()
    {
        var canvas = UiFactory.CreateOverlay("MainMenuCanvas", transform);
        var root = canvas.transform;
        var bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(root, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.03f, 0.07f, 0.14f, 1f);

        UiFactory.Panel(root, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, -460), new Vector2(520, 460), NetFoldTheme.Glass);
        UiFactory.Label(root, "Title", "AR Science", 48, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, 340), new Vector2(460, 420));
        UiFactory.Label(root, "Sub", "讲解、自由实验、挑战分开进入", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, 280), new Vector2(460, 334));

        UiFactory.Label(root, "NetFoldHead", "NetFold · 立体图形", 22, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, 220), new Vector2(450, 264));
        UiFactory.Button(root, "NetFoldLearn", "讲解", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, 156), new Vector2(-160, 214), SceneLoader.LoadNetFold, NetFoldTheme.AccentDeep);
        UiFactory.Button(root, "NetFoldFree", "自由实验", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-145, 156), new Vector2(145, 214), SceneLoader.LoadNetFoldFree, new Color(0.16f, 0.42f, 0.62f, 1f));
        UiFactory.Button(root, "NetFoldChallenge", "挑战", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160, 156), new Vector2(450, 214), SceneLoader.LoadNetFoldChallenge, new Color(0.1f, 0.28f, 0.48f, 1f));

        UiFactory.Label(root, "DriftHead", "ParticleDrift · 分子热运动", 22, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, 90), new Vector2(450, 134));
        UiFactory.Button(root, "ParticleDriftLearn", "讲解", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, 26), new Vector2(-160, 84), SceneLoader.LoadParticleDrift, new Color(0.48f, 0.12f, 0.28f, 1f));
        UiFactory.Button(root, "ParticleDriftFree", "自由实验", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-145, 26), new Vector2(145, 84), SceneLoader.LoadParticleDriftFree, new Color(0.62f, 0.22f, 0.36f, 1f));
        UiFactory.Button(root, "ParticleDriftChallenge", "挑战", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(450, 84), SceneLoader.LoadParticleDriftChallenge, new Color(0.36f, 0.1f, 0.22f, 1f));

        UiFactory.Label(root, "LensHead", "LensCraft · 凸透镜成像", 22, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, -40), new Vector2(450, 4));
        UiFactory.Button(root, "LensCraftLearn", "讲解", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-450, -104), new Vector2(-160, -46), SceneLoader.LoadLensCraft, new Color(0.1f, 0.38f, 0.42f, 1f));
        UiFactory.Button(root, "LensCraftFree", "自由实验", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-145, -104), new Vector2(145, -46), SceneLoader.LoadLensCraftFree, new Color(0.14f, 0.5f, 0.48f, 1f));
        UiFactory.Button(root, "LensCraftChallenge", "挑战", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160, -104), new Vector2(450, -46), SceneLoader.LoadLensCraftChallenge, new Color(0.08f, 0.28f, 0.36f, 1f));

        UiFactory.Button(root, "Quit", "退出", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, -190), new Vector2(180, -132), QuitApp, new Color(0.18f, 0.22f, 0.3f, 0.9f));
        UiFactory.Label(root, "Hint", "右键旋转视角    滚轮缩放    WASD 平移    Esc 返回", 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, -280), new Vector2(460, -210));
    }

    static void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
