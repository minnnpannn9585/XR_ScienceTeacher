using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public enum LensStep
{
    Bench = 0,
    Rays = 1,
    Distance = 2,
    Focal = 3
}

public class OpticalBenchController : MonoBehaviour
{
    public const float AxisY = 0.13f;
    public const float ObjectHeight = 0.07f;

    public float Focal = 0.15f;
    public float ObjectDistance = 0.4f;
    public float ScreenDistance = 0.28f;
    public bool LightOn = true;
    public bool ShowRays;
    public bool ShowGuides = true;
    public bool DepthOn;
    public bool AutoScreen;
    public bool LockCandle;
    public bool LockScreen;
    public bool LockFocal;
    public bool RayParallel = true;
    public bool RayCenter = true;
    public bool RayFocus = true;
    public LensStep Step = LensStep.Bench;
    public ImageResult Imaging;
    public float Sharpness;
    public string Status;
    public CandleController Candle;
    public ScreenController ScreenPlate;
    public LensCraftUI UI;
    public StarRatingController Stars;
    public LensQuizController Quiz;

    LensController _lens;
    LightRayController _rays;
    ImageFormationController _images;
    DepthOfFieldController _dof;
    Camera _cam;
    InputAdapter _input;
    GameMode _launch = GameMode.Learn;
    Coroutine _demo;
    int _epoch = 1;
    int _beat;

    public void Bootstrap(GameMode startMode)
    {
        Status = Loc.Get("lens.converge");
        LabFactory.UseWorldLabels = startMode != GameMode.Challenge;
        _launch = startMode;
        DOTween.Init();
        EnsureSystems();
        BuildDesk();
        BuildRigs();
        Candle = new GameObject("Candle").AddComponent<CandleController>();
        Candle.transform.SetParent(transform, false);
        Candle.Bench = this;
        Candle.Build();
        ScreenPlate = new GameObject("Screen").AddComponent<ScreenController>();
        ScreenPlate.transform.SetParent(transform, false);
        ScreenPlate.Bench = this;
        ScreenPlate.Build();
        _lens = gameObject.AddComponent<LensController>();
        _lens.Build(this);
        _rays = gameObject.AddComponent<LightRayController>();
        _rays.Build(this);
        _images = gameObject.AddComponent<ImageFormationController>();
        _images.Build(this);
        if (startMode == GameMode.Challenge)
        {
            _dof = gameObject.AddComponent<DepthOfFieldController>();
            _dof.Build();
        }
        else
        {
            ShowGuides = false;
        }

        UI = gameObject.AddComponent<LensCraftUI>();
        UI.Build(this, startMode);
        _input.BackPressed += SceneLoader.LoadMainMenu;
        var hub = gameObject.AddComponent<InputManager>();
        hub.Bind(_input);
        _input.AllowWorldManipulate = startMode != GameMode.Learn;
        if (startMode != GameMode.Learn)
        {
            hub.ResetRequested += ResetCurrent;
            hub.RayToggled += ToggleRays;
        }

        if (startMode == GameMode.Challenge)
        {
            hub.ViewToggled += ToggleDepth;
        }
        hub.ConfirmRequested += SubmitAnswer;
        ApplyPlacement();
        if (startMode == GameMode.Challenge)
        {
            Quiz.Begin(this, Stars);
        }
        else if (startMode == GameMode.Free)
        {
            ApplyFree();
        }
        else
        {
            ApplyBeat(0);
        }
    }

    public bool RayOnBench(Ray ray, out Vector3 local)
    {
        var plane = new Plane(Vector3.up, transform.position + Vector3.up * AxisY);
        if (!plane.Raycast(ray, out float enter))
        {
            local = Vector3.zero;
            return false;
        }

        local = transform.InverseTransformPoint(ray.GetPoint(enter));
        return true;
    }

    public void ApplyPlacement()
    {
        Candle.Place(ObjectDistance);
        ScreenPlate.Place(ScreenDistance);
        _lens.Apply(Focal, ShowGuides);
        if (UI != null)
        {
            UI.SetFocal(Focal * 100f, !LockFocal);
        }
    }

    public void SetFocalFromSlider(float centimeters)
    {
        if (LockFocal)
        {
            return;
        }

        Focal = Mathf.Clamp(centimeters * 0.01f, 0.1f, 0.28f);
        Status = Loc.Get("lens.focal.stronger");
    }

    public void ToggleLight()
    {
        LightOn = !LightOn;
        Status = LightOn ? Loc.Get("lens.light.on") : Loc.Get("lens.light.off");
    }

