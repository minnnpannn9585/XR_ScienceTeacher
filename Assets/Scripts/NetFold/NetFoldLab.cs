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
    bool _suppressPick;

    public void Bootstrap(GameMode startMode)
    {
        _launch = startMode;
        DOTween.Init();
        EnsureSystems();
        BuildDesk();
        Projection.SetAnchor(Stage);
        Section.SetAnchor(Stage);
        Section.AllowDrag = startMode != GameMode.Learn;
        BuildRigs();
        UI = gameObject.AddComponent<NetFoldUI>();
        UI.Build(this, startMode);
        Input.AllowWorldManipulate = startMode != GameMode.Learn;
        Modes.ModeChanged += OnModeChanged;
        Modes.StepChanged += OnStepChanged;
        Input.SelectionChanged += OnSelected;
        Input.BackPressed += SceneLoader.LoadMainMenu;
        Section.EdgeCountChanged += edges =>
        {
            UI.DataPanel?.SetSectionEdges(edges);
            UI.DataPanel?.SetView("截面");
        };
        Challenge.Presented += (title, body, choices) =>
        {
            UI.SetChallengeGuide(title, body);
            UI.ShowChoices(choices, Challenge.SubmitChoice);
        };
        Challenge.Answered += (ok, msg) =>
        {
            if (!ok)
            {
                UI.MarkChoice(Challenge.LastPick);
            }

            if (UI.GuideBody != null)
            {
                UI.GuideBody.text = msg;
            }
        };
        Challenge.Completed += (stars, reason) =>
        {
            UI.ClearChoices();
            UI.SetChallengeGuide("挑战完成", reason);
            UI.Result?.Show(stars, reason);
        };
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

        UI.DataPanel?.SetProjection(true, true, true);
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

        var desk = LabFactory.Primitive(PrimitiveType.Cube, "VirtualDesk", DeskAnchor, Vector3.zero, new Vector3(1.8f, 0.045f, 1.15f), StudioSet.Stone(), true);
        StudioSet.DressDesk(desk.transform);

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
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NetFoldTheme.Void;
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
        orbit.Yaw = 34f;
        orbit.Pitch = 32f;
        orbit.Distance = 2.55f;

        StudioSet.Install(transform, cam, DeskAnchor.position);

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

    void OnModeChanged(GameMode mode)
    {
        StopDemo();
        UI.Result?.Hide();
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
            UI.DataPanel?.SetView("挑战");
        }
    }

    void OnStepChanged(LearnStep step)
    {
        StopDemo();
        Challenge.Stop();
        UI.SetLearnGuide(step);
        UI.DataPanel?.SetView("讲解");
        switch (step)
        {
            case LearnStep.Recognize:
                SpawnSingle(ShapeType.Cube);
                Projection.SetActive(false);
                Section.SetActive(false);
                break;
            case LearnStep.Unfold:
                SpawnSingle(ShapeType.Cube);
                Projection.SetActive(false);
                Section.SetActive(false);
                Unfolding.Unfold();
                break;
            case LearnStep.Projection:
                SpawnSingle(ShapeType.Cube);
                Section.SetActive(false);
                Projection.SetRay(0, true);
                Projection.SetRay(1, true);
                Projection.SetRay(2, true);
                Projection.SetActive(true);
                UI.DataPanel?.SetProjection(true, true, true);
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

    void SpawnSingle(ShapeType type)
    {
        ClearLearnShapes();
        _hero = GeometryFactory.Create(type, Stage, Vector3.zero);
        _learnShapes.Add(_hero);
        bool learn = _launch == GameMode.Learn;
        _hero.CanDrag = !learn;
        _hero.AllowIdleSpin = !learn;
        BindCurrent(_hero);
        if (learn)
        {
            Input.ClearSelection();
        }
        else
        {
            _suppressPick = true;
            Input.Select(_hero);
            _suppressPick = false;
        }

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
        UI.DataPanel?.Show(shape);
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
            if (_launch == GameMode.Free && !_suppressPick)
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
        UI.DataPanel?.SetView(tool == InteractTool.Rotate ? "旋转" : tool == InteractTool.Scale ? "缩放" : "选择");
    }

    public void ResetSelected()
    {
        if (_launch == GameMode.Free)
        {
            ShowFreeShape(_hero != null ? _hero.Type : ShapeType.Cube);
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
            UI.DataPanel?.SetView("展开图");
            return;
        }

        if (Modes.Mode != GameMode.Learn || _hero == null)
        {
            return;
        }

        Modes.SetStep(LearnStep.Unfold);
        Unfolding.Bind(_hero);
        Unfolding.Unfold();
        UI.DataPanel?.SetView("展开图");
    }

    public void Fold()
    {
        if (_launch == GameMode.Free)
        {
            Unfolding.Fold();
            UI.DataPanel?.SetView("立体");
            return;
        }

        if (Modes.Mode != GameMode.Learn)
        {
            return;
        }

        Unfolding.Fold();
        UI.DataPanel?.SetView("立体");
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

            UI.DataPanel?.SetView(Projection.IsActive ? "三视图" : "自由实验");
            UI.DataPanel?.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
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
        UI.DataPanel?.SetView(Projection.IsActive ? "三视图" : "学习");
        UI.DataPanel?.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
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
            UI.DataPanel?.SetView(Section.IsActive ? "截面" : "自由实验");
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
        UI.DataPanel?.SetView(Section.IsActive ? "截面" : "学习");
    }

    public void ToggleProjectionRay(int index)
    {
        if (!Projection.IsActive)
        {
            ToggleProjection();
        }

        bool next = index == 0 ? !Projection.FrontRays : index == 1 ? !Projection.TopRays : !Projection.SideRays;
        Projection.SetRay(index, next);
        UI.DataPanel?.SetProjection(Projection.FrontRays, Projection.TopRays, Projection.SideRays);
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
        if (step == LearnStep.Recognize)
        {
            float yaw = 0f;
            while (_hero != null)
            {
                yaw += 18f * Time.deltaTime;
                _hero.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }

            yield break;
        }

        if (step != LearnStep.Section || Section == null)
        {
            yield break;
        }

        while (Section != null)
        {
            Section.Rotate(new Vector2(14f, 8f) * Time.deltaTime);
            yield return null;
        }
    }

    void BeginFree()
    {
        StopDemo();
        if (UI.GuideTitle != null)
        {
            UI.GuideTitle.text = "自由实验";
        }

        if (UI.ModeLabel != null)
        {
            UI.ModeLabel.text = "自由实验";
        }

        ShowFreeShape(ShapeType.Cube);
    }

    public void ShowFreeShape(ShapeType type)
    {
        if (_launch != GameMode.Free)
        {
            return;
        }

        if (_hero != null && _hero.Type == type)
        {
            UI.HighlightShape(type);
            return;
        }

        StopDemo();
        Projection.SetActive(false);
        Section.SetActive(false);
        if (UI.Toolbar != null)
        {
            UI.Toolbar.SetProjectionRays(false);
        }

        SpawnSingle(type);
        UI.HighlightShape(type);
        if (UI.GuideBody != null)
        {
            UI.GuideBody.text = "当前是" + ShapeCatalog.DisplayName(type) + "。可以旋转、缩放、展开、折叠、看三视图，或拖动截面。换一个几何体时，上一个会收起来。";
        }

        UI.DataPanel?.SetView("自由实验");
    }

    void ClearLearnShapes()
    {
        Unfolding.Bind(null);
        Projection.Bind(null);
        Section.Bind(null);
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
