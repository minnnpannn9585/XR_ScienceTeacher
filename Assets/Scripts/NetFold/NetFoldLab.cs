using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class NetFoldLab : MonoBehaviour
{
    public InputAdapter Input;
    public GameModeController Modes;
    public UnfoldController Unfolding;
    public ProjectionController Projection;
    public SectionController Section;
    public ChallengeController Challenge;
    public StarRatingController Stars;
    public NetFoldUI UI;
    public Transform DeskAnchor;
    public Transform Stage;

    readonly List<ShapeController> _learnShapes = new List<ShapeController>();
    ShapeController _hero;
    Coroutine _demo;
    GameMode _launch = GameMode.Learn;

    public void Bootstrap(GameMode startMode)
    {
        _launch = startMode;
        DOTween.Init();
        EnsureSystems();
        BuildDesk();
        BuildRigs();
        UI = gameObject.AddComponent<NetFoldUI>();
        UI.Build(this, startMode);
        Input.AllowWorldManipulate = startMode != GameMode.Learn;
        if (startMode == GameMode.Challenge)
        {
            BuildWorldToolbar();
        }
        Modes.ModeChanged += OnModeChanged;
        Modes.StepChanged += OnStepChanged;
        Input.SelectionChanged += OnSelected;
        Input.BackPressed += SceneLoader.LoadMainMenu;
        Section.EdgeCountChanged += edges =>
        {
            UI.DataPanel.SetSectionEdges(edges);
            UI.DataPanel.SetView("截面");
        };
        Challenge.QuestionChanged += (n, title) => UI.SetChallengeGuide(title, "点击候选项作答。提示会计入星级。");
        Challenge.Answered += (ok, msg) =>
        {
            if (UI.ChallengeLabel != null)
            {
                UI.ChallengeLabel.text = msg;
            }
        };
        Challenge.Completed += (stars, reason) => UI.Result.Show(stars, reason);
        if (startMode == GameMode.Challenge)
        {
            Modes.SetMode(GameMode.Challenge);
        }
        else if (startMode == GameMode.Free)
        {
            BeginFree();
        }
        else
        {
            OnStepChanged(LearnStep.Recognize);
        }

        UI.DataPanel.SetProjection(true, true, true);
    }

    void EnsureSystems()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            if (InputAdapter.DetectXR())
            {
                es.AddComponent<XRUIInputModule>();
            }
            else
            {
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        if (FindObjectOfType<FeedbackService>() == null)
        {
            new GameObject("Feedback").AddComponent<FeedbackService>();
        }

        Modes = gameObject.AddComponent<GameModeController>();
        Stars = gameObject.AddComponent<StarRatingController>();
        Unfolding = gameObject.AddComponent<UnfoldController>();
        Projection = gameObject.AddComponent<ProjectionController>();
        Section = gameObject.AddComponent<SectionController>();
        Challenge = gameObject.AddComponent<ChallengeController>();
        Input = gameObject.AddComponent<InputAdapter>();
    }

    void BuildDesk()
    {
        DeskAnchor = new GameObject("DeskAnchor").transform;
        DeskAnchor.SetParent(transform, false);
        DeskAnchor.position = new Vector3(0f, 0.78f, 0.35f);

        var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = "VirtualDesk";
        desk.transform.SetParent(DeskAnchor, false);
        desk.transform.localScale = new Vector3(1.8f, 0.04f, 1.15f);
        desk.transform.localPosition = Vector3.zero;
        desk.GetComponent<MeshRenderer>().sharedMaterial = UrpMaterialUtil.CreateLit(NetFoldTheme.Desk, true, 0.05f, 0.85f, true, NetFoldTheme.Accent * 0.25f);

        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        glow.name = "DeskGlow";
        glow.transform.SetParent(DeskAnchor, false);
        glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        glow.transform.localPosition = new Vector3(0f, 0.022f, 0f);
        glow.transform.localScale = new Vector3(1.7f, 1.05f, 1f);
        UnityEngine.Object.Destroy(glow.GetComponent<Collider>());
        glow.GetComponent<MeshRenderer>().sharedMaterial = UrpMaterialUtil.CreateLit(new Color(0.3f, 0.65f, 1f, 0.12f), true, 0f, 0.2f, true, NetFoldTheme.Accent * 0.4f);

        Stage = new GameObject("Stage").transform;
        Stage.SetParent(DeskAnchor, false);
        Stage.localPosition = new Vector3(0f, 0.03f, 0f);
        Challenge.Bind(Stars, Stage);
    }

    void BuildRigs()
    {
        var pcRig = new GameObject("PC Rig");
        pcRig.transform.SetParent(transform, false);
        var camGo = new GameObject("PC Camera");
        camGo.transform.SetParent(pcRig.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.04f, 0.08f, 0.14f);
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.tag = "MainCamera";
        if (FindObjectOfType<AudioListener>() == null)
        {
            camGo.AddComponent<AudioListener>();
        }

        var orbit = camGo.AddComponent<PCOrbitCamera>();
        var look = new GameObject("LookTarget");
        look.transform.SetParent(DeskAnchor, false);
        orbit.Bind(Input, look.transform);

        var light = new GameObject("KeyLight").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(0.85f, 0.93f, 1f);
        light.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
        var fill = new GameObject("FillLight").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.35f;
        fill.color = new Color(0.45f, 0.65f, 1f);
        fill.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

        var xr = XRRigBuilder.Build(transform, new Vector3(0f, 0f, -0.55f));
        Input.PcCamera = cam;
        Input.PcRig = pcRig;
        Input.XrOrigin = xr.gameObject;
        Input.XrHead = xr.Camera != null ? xr.Camera.transform : null;
        var rays = xr.GetComponentsInChildren<XRRayInteractor>(true);
        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i].name.Contains("Right"))
            {
                Input.RightRay = rays[i];
            }
            else
            {
                Input.LeftRay = rays[i];
            }
        }

        Input.ApplyPlatform();
    }

    void BuildWorldToolbar()
    {
        var canvas = UiFactory.CreateWorld("XRWorldUI", DeskAnchor, DeskAnchor.position + new Vector3(0f, 0.42f, -0.58f), new Vector2(900, 220), new Vector3(18f, 180f, 0f));
        canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        UiFactory.Button(canvas.transform, "WUnfold", "展开", new Vector2(0f, 0.15f), new Vector2(0.2f, 0.85f), new Vector2(8, 8), new Vector2(-8, -8), Unfold);
        UiFactory.Button(canvas.transform, "WFold", "折叠", new Vector2(0.2f, 0.15f), new Vector2(0.4f, 0.85f), new Vector2(8, 8), new Vector2(-8, -8), Fold);
        UiFactory.Button(canvas.transform, "WViews", "三视图", new Vector2(0.4f, 0.15f), new Vector2(0.6f, 0.85f), new Vector2(8, 8), new Vector2(-8, -8), ToggleProjection);
        UiFactory.Button(canvas.transform, "WCut", "截面", new Vector2(0.6f, 0.15f), new Vector2(0.8f, 0.85f), new Vector2(8, 8), new Vector2(-8, -8), ToggleSection);
        UiFactory.Button(canvas.transform, "WHint", "提示", new Vector2(0.8f, 0.15f), new Vector2(1f, 0.85f), new Vector2(8, 8), new Vector2(-8, -8), Hint);
    }

    void OnModeChanged(GameMode mode)
    {
        StopDemo();
        UI.Result.Hide();
        if (mode == GameMode.Learn)
        {
            Challenge.Stop();
            OnStepChanged(Modes.Step);
        }
        else
        {
            ClearLearnShapes();
            Unfolding.Bind(null);
            Projection.SetActive(false);
            Section.SetActive(false);
            Challenge.Begin();
            UI.DataPanel.SetView("挑战");
        }
    }

    void OnStepChanged(LearnStep step)
    {
        StopDemo();
        Challenge.Stop();
        UI.SetLearnGuide(step);
        UI.DataPanel.SetView("讲解");
        switch (step)
        {
            case LearnStep.Recognize:
                SpawnRecognize();
                Projection.SetActive(false);
                Section.SetActive(false);
                break;
            case LearnStep.Unfold:
                SpawnSingle(ShapeType.Cube);
                Projection.SetActive(false);
                Section.SetActive(false);
                break;
            case LearnStep.Projection:
                SpawnSingle(ShapeType.Cube);
                Section.SetActive(false);
                Projection.SetActive(true);
                UI.DataPanel.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
                break;
            case LearnStep.Section:
                SpawnSingle(ShapeType.Cube);
                Projection.SetActive(false);
                Section.SetActive(true);
                break;
        }

        if (UI.Toolbar != null)
        {
            UI.Toolbar.SetProjectionRays(step == LearnStep.Projection);
        }

        LockLearnShapes();
        if (_launch == GameMode.Learn)
        {
            _demo = StartCoroutine(PresentStep(step));
        }
    }

    void SpawnRecognize()
    {
        ClearLearnShapes();
        _learnShapes.Add(GeometryFactory.Create(ShapeType.Cube, Stage, new Vector3(-0.48f, 0f, 0f)));
        _learnShapes.Add(GeometryFactory.Create(ShapeType.Cylinder, Stage, new Vector3(-0.16f, 0f, 0f)));
        _learnShapes.Add(GeometryFactory.Create(ShapeType.Cone, Stage, new Vector3(0.16f, 0f, 0f)));
        _learnShapes.Add(GeometryFactory.Create(ShapeType.TriangularPrism, Stage, new Vector3(0.48f, 0f, 0f)));
        _hero = _learnShapes[0];
        BindCurrent(_hero);
        Input.Select(_hero);
        LockLearnShapes();
    }

    void SpawnSingle(ShapeType type)
    {
        ClearLearnShapes();
        _hero = GeometryFactory.Create(type, Stage, Vector3.zero);
        _learnShapes.Add(_hero);
        BindCurrent(_hero);
        Input.Select(_hero);
        LockLearnShapes();
    }

    void LockLearnShapes()
    {
        if (_launch != GameMode.Learn)
        {
            return;
        }

        for (int i = 0; i < _learnShapes.Count; i++)
        {
            if (_learnShapes[i] != null)
            {
                _learnShapes[i].CanDrag = false;
            }
        }
    }

    void BindCurrent(ShapeController shape)
    {
        Unfolding.Bind(shape);
        Projection.Bind(shape);
        Section.Bind(shape);
        UI.DataPanel.Show(shape);
    }

    void OnSelected(IInteractable interactable)
    {
        var shape = interactable as ShapeController;
        if (shape == null && interactable != null)
        {
            shape = interactable.GameObject.GetComponentInParent<ShapeController>();
        }

        if (shape != null)
        {
            _hero = shape;
            BindCurrent(shape);
            if (_launch == GameMode.Free || (Modes.Mode == GameMode.Learn && Modes.Step == LearnStep.Unfold))
            {
                Unfolding.TrySelectFace(Input.Provider.PointerRay);
            }
        }
    }

    public void NextStep()
    {
        if (_launch != GameMode.Learn || Modes.Mode != GameMode.Learn)
        {
            return;
        }

        if (Modes.Step >= LearnStep.Section)
        {
            if (UI.GuideBody != null)
            {
                UI.GuideBody.text = "四步讲解已完成。自由实验和挑战从主菜单进入。";
            }

            return;
        }

        Modes.NextStep();
    }

    public void PrevStep()
    {
        if (_launch != GameMode.Learn || Modes.Mode != GameMode.Learn)
        {
            return;
        }

        Modes.PrevStep();
    }

    public void SetTool(InteractTool tool)
    {
        Input.ActiveTool = tool;
        UI.DataPanel.SetView(tool == InteractTool.Rotate ? "旋转" : tool == InteractTool.Scale ? "缩放" : "选择");
    }

    public void ResetSelected()
    {
        if (_launch == GameMode.Free)
        {
            BeginFree();
            return;
        }

        _hero?.ResetPose();
        if (Unfolding.IsUnfolded)
        {
            Unfolding.Fold();
        }
    }

    public void Unfold()
    {
        if (_launch == GameMode.Free)
        {
            if (_hero == null)
            {
                return;
            }

            Section.SetActive(false);
            Unfolding.Bind(_hero);
            Unfolding.Unfold();
            UI.DataPanel.SetView("展开图");
            return;
        }

        if (Modes.Mode != GameMode.Learn || _hero == null)
        {
            return;
        }

        Modes.SetStep(LearnStep.Unfold);
        Unfolding.Bind(_hero);
        Unfolding.Unfold();
        UI.DataPanel.SetView("展开图");
    }

    public void Fold()
    {
        if (_launch == GameMode.Free)
        {
            Unfolding.Fold();
            UI.DataPanel.SetView("立体");
            return;
        }

        if (Modes.Mode != GameMode.Learn)
        {
            return;
        }

        Unfolding.Fold();
        UI.DataPanel.SetView("立体");
    }

    public void ToggleProjection()
    {
        if (_launch == GameMode.Free)
        {
            if (_hero == null)
            {
                return;
            }

            Section.SetActive(false);
            Projection.Bind(_hero);
            Projection.Toggle();
            if (UI.Toolbar != null)
            {
                UI.Toolbar.SetProjectionRays(Projection.IsActive);
            }

            UI.DataPanel.SetView(Projection.IsActive ? "三视图" : "自由实验");
            UI.DataPanel.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
            return;
        }

        if (Modes.Mode != GameMode.Learn)
        {
            return;
        }

        if (Modes.Step != LearnStep.Projection)
        {
            Modes.SetStep(LearnStep.Projection);
            return;
        }

        Projection.Bind(_hero);
        Projection.Toggle();
        UI.DataPanel.SetView(Projection.IsActive ? "三视图" : "学习");
        UI.DataPanel.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
    }

    public void ToggleSection()
    {
        if (_launch == GameMode.Free)
        {
            if (_hero == null)
            {
                return;
            }

            if (Projection.IsActive)
            {
                Projection.SetActive(false);
                if (UI.Toolbar != null)
                {
                    UI.Toolbar.SetProjectionRays(false);
                }
            }

            Section.Bind(_hero);
            Section.Toggle();
            UI.DataPanel.SetView(Section.IsActive ? "截面" : "自由实验");
            return;
        }

        if (Modes.Mode != GameMode.Learn)
        {
            return;
        }

        if (Modes.Step != LearnStep.Section)
        {
            Modes.SetStep(LearnStep.Section);
            return;
        }

        Section.Bind(_hero);
        Section.Toggle();
        UI.DataPanel.SetView(Section.IsActive ? "截面" : "学习");
    }

    public void ToggleProjectionRay(int index)
    {
        if (!Projection.IsActive)
        {
            ToggleProjection();
        }

        bool next = index == 0 ? !Projection.FrontRays : index == 1 ? !Projection.TopRays : !Projection.SideRays;
        Projection.SetRay(index, next);
        UI.DataPanel.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
    }

    public void Hint()
    {
        if (Modes.Mode == GameMode.Challenge)
        {
            Challenge.UseHint();
        }
    }

    public void RestartChallenge()
    {
        Modes.SetMode(GameMode.Challenge);
        Challenge.Begin();
    }

    public void PlayAutoDemo()
    {
        if (_launch != GameMode.Learn)
        {
            return;
        }

        StopDemo();
        _demo = StartCoroutine(AutoDemo());
    }

    IEnumerator AutoDemo()
    {
        Modes.SetMode(GameMode.Learn);
        Modes.SetStep(LearnStep.Recognize);
        yield return new WaitForSeconds(1.2f);
        Modes.SetStep(LearnStep.Unfold);
        yield return new WaitForSeconds(0.3f);
        Unfolding.Unfold();
        yield return new WaitForSeconds(1.4f);
        Unfolding.Fold();
        yield return new WaitForSeconds(1.1f);
        Modes.SetStep(LearnStep.Projection);
        yield return new WaitForSeconds(1.6f);
        Modes.SetStep(LearnStep.Section);
        float t = 0f;
        while (t < 2.2f)
        {
            t += Time.deltaTime;
            if (Section != null)
            {
                Section.Rotate(new Vector2(18f, 8f) * Time.deltaTime);
            }

            yield return null;
        }
    }

    void StopDemo()
    {
        if (_demo != null)
        {
            StopCoroutine(_demo);
            _demo = null;
        }
    }

    IEnumerator PresentStep(LearnStep step)
    {
        if (UI == null || UI.GuideBody == null)
        {
            yield break;
        }

        if (step == LearnStep.Recognize)
        {
            string[] lines =
            {
                "正方体有 6 个全等的正方形面、12 条棱、8 个顶点。相对的面互相平行，相邻的棱互相垂直。",
                "圆柱有 2 个互相平行且全等的圆形底面，侧面是一个曲面。把侧面展开，得到一个长方形。",
                "圆锥有 1 个圆形底面和 1 个曲面侧面。顶点到底面圆心的距离是高。侧面展开后是一个扇形。",
                "三棱柱有 2 个平行的三角形底面和 3 个长方形侧面，一共 5 个面、9 条棱、6 个顶点。"
            };
            for (int i = 0; i < _learnShapes.Count && i < lines.Length; i++)
            {
                Input.Select(_learnShapes[i]);
                UI.GuideBody.text = lines[i];
                yield return new WaitForSeconds(2.6f);
            }

            UI.GuideBody.text = "面、棱、顶点是描述立体图形的三个基本量。记住每种图形有几个面、几条棱、几个顶点，后面的展开和三视图都会用到。";
            yield break;
        }

        if (step == LearnStep.Unfold)
        {
            UI.GuideBody.text = "沿着某些棱把立体剪开、铺平，得到的平面图形叫展开图。动画里正方体正在展开。展开图必须能折回去，重新围成原来的立体。";
            yield return new WaitForSeconds(0.6f);
            Unfolding.Unfold();
            yield return new WaitForSeconds(2.4f);
            UI.GuideBody.text = "正方体一共有 11 种展开图。判断时看这些正方形能不能折成封闭的盒子，面不能重叠，也不能缺一个面。现在它再折回去。";
            Unfolding.Fold();
            yield return new WaitForSeconds(2f);
            yield break;
        }

        if (step == LearnStep.Projection)
        {
            UI.GuideBody.text = "三视图是从三个方向看同一个立体：正面是主视图，上面是俯视图，左面是左视图。蓝色是主视方向，绿色是俯视方向，紫色是左视方向。";
            Projection.SetRay(0, true);
            Projection.SetRay(1, false);
            Projection.SetRay(2, false);
            UI.DataPanel.SetProjection(true, false, false);
            yield return new WaitForSeconds(2.2f);
            UI.GuideBody.text = "主视图和俯视图一样长，叫长对正。主视图和左视图一样高，叫高平齐。俯视图和左视图一样宽，叫宽相等。";
            Projection.SetRay(0, false);
            Projection.SetRay(1, true);
            UI.DataPanel.SetProjection(false, true, false);
            yield return new WaitForSeconds(2.2f);
            Projection.SetRay(1, false);
            Projection.SetRay(2, true);
            UI.DataPanel.SetProjection(false, false, true);
            yield return new WaitForSeconds(2f);
            Projection.SetRay(0, true);
            Projection.SetRay(1, true);
            Projection.SetRay(2, true);
            UI.DataPanel.SetProjection(true, true, true);
            UI.GuideBody.text = "三条投影线一起看：长对正，高平齐，宽相等。画三视图时，三个图的位置和尺寸都要符合这三句话。";
            yield break;
        }

        UI.GuideBody.text = "用一个平面去截立体图形，平面和表面相交围成的图形叫截面。正方体的截面不一定是正方形。";
        float t = 0f;
        while (t < 2.2f)
        {
            t += Time.deltaTime;
            Section.Rotate(new Vector2(22f, 10f) * Time.deltaTime);
            yield return null;
        }

        UI.GuideBody.text = "平面切得越斜，截面的边数可以变多。正方体的截面可以是三角形、四边形、五边形，最多是六边形。右侧的截面边数会跟着切割角度变化。";
        t = 0f;
        while (t < 2.4f)
        {
            t += Time.deltaTime;
            Section.Rotate(new Vector2(-16f, 24f) * Time.deltaTime);
            yield return null;
        }
    }

    void BeginFree()
    {
        StopDemo();
        SpawnRecognize();
        Projection.SetActive(false);
        Section.SetActive(false);
        if (UI.Toolbar != null)
        {
            UI.Toolbar.SetProjectionRays(false);
        }

        if (UI.GuideTitle != null)
        {
            UI.GuideTitle.text = "自由实验";
        }

        if (UI.GuideBody != null)
        {
            UI.GuideBody.text = "点选几何体，可以展开、折叠、看三视图，或拖动截面。";
        }

        if (UI.ModeLabel != null)
        {
            UI.ModeLabel.text = "自由实验";
        }

        UI.DataPanel.SetView("自由实验");
    }

    void ClearLearnShapes()
    {
        if (Input != null)
        {
            Input.ClearSelection();
        }

        for (int i = 0; i < _learnShapes.Count; i++)
        {
            if (_learnShapes[i] != null)
            {
                DOTween.Kill(_learnShapes[i].transform);
                Destroy(_learnShapes[i].gameObject);
            }
        }

        _learnShapes.Clear();
        _hero = null;
    }
}