    public void ToggleRays()
    {
        ShowRays = !ShowRays;
        Status = ShowRays ? Loc.Get("lens.rays.on") : Loc.Get("lens.rays.off");
    }

    public void ToggleGuides()
    {
        ShowGuides = !ShowGuides;
        Status = ShowGuides ? Loc.Get("lens.guides.on") : Loc.Get("lens.guides.off");
    }

    public void ToggleDepth()
    {
        DepthOn = !DepthOn;
        Status = DepthOn ? Loc.Get("lens.dof.on") : Loc.Get("lens.dof.off");
    }

    public void ToggleRay(int index)
    {
        if (index == 0) RayParallel = !RayParallel;
        else if (index == 1) RayCenter = !RayCenter;
        else RayFocus = !RayFocus;
        ShowRays = true;
        Status = Loc.Get("lens.rays.each");
    }

    public void Toolbar(string action)
    {
        switch (action)
        {
            case "重置": ResetCurrent(); break;
            case "自动演示": PlayAutoDemo(); break;
            case "显示光路": ToggleRays(); break;
            case "辅助线":
                if (_launch == GameMode.Challenge) ToggleGuides();
                break;
            case "景深":
                if (_launch == GameMode.Challenge) ToggleDepth();
                break;
            case "提示":
                if (_launch == GameMode.Challenge) Hint();
                break;
        }
    }

    public void NextStep()
    {
        if (_launch != GameMode.Learn)
        {
            if (_launch == GameMode.Challenge)
            {
                Status = Loc.Get("common.finishQuestion");
            }

            return;
        }

        if (_beat >= 7)
        {
            Status = Loc.Get("common.lesson.menu");
            if (UI != null)
            {
                UI.SetLesson(Loc.Get("drift.lesson.done.title"), Loc.Get("lens.lesson.done.body"));
            }

            return;
        }

        ApplyBeat(_beat + 1);
    }

    public void PrevStep()
    {
        if (_launch != GameMode.Learn)
        {
            return;
        }

        ApplyBeat(_beat > 0 ? _beat - 1 : 0);
    }

    public void ResetCurrent()
    {
        StopDemo();
        _epoch++;
        if (_launch == GameMode.Challenge)
        {
            if (UI != null && UI.Result != null && UI.Result.IsOpen)
            {
                RestartChallenge();
            }
            else
            {
                Quiz.Begin(this, Stars);
            }

            return;
        }

        if (_launch == GameMode.Free)
        {
            ApplyFree();
            return;
        }

        ApplyBeat(_beat);
    }

    public void Hint()
    {
        if (_launch == GameMode.Challenge && Quiz.Running)
        {
            Quiz.Hint();
        }
    }

    public void SubmitAnswer()
    {
        if (_launch == GameMode.Challenge)
        {
            Quiz.Submit();
        }
    }

    public void RestartChallenge()
    {
        if (UI != null && UI.Result != null)
        {
            UI.Result.Hide();
        }

        Quiz.Begin(this, Stars);
    }

    public void PlayAutoDemo()
    {
        if (_launch != GameMode.Learn)
        {
            Status = Loc.Get("common.demo.learnOnly");
            return;
        }

        StopDemo();
        _demo = StartCoroutine(AutoDemo());
    }

    void Update()
    {
        if (Candle == null)
        {
            return;
        }

        if (!LockCandle)
        {
            ObjectDistance = Mathf.Clamp(-Candle.transform.localPosition.x, 0.08f, 0.72f);
        }
        else
        {
            Candle.Place(ObjectDistance);
        }

        if (!AutoScreen && !LockScreen)
        {
            ScreenDistance = Mathf.Clamp(ScreenPlate.transform.localPosition.x, 0.08f, 0.78f);
        }

        Imaging = LensMath.Evaluate(ObjectDistance, Focal);
        if (AutoScreen && Imaging.HasImage && Imaging.IsReal && !float.IsInfinity(Imaging.ImageDistance))
        {
            float target = Mathf.Clamp(Imaging.ImageDistance, 0.08f, 0.78f);
            ScreenDistance = Mathf.MoveTowards(ScreenDistance, target, Time.deltaTime * 0.5f);
            ScreenPlate.Place(ScreenDistance);
        }
        else if (LockScreen)
        {
            ScreenPlate.Place(ScreenDistance);
        }

        Sharpness = LensMath.Sharpness(Imaging, ScreenDistance);
        Candle.SetLit(LightOn);
        _lens.Apply(Focal, ShowGuides);
        _rays.Redraw();
        _images.Apply();
        if (_dof != null)
        {
            _dof.Apply(this, _cam);
        }
        if (Quiz != null)
        {
            Quiz.Tick();
        }

        if (UI != null)
        {
            string mag = Imaging.HasImage ? Imaging.AbsMagnification.ToString("0.00") : "—";
            UI.Refresh(
                Loc.Format("lens.u", LensMath.Centimeters(ObjectDistance)),
                Loc.Format("lens.v", LensMath.Centimeters(Imaging.ImageDistance)),
                Loc.Format("lens.f", LensMath.Centimeters(Focal)),
                Loc.Format("lens.mag", mag),
                Loc.Format("lens.nature.row", Imaging.Nature),
                Loc.Format("lens.rule.row", Imaging.Rule),
                Status);
        }
    }

