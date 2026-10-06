using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetFoldUI : MonoBehaviour
{
    public ShapeDataPanel DataPanel { get; private set; }
    public ResultPanel Result { get; private set; }
    public TMP_Text GuideTitle;
    public TMP_Text GuideBody;
    public TMP_Text ModeLabel;
    public TMP_Text ChallengeLabel;
    public ToolbarController Toolbar { get; private set; }

    ChallengeScreenQuiz _quiz;
    Image[] _shapeButtons;
    readonly ShapeType[] _shapeOrder = { ShapeType.Cube, ShapeType.Cylinder, ShapeType.Cone, ShapeType.TriangularPrism };

    public void Build(NetFoldLab lab, GameMode mode)
    {
        if (mode == GameMode.Challenge)
        {
            var quizCanvas = UiFactory.CreateOverlay("NetFoldHUD", transform);
            _quiz = gameObject.AddComponent<ChallengeScreenQuiz>();
            _quiz.Build(quizCanvas.transform);
            GuideTitle = _quiz.TitleText;
            GuideBody = _quiz.BodyText;
            return;
        }

        var canvas = UiFactory.CreateOverlay("NetFoldHUD", transform);
        var root = canvas.transform;

        var top = UiFactory.Panel(root, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24, -96), new Vector2(-24, -16), NetFoldTheme.Glass);
        UiFactory.Label(top.transform, "Title", mode == GameMode.Free ? "NetFold · 自由实验" : mode == GameMode.Learn ? "NetFold · 讲解" : "NetFold · 挑战", 34, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(24, 8), new Vector2(-10, -8));
        ModeLabel = UiFactory.Label(top.transform, "Mode", mode == GameMode.Free ? "自由实验" : mode == GameMode.Learn ? "讲解" : "挑战模式", 22, TextAlignmentOptions.Center, new Vector2(0.64f, 0.18f), new Vector2(0.84f, 0.82f), Vector2.zero, Vector2.zero);
        UiFactory.Button(top.transform, "Back", "返回", new Vector2(0.86f, 0.18f), new Vector2(0.99f, 0.82f), Vector2.zero, Vector2.zero, SceneLoader.LoadMainMenu);
        if (mode == GameMode.Free)
        {
            BuildShapePicker(root, lab);
        }

        var right = UiFactory.Panel(root, "Data", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-300, -250), new Vector2(-24, 250), NetFoldTheme.Glass);
        var title = UiFactory.Label(right.transform, "Name", "未选择几何体", 28, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -56), new Vector2(-16, -16));
        var faces = UiFactory.Label(right.transform, "Faces", "面数  --", 24, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -108), new Vector2(-16, -60));
        var edges = UiFactory.Label(right.transform, "Edges", "棱数  --", 24, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -156), new Vector2(-16, -108));
        var verts = UiFactory.Label(right.transform, "Verts", "顶点数  --", 24, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -204), new Vector2(-16, -156));
        var view = UiFactory.Label(right.transform, "View", mode == GameMode.Free ? "当前视图  自由实验" : "当前视图  讲解", 22, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -260), new Vector2(-16, -210));
        var section = UiFactory.Label(right.transform, "Section", "截面边数  --", 22, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -312), new Vector2(-16, -264));
        var proj = UiFactory.Label(right.transform, "Proj", "投影线  关", 20, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -380), new Vector2(-16, -318));
        DataPanel = gameObject.AddComponent<ShapeDataPanel>();
        DataPanel.Bind(faces, edges, verts, view, section, proj, title);

        bool guided = mode == GameMode.Learn;
        var bottom = UiFactory.Panel(root, "Guide", new Vector2(0f, 0f), new Vector2(1f, 0f), guided ? new Vector2(24, 16) : new Vector2(250, 18), guided ? new Vector2(-330, 250) : new Vector2(-250, 150), NetFoldTheme.Glass);
        string guideTitle = mode == GameMode.Free ? "自由实验" : GameModeController.StepTitle(LearnStep.Recognize);
        string guideBody = mode == GameMode.Free ? "从顶部选择一个几何体。一次只显示一个，可以旋转、展开、看三视图，或拖动截面。" : GameModeController.StepHint(LearnStep.Recognize);
        GuideTitle = UiFactory.Label(bottom.transform, "Step", guideTitle, 26, TextAlignmentOptions.Left, new Vector2(0f, guided ? 0.78f : 0.55f), new Vector2(0.72f, 1f), new Vector2(20, 0), new Vector2(-10, -8));
        GuideBody = UiFactory.Label(bottom.transform, "Body", guideBody, 20, TextAlignmentOptions.TopLeft, new Vector2(0f, guided ? 0.08f : 0f), new Vector2(0.72f, guided ? 0.76f : 0.58f), new Vector2(20, 10), new Vector2(-10, 0));
        ChallengeLabel = UiFactory.Label(bottom.transform, "ChallengeMsg", "", 20, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0.72f, 0.55f), new Vector2(20, 8), new Vector2(-10, 0));
        if (mode == GameMode.Learn)
        {
            UiFactory.Button(bottom.transform, "Prev", "上一步", new Vector2(0.76f, 0.55f), new Vector2(0.87f, 0.92f), Vector2.zero, Vector2.zero, lab.PrevStep);
            UiFactory.Button(bottom.transform, "Next", "下一步", new Vector2(0.88f, 0.55f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero, lab.NextStep);
            UiFactory.Label(bottom.transform, "Keys", "右键旋转视角    滚轮缩放    物体已锁定，只播放讲解动画    Esc 返回", 16, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.72f, 0.12f), new Vector2(20, 6), new Vector2(-8, 0));
        }

        if (mode != GameMode.Learn)
        {
            Toolbar = gameObject.AddComponent<ToolbarController>();
            Toolbar.Build(root, lab, mode);
        }

        Result = gameObject.AddComponent<ResultPanel>();
        Result.Build(root);
    }

    public void HighlightShape(ShapeType type)
    {
        if (_shapeButtons == null)
        {
            return;
        }

        for (int i = 0; i < _shapeButtons.Length; i++)
        {
            bool on = _shapeOrder[i] == type;
            Color color = on ? new Color(0.22f, 0.58f, 0.95f, 1f) : NetFoldTheme.AccentDeep;
            var button = _shapeButtons[i].GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = color;
            button.colors = colors;
            _shapeButtons[i].color = color;
        }
    }

    void BuildShapePicker(Transform root, NetFoldLab lab)
    {
        var picker = UiFactory.Panel(root, "ShapePicker", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -176f), new Vector2(460f, -104f), NetFoldTheme.Glass);
        _shapeButtons = new Image[_shapeOrder.Length];
        for (int i = 0; i < _shapeOrder.Length; i++)
        {
            ShapeType type = _shapeOrder[i];
            float x0 = i / (float)_shapeOrder.Length;
            float x1 = (i + 1) / (float)_shapeOrder.Length;
            var button = UiFactory.Button(picker.transform, type.ToString(), ShapeCatalog.DisplayName(type), new Vector2(x0, 0f), new Vector2(x1, 1f), new Vector2(10f, 10f), new Vector2(-10f, -10f), () => lab.ShowFreeShape(type));
            _shapeButtons[i] = button.GetComponent<Image>();
        }
    }

    public void SetLearnGuide(LearnStep step)
    {
        if (GuideTitle != null) GuideTitle.text = GameModeController.StepTitle(step);
        if (GuideBody != null) GuideBody.text = GameModeController.StepHint(step);
        if (ModeLabel != null) ModeLabel.text = "讲解";
    }

    public void SetChallengeGuide(string title, string body)
    {
        if (_quiz != null)
        {
            _quiz.SetQuestion(title, body);
            return;
        }

        if (GuideTitle != null) GuideTitle.text = title;
        if (GuideBody != null) GuideBody.text = body;
        if (ModeLabel != null) ModeLabel.text = "挑战模式";
    }

    public void ShowChoices(string[] labels, Action<int> picked)
    {
        if (_quiz != null)
        {
            _quiz.ShowChoices(labels, picked, new Color(0.12f, 0.32f, 0.55f, 0.96f));
        }
    }

    public void MarkChoice(int index)
    {
        if (_quiz != null)
        {
            _quiz.MarkChoice(index);
        }
    }

    public void ClearChoices()
    {
        if (_quiz != null)
        {
            _quiz.ClearChoices();
        }
    }
}
