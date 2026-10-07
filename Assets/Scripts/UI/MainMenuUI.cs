using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public void Build()
    {
        DressWorld();
        var canvas = UiFactory.CreateOverlay("MainMenuCanvas", transform);
        var root = canvas.transform;
        UiFactory.ScreenWash(root);

        var eyebrow = UiFactory.Label(root, "Eyebrow", "科学工作室", 18, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 292f), new Vector2(760f, 328f), NetFoldTheme.Hairline);
        eyebrow.fontStyle = FontStyles.Bold;
        UiFactory.Label(root, "Title", "AR Science", 64, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 200f), new Vector2(860f, 292f));
        UiFactory.Label(root, "Sub", "先理解原理，再动手实验，最后挑战三星。", 22, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 148f), new Vector2(820f, 204f), NetFoldTheme.TextDim);

        Module(root, "NetFold", "立体图形", "展开、三视图与截面", -6f, 136f, NetFoldTheme.Accent, SceneLoader.LoadNetFold, SceneLoader.LoadNetFoldFree, SceneLoader.LoadNetFoldChallenge);
        Module(root, "ParticleDrift", "分子热运动", "扩散、温度与微观粒子", -164f, -22f, new Color(0.78f, 0.52f, 0.58f, 1f), SceneLoader.LoadParticleDrift, SceneLoader.LoadParticleDriftFree, SceneLoader.LoadParticleDriftChallenge);
        Module(root, "LensCraft", "凸透镜成像", "物距、像距与光路", -322f, -180f, NetFoldTheme.Hairline, SceneLoader.LoadLensCraft, SceneLoader.LoadLensCraftFree, SceneLoader.LoadLensCraftChallenge);

        UiFactory.Button(root, "Quit", "退出", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -394f), new Vector2(280f, -342f), QuitApp, new Color(0.12f, 0.13f, 0.15f, 0.92f));
        int total = ChallengeProgress.BestStars("NetFold") + ChallengeProgress.BestStars("ParticleDrift") + ChallengeProgress.BestStars("LensCraft");
        UiFactory.Label(root, "Progress", "探索进度  " + total + " / 9 星   ·   最佳成绩自动保存", 20, TextAlignmentOptions.Right, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -394f), new Vector2(884f, -342f), NetFoldTheme.Hairline);
        UiFactory.Label(root, "Hint", "右键旋转视角    滚轮缩放    WASD 平移    Esc 返回", 18, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, -450f), new Vector2(900f, -402f), NetFoldTheme.TextDim);
    }

    static void Module(Transform root, string id, string title, string blurb, float y0, float y1, Color accent, UnityAction learn, UnityAction free, UnityAction challenge)
    {
        var card = UiFactory.Panel(root, id, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, y0), new Vector2(900f, y1), NetFoldTheme.Glass);
        var line = card.transform.Find("Hairline");
        if (line != null)
        {
            line.GetComponent<Image>().color = accent;
        }

        UiFactory.Label(card.transform, "Name", title, 28, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0.46f, 1f), new Vector2(24f, -44f), new Vector2(0f, -12f));
        UiFactory.Label(card.transform, "Blurb", blurb, 18, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -72f), new Vector2(-20f, -44f), NetFoldTheme.TextDim);
        int best = ChallengeProgress.BestStars(id);
        UiFactory.Label(card.transform, "Best", best == 0 ? "等待探索  ☆☆☆" : "最佳成绩  " + ChallengeProgress.StarsText(best), 20, TextAlignmentOptions.Right, new Vector2(0.46f, 1f), Vector2.one, new Vector2(0f, -44f), new Vector2(-24f, -12f), best == 0 ? NetFoldTheme.TextDim : NetFoldTheme.Hairline);
        UiFactory.Button(card.transform, "Learn", "讲解", Vector2.zero, Vector2.zero, new Vector2(20f, 16f), new Vector2(286f, 64f), learn);
        UiFactory.Button(card.transform, "Free", "自由实验", Vector2.zero, Vector2.zero, new Vector2(298f, 16f), new Vector2(564f, 64f), free);
        UiFactory.Button(card.transform, "Challenge", "挑战", Vector2.zero, Vector2.zero, new Vector2(576f, 16f), new Vector2(808f, 64f), challenge);
    }

    static void DressWorld()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("MenuCamera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        Vector3 hero = new Vector3(1.05f, 1.02f, 0.15f);
        cam.fieldOfView = 32f;
        cam.transform.position = new Vector3(0.1f, 1.42f, -2.5f);
        cam.transform.LookAt(new Vector3(0.28f, 1.12f, 0.55f));

        var root = new GameObject("MenuStudio").transform;
        StudioSet.Install(root, cam, hero);
        var desk = LabFactory.Primitive(PrimitiveType.Cube, "MenuPlinth", root, new Vector3(hero.x, 0.78f, hero.z), new Vector3(0.78f, 0.045f, 0.78f), StudioSet.Stone(), false);
        StudioSet.DressDesk(desk.transform);
        var shape = GeometryFactory.Create(ShapeType.Cube, root, new Vector3(hero.x, 0.805f, hero.z), 0.36f, false);
        shape.AllowIdleSpin = true;
        shape.IdleSpinSpeed = 8f;
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