    void ApplyBeat(int index)
    {
        StopDemo();
        _epoch++;
        _beat = Mathf.Clamp(index, 0, 7);
        LockCandle = true;
        LockScreen = true;
        LockFocal = true;
        AutoScreen = false;
        LightOn = true;
        ShowGuides = false;
        DepthOn = false;
        ShowRays = false;
        RayParallel = RayCenter = RayFocus = true;
        if (UI != null)
        {
            UI.SetLesson(BeatTitle(_beat), BeatBody(_beat));
            UI.ClearChoices();
            UI.SetAnswerVisible(false);
        }

        _demo = StartCoroutine(PlayBeat(_beat, _epoch));
    }

    IEnumerator PlayBeat(int index, int epoch)
    {
        Focal = 0.15f;
        LightOn = true;
        switch (index)
        {
            case 0:
                ObjectDistance = 0.42f;
                ScreenDistance = 0.55f;
                AutoScreen = true;
                Status = Loc.Get("lens.focusing");
                ApplyPlacement();
                yield return new WaitForSeconds(2.4f);
                break;
            case 1:
                ObjectDistance = 0.4f;
                ScreenDistance = 0.24f;
                ShowRays = true;
                RayParallel = true;
                RayCenter = false;
                RayFocus = false;
                Status = Loc.Get("lens.ray.red");
                ApplyPlacement();
                yield return new WaitForSeconds(1.7f);
                if (epoch != _epoch) yield break;
                RayCenter = true;
                Status = Loc.Get("lens.ray.green");
                yield return new WaitForSeconds(1.7f);
                if (epoch != _epoch) yield break;
                RayFocus = true;
                Status = Loc.Get("lens.ray.blue");
                yield return new WaitForSeconds(1.6f);
                break;
            case 2:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.5f;
                Status = Loc.Get("lens.case.beyond");
                ApplyPlacement();
                yield return FocusHold(epoch, 0.46f);
                break;
            case 3:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.3f;
                Status = Loc.Get("lens.case.twice");
                ApplyPlacement();
                yield return new WaitForSeconds(2.2f);
                break;
            case 4:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.22f;
                Status = Loc.Get("lens.case.between");
                ApplyPlacement();
                yield return new WaitForSeconds(2.2f);
                break;
            case 5:
                ShowRays = true;
                ObjectDistance = 0.15f;
                ScreenDistance = 0.45f;
                Status = Loc.Get("lens.case.focus");
                ApplyPlacement();
                yield return new WaitForSeconds(2.4f);
                break;
            case 6:
                ShowRays = true;
                ObjectDistance = 0.09f;
                ScreenDistance = 0.4f;
                Status = Loc.Get("lens.case.inside");
                ApplyPlacement();
                yield return new WaitForSeconds(2.4f);
                break;
            default:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.36f;
                Focal = 0.24f;
                Status = Loc.Get("lens.focal.shrink");
                ApplyPlacement();
                float f = 0.24f;
                while (f > 0.11f)
                {
                    if (epoch != _epoch) yield break;
                    f -= Time.deltaTime * 0.035f;
                    Focal = f;
                    yield return null;
                }

                Status = Loc.Get("lens.focal.stronger");
                break;
        }
    }

    IEnumerator FocusHold(int epoch, float targetU)
    {
        while (ObjectDistance > targetU)
        {
            if (epoch != _epoch) yield break;
            ObjectDistance = Mathf.MoveTowards(ObjectDistance, targetU, Time.deltaTime * 0.06f);
            yield return null;
        }

        yield return new WaitForSeconds(1.2f);
    }

