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
    public string Status = "凸透镜对光有会聚作用";
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
        Status = "焦距越小，会聚能力越强";
    }

    public void ToggleLight()
    {
        LightOn = !LightOn;
        Status = LightOn ? "光源已打开" : "光源已关闭";
    }

    public void ToggleRays()
    {
        ShowRays = !ShowRays;
        Status = ShowRays ? "三条特殊光线已显示" : "光路已隐藏";
    }

    public void ToggleGuides()
    {
        ShowGuides = !ShowGuides;
        Status = ShowGuides ? "焦点辅助线已显示" : "辅助线已隐藏";
    }

    public void ToggleDepth()
    {
        DepthOn = !DepthOn;
        Status = DepthOn ? "景深模糊已打开" : "景深模糊已关闭";
    }

    public void ToggleRay(int index)
    {
        if (index == 0) RayParallel = !RayParallel;
        else if (index == 1) RayCenter = !RayCenter;
        else RayFocus = !RayFocus;
        ShowRays = true;
        Status = "可以单独观察每一条特殊光线";
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
                Status = "请先完成当前挑战题";
            }

            return;
        }

        if (_beat >= 7)
        {
            Status = "讲解已完成。自由实验和挑战从主菜单进入。";
            if (UI != null)
            {
                UI.SetLesson("讲解完成", "八步讲解已完成。想自己拖动蜡烛和光屏，从主菜单进入自由实验。");
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
            Status = "自动演示在讲解关卡中播放";
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
                "物距 u  " + LensMath.Centimeters(ObjectDistance),
                "像距 v  " + LensMath.Centimeters(Imaging.ImageDistance),
                "焦距 f  " + LensMath.Centimeters(Focal),
                "放大率  " + mag,
                "像的性质  " + Imaging.Nature,
                "当前规律  " + Imaging.Rule,
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
                Status = "光屏正在移向清晰的实像";
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
                Status = "红：平行于主光轴，折射后过焦点";
                ApplyPlacement();
                yield return new WaitForSeconds(1.7f);
                if (epoch != _epoch) yield break;
                RayCenter = true;
                Status = "绿：通过光心，方向不变";
                yield return new WaitForSeconds(1.7f);
                if (epoch != _epoch) yield break;
                RayFocus = true;
                Status = "蓝：通过焦点，折射后平行于主光轴";
                yield return new WaitForSeconds(1.6f);
                break;
            case 2:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.5f;
                Status = "u > 2f，倒立缩小的实像";
                ApplyPlacement();
                yield return FocusHold(epoch, 0.46f);
                break;
            case 3:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.3f;
                Status = "u = 2f，倒立等大的实像";
                ApplyPlacement();
                yield return new WaitForSeconds(2.2f);
                break;
            case 4:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.22f;
                Status = "f < u < 2f，倒立放大的实像";
                ApplyPlacement();
                yield return new WaitForSeconds(2.2f);
                break;
            case 5:
                ShowRays = true;
                ObjectDistance = 0.15f;
                ScreenDistance = 0.45f;
                Status = "u = f，折射光线平行，不成像";
                ApplyPlacement();
                yield return new WaitForSeconds(2.4f);
                break;
            case 6:
                ShowRays = true;
                ObjectDistance = 0.09f;
                ScreenDistance = 0.4f;
                Status = "u < f，正立放大的虚像";
                ApplyPlacement();
                yield return new WaitForSeconds(2.4f);
                break;
            default:
                ShowRays = true;
                AutoScreen = true;
                ObjectDistance = 0.36f;
                Focal = 0.24f;
                Status = "焦距变小，会聚更强";
                ApplyPlacement();
                float f = 0.24f;
                while (f > 0.11f)
                {
                    if (epoch != _epoch) yield break;
                    f -= Time.deltaTime * 0.035f;
                    Focal = f;
                    yield return null;
                }

                Status = "焦距越小，会聚能力越强";
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
            case 0: return "步骤 1 / 8    认识光具座";
            case 1: return "步骤 2 / 8    三条特殊光线";
            case 2: return "步骤 3 / 8    二倍焦距以外";
            case 3: return "步骤 4 / 8    二倍焦距处";
            case 4: return "步骤 5 / 8    一倍与二倍之间";
            case 5: return "步骤 6 / 8    焦点上";
            case 6: return "步骤 7 / 8    焦点以内";
            default: return "步骤 8 / 8    焦距的影响";
        }
    }

    static string BeatBody(int index)
    {
        switch (index)
        {
            case 0:
                return "光具座上有蜡烛、凸透镜和光屏。蜡烛是物体，凸透镜把光会聚起来，光屏承接实像。物距 u 是物体到透镜的距离，像距 v 是像到透镜的距离，焦距 f 是焦点到透镜的距离。三者满足 1/f = 1/u + 1/v。光屏对准像时，实像最清晰。";
            case 1:
                return "凸透镜对光有会聚作用。平行于主光轴的光线，折射后通过焦点。通过光心的光线方向不变。通过焦点的光线，折射后平行于主光轴。任意两条特殊光线的交点，就是像的位置。";
            case 2:
                return "物体在二倍焦距以外（u > 2f）时，成倒立、缩小的实像。像在透镜另一侧，落在一倍焦距和二倍焦距之间。照相机就是这样：远处的景物，在底片上成缩小的实像。";
            case 3:
                return "物体正好在二倍焦距处（u = 2f）时，成倒立、等大的实像。像也在另一侧的二倍焦距处，物距和像距相等，放大率是 1。";
            case 4:
                return "物体在一倍焦距和二倍焦距之间（f < u < 2f）时，成倒立、放大的实像。像在二倍焦距以外。投影仪、幻灯机利用的就是这种放大的实像。";
            case 5:
                return "物体放在焦点上（u = f）时，折射光线互相平行，不能相交，所以不成像。光屏移到任何位置，都得不到清晰的像。";
            case 6:
                return "物体放在焦点以内（u < f）时，折射光线是发散的。把它们反向延长，会在物体同侧相交，成正立、放大的虚像。虚像不能呈在光屏上，要透过透镜用眼睛看。放大镜就是这样工作的。";
            default:
                return "焦距 f 越小，凸透镜的会聚能力越强。物体位置不变时，焦距调小，像会更靠近透镜，大小也会跟着变。焦距越大，会聚能力越弱。";
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
                Status = "凸透镜对光有会聚作用";
                break;
            case LensStep.Rays:
                ShowRays = true;
                RayParallel = RayCenter = RayFocus = true;
                Status = "观察三条特殊光线如何汇聚";
                break;
            case LensStep.Distance:
                ObjectDistance = 0.5f;
                AutoScreen = true;
                LockScreen = true;
                ShowRays = true;
                Status = "把蜡烛从远处拖向凸透镜";
                break;
            default:
                ObjectDistance = 0.36f;
                Focal = 0.18f;
                AutoScreen = true;
                LockScreen = true;
                ShowRays = true;
                Status = "焦距越小，会聚能力越强";
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
        Status = "自由调节蜡烛、焦距和光屏";
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
            Status = u > Focal ? "物距减小，像的性质在变化" : "u < f，成正立放大虚像";
            yield return null;
        }

        ApplyLearn(LensStep.Focal);
        float f = 0.24f;
        while (f > 0.11f)
        {
            if (epoch != _epoch) yield break;
            f -= Time.deltaTime * 0.04f;
            Focal = f;
            Status = "焦距越小，会聚能力越强";
            yield return null;
        }

        Status = "自动演示结束。可以自己操作。自由实验和挑战从主菜单进入。";
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