    static string BeatTitle(int index)
    {
        switch (index)
        {
            case 0: return Loc.Get("lens.beat1.title");
            case 1: return Loc.Get("lens.beat2.title");
            case 2: return Loc.Get("lens.beat3.title");
            case 3: return Loc.Get("lens.beat4.title");
            case 4: return Loc.Get("lens.beat5.title");
            case 5: return Loc.Get("lens.beat6.title");
            case 6: return Loc.Get("lens.beat7.title");
            default: return Loc.Get("lens.beat8.title");
        }
    }

    static string BeatBody(int index)
    {
        switch (index)
        {
            case 0:
                return Loc.Get("lens.beat1.body");
            case 1:
                return Loc.Get("lens.beat2.body");
            case 2:
                return Loc.Get("lens.beat3.body");
            case 3:
                return Loc.Get("lens.beat4.body");
            case 4:
                return Loc.Get("lens.beat5.body");
            case 5:
                return Loc.Get("lens.beat6.body");
            case 6:
                return Loc.Get("lens.beat7.body");
            default:
                return Loc.Get("lens.beat8.body");
        }
    }

    void ApplyLearn(LensStep step)
    {
        Step = step;
        LockCandle = false;
        LockScreen = false;
        LockFocal = false;
        AutoScreen = false;
        LightOn = true;
        ShowGuides = false;
        DepthOn = false;
        switch (step)
        {
            case LensStep.Bench:
                ObjectDistance = 0.4f;
                Focal = 0.15f;
                ScreenDistance = 0.28f;
                ShowRays = false;
                Status = Loc.Get("lens.converge");
                break;
            case LensStep.Rays:
                ShowRays = true;
                RayParallel = RayCenter = RayFocus = true;
                Status = Loc.Get("lens.watch.rays");
                break;
            case LensStep.Distance:
                ObjectDistance = 0.5f;
                AutoScreen = true;
                LockScreen = true;
                ShowRays = true;
                Status = Loc.Get("lens.drag.candle");
                break;
            default:
                ObjectDistance = 0.36f;
                Focal = 0.18f;
                AutoScreen = true;
                LockScreen = true;
                ShowRays = true;
                Status = Loc.Get("lens.focal.stronger");
                break;
        }

        ApplyPlacement();
        if (UI != null)
        {
            UI.SetLearn(step);
            UI.ClearChoices();
            UI.SetAnswerVisible(false);
        }
    }

    void ApplyFree()
    {
        StopDemo();
        LockCandle = false;
        LockScreen = false;
        LockFocal = false;
        AutoScreen = false;
        LightOn = true;
        ShowGuides = false;
        DepthOn = false;
        ShowRays = false;
        RayParallel = RayCenter = RayFocus = true;
        ObjectDistance = 0.4f;
        Focal = 0.15f;
        ScreenDistance = 0.28f;
        Status = Loc.Get("lens.free.status");
        ApplyPlacement();
        if (UI != null)
        {
            UI.SetFree();
            UI.ClearChoices();
            UI.SetAnswerVisible(false);
        }
    }

    IEnumerator AutoDemo()
    {
        int epoch = _epoch;
        ApplyLearn(LensStep.Bench);
        yield return new WaitForSeconds(0.8f);
        if (epoch != _epoch) yield break;
        ApplyLearn(LensStep.Rays);
        LightOn = true;
        ShowRays = true;
        yield return new WaitForSeconds(1.2f);
        if (epoch != _epoch) yield break;
        ApplyLearn(LensStep.Distance);
        float u = 0.55f;
        while (u > 0.1f)
        {
            if (epoch != _epoch) yield break;
            u -= Time.deltaTime * 0.08f;
            ObjectDistance = u;
            Candle.Place(u);
            Status = u > Focal ? Loc.Get("lens.drag.changing") : Loc.Get("lens.drag.virtual");
            yield return null;
        }

        ApplyLearn(LensStep.Focal);
        float f = 0.24f;
        while (f > 0.11f)
        {
            if (epoch != _epoch) yield break;
            f -= Time.deltaTime * 0.04f;
            Focal = f;
            Status = Loc.Get("lens.focal.stronger");
            yield return null;
        }

        Status = Loc.Get("common.demo.done");
    }

    void StopDemo()
    {
        if (_demo != null)
        {
            StopCoroutine(_demo);
            _demo = null;
        }
    }

    void EnsureSystems()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            var system = es.AddComponent<EventSystem>();
            system.sendNavigationEvents = false;
            es.AddComponent<InputSystemUIInputModule>();
        }

        if (FindObjectOfType<FeedbackService>() == null)
        {
            new GameObject("Feedback").AddComponent<FeedbackService>();
        }

        Stars = gameObject.AddComponent<StarRatingController>();
        Quiz = gameObject.AddComponent<LensQuizController>();
        _input = gameObject.AddComponent<InputAdapter>();
    }

    void BuildDesk()
    {
        transform.position = new Vector3(0f, 0.4f, 0f);
        var desk = LabFactory.Primitive(PrimitiveType.Cube, "Desk", transform, new Vector3(0f, -0.02f, 0f), new Vector3(1.9f, 0.045f, 0.72f), StudioSet.Stone(), true);
        StudioSet.DressDesk(desk.transform);
        LabFactory.Primitive(PrimitiveType.Cube, "Rail", transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.7f, 0.012f, 0.028f), StudioSet.Brass(), false);
        LabFactory.Primitive(PrimitiveType.Cylinder, "LampStem", transform, new Vector3(-0.78f, 0.04f, -0.14f), new Vector3(0.014f, 0.028f, 0.014f), StudioSet.Brass(), false);
        LabFactory.Primitive(PrimitiveType.Sphere, "Lamp", transform, new Vector3(-0.78f, 0.08f, -0.14f), Vector3.one * 0.026f, LabFactory.Lit(new Color(1f, 0.9f, 0.72f), false, 0f, 0.45f, true, new Color(1f, 0.72f, 0.32f)), false);
        for (int i = -8; i <= 8; i++)
        {
            float x = i * 0.1f;
            LabFactory.Primitive(PrimitiveType.Cube, "Tick", transform, new Vector3(x, 0.03f, 0.08f), new Vector3(0.003f, 0.006f, i % 2 == 0 ? 0.028f : 0.016f), LabFactory.Lit(NetFoldTheme.Ivory, false, 0.05f, 0.35f), false);
        }

        var axis = new GameObject("Axis").AddComponent<LineRenderer>();
        axis.transform.SetParent(transform, false);
        axis.useWorldSpace = false;
        axis.positionCount = 2;
        axis.SetPosition(0, new Vector3(-0.8f, AxisY, 0f));
        axis.SetPosition(1, new Vector3(0.82f, AxisY, 0f));
        axis.widthMultiplier = 0.003f;
        axis.material = LabFactory.ParticleMat(new Color(NetFoldTheme.Ivory.r, NetFoldTheme.Ivory.g, NetFoldTheme.Ivory.b, 0.7f));
        axis.startColor = axis.endColor = new Color(NetFoldTheme.Ivory.r, NetFoldTheme.Ivory.g, NetFoldTheme.Ivory.b, 0.75f);
    }

    void BuildRigs()
    {
        var pcRig = new GameObject("PC Rig");
        pcRig.transform.SetParent(transform, false);
        var camGo = new GameObject("PC Camera");
        camGo.transform.SetParent(pcRig.transform, false);
        _cam = camGo.AddComponent<Camera>();
        _cam.nearClipPlane = 0.05f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = NetFoldTheme.Void;
        var extra = camGo.AddComponent<UniversalAdditionalCameraData>();
        extra.renderPostProcessing = true;
        camGo.tag = "MainCamera";
        if (FindObjectOfType<AudioListener>() == null)
        {
            camGo.AddComponent<AudioListener>();
        }

        var orbit = camGo.AddComponent<PCOrbitCamera>();
        orbit.Yaw = 58f;
        orbit.Pitch = 26f;
        orbit.Distance = 1.55f;
        orbit.TargetOffset = new Vector3(0f, 0.12f, 0f);
        var look = new GameObject("LookTarget");
        look.transform.SetParent(transform, false);
        orbit.Bind(_input, look.transform);
        StudioSet.Install(transform, _cam, transform.position);
        var xr = XRRigBuilder.Build(transform, new Vector3(0f, 0f, -0.6f));
        _input.PcCamera = _cam;
        _input.PcRig = pcRig;
        _input.XrOrigin = xr.gameObject;
        _input.XrHead = xr.Camera != null ? xr.Camera.transform : null;
        var rays = xr.GetComponentsInChildren<XRRayInteractor>(true);
        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i].name.Contains("Right")) _input.RightRay = rays[i];
            else _input.LeftRay = rays[i];
        }

        _input.ApplyPlatform();
    }
}
