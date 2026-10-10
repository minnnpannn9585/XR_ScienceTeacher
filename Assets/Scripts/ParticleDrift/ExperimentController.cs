using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DriftQuiz = ParticleDrift.ChallengeController;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.UI;

public enum DriftStep
{
    Motion = 0,
    Temperature = 1,
    Diffusion = 2,
    Gaps = 3
}

public class ExperimentController : MonoBehaviour
{
    public InputAdapter Input;
    public InputManager InputHub;
    public TemperatureController Temperature;
    public DyeDiffusionController Diffusion;
    public MicroMacroView Views;
    public DriftQuiz Quiz;
    public StarRatingController Stars;
    public ParticleDriftUI UI;
    public DriftStep Step;
    public float Volume01 = 0.6f;

    public BeakerController Single;
    public BeakerController Cold;
    public BeakerController Hot;
    public BeakerController Water;
    public BeakerController Alcohol;
    public BeakerController Selected;

    Transform _desk;
    LabProp _dropper;
    LabProp _heat;
    LabProp _cool;
    LabProp _ammonia;
    LabProp _papersRoot;
    LabProp _cover;
    LabProp _no2;
    GameObject _thermo;
    Transform _thermoFill;
    Material _thermoMat;
    readonly List<GameObject> _all = new List<GameObject>();
    BeakerController[] _beakers;
    Renderer[] _papers;
    Material[] _paperMats;
    Renderer _cloud;
    Material _cloudMat;
    ParticleSystem _ammoniaPs;
    ParticleSystem _no2Ps;

    GameMode _launchMode = GameMode.Learn;
    bool _challenge;
    bool _heatmap;
    bool _mixed;
    bool _lockTemp;
    bool _sealed;
    bool _awaitQ1;
    bool _awaitQ2;
    bool _q3Running;
    int _gasStage;
    float _ammoniaT;
    float _no2T;
    int _epoch = 1;
    int _beat;
    int _flow = 1;
    int _outlineId;
    Coroutine _demo;
    string _state;

    public void Bootstrap(GameMode startMode)
    {
        _state = Loc.Get("drift.state.random");
        LabFactory.UseWorldLabels = startMode != GameMode.Challenge;
        _launchMode = startMode;
        DOTween.Init();
        EnsureSystems();
        BuildDesk();
        BuildRigs();
        Temperature.Build();
        UI = gameObject.AddComponent<ParticleDriftUI>();
        UI.Build(this, Temperature.CurveTexture, startMode);
        Views.Changed += OnViewChanged;
        Quiz.Presented += OnPresented;
        Quiz.Finished += OnFinished;
        Input.BackPressed += SceneLoader.LoadMainMenu;
        Input.AllowWorldManipulate = startMode != GameMode.Learn;
        if (startMode != GameMode.Learn)
        {
            InputHub.ResetRequested += ResetCurrent;
            InputHub.ViewToggled += ToggleView;
            InputHub.ConfirmRequested += Confirm;
        }
        if (startMode == GameMode.Challenge)
        {
            SetModeChallenge();
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

    void Update()
    {
        float dt = Time.deltaTime;
        if (_beakers == null)
        {
            return;
        }

        for (int i = 0; i < _beakers.Length; i++)
        {
            BeakerController beaker = _beakers[i];
            if (beaker != null && beaker.gameObject.activeInHierarchy)
            {
                beaker.Tick(dt);
                if (beaker.ConsumeUniform())
                {
                    OnUniform(beaker);
                }
            }
        }

        TickGas(dt);
        UpdateDeskThermo();
        float progress = 0f;
        float celsius = Selected != null ? Selected.Temperature : 25f;
        if (Selected != null && Selected.DiffusionDuration > 0.05f && (Selected.Diffusing || Selected.Uniform))
        {
            progress = Mathf.Clamp01(Selected.DiffusionElapsed / Selected.DiffusionDuration);
        }

        Temperature.Tick(dt, celsius, progress);
        if (UI != null)
        {
            string view = Views != null && Views.IsMicro ? Loc.Get("drift.micro") : Loc.Get("drift.macro");
            UI.Refresh(TempText(), SpeedText(), TimeText(), ConcText(), Loc.Format("drift.count", MoleculeCount()), Loc.Format("drift.stateLine", view, _state), Loc.Format("drift.volume", Mathf.RoundToInt(Volume01 * 100f)));
        }
    }

    void EnsureSystems()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            var system = es.AddComponent<EventSystem>();
            system.sendNavigationEvents = false;
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

        Stars = gameObject.AddComponent<StarRatingController>();
        Temperature = gameObject.AddComponent<TemperatureController>();
        Diffusion = gameObject.AddComponent<DyeDiffusionController>();
        Views = gameObject.AddComponent<MicroMacroView>();
        Quiz = gameObject.AddComponent<DriftQuiz>();
        Input = gameObject.AddComponent<InputAdapter>();
        InputHub = gameObject.AddComponent<InputManager>();
        InputHub.Bind(Input);
    }

    void BuildDesk()
    {
        var anchor = new GameObject("DeskAnchor").transform;
        anchor.SetParent(transform, false);
        anchor.position = new Vector3(0f, 0.4f, 0f);
        _desk = anchor;

        var desk = LabFactory.Primitive(PrimitiveType.Cube, "VirtualDesk", anchor, new Vector3(0f, -0.02f, 0f), new Vector3(1.7f, 0.045f, 1.05f), StudioSet.Stone(), true);
        StudioSet.DressDesk(desk.transform);

        Single = MakeBeaker(BeakerRole.Single);
        Cold = MakeBeaker(BeakerRole.Cold);
        Hot = MakeBeaker(BeakerRole.Hot);
        Water = MakeBeaker(BeakerRole.Water);
        Alcohol = MakeBeaker(BeakerRole.Alcohol);
        _beakers = new[] { Single, Cold, Hot, Water, Alcohol };

        Single.Place(new Vector3(0f, 0f, 0.02f));
        Cold.Place(new Vector3(-0.22f, 0f, 0.04f));
        Hot.Place(new Vector3(0.22f, 0f, 0.04f));
        Water.Place(new Vector3(-0.16f, 0f, 0.02f));
        Alcohol.Place(new Vector3(0.22f, 0f, 0.02f));

        _dropper = MakeDropper();
        _dropper.Place(new Vector3(0f, 0f, -0.28f));
        _heat = MakePlate("HeatPlate", LabAction.Heat, new Vector3(0.22f, 0.01f, 0.24f), new Vector3(0.16f, 0.02f, 0.12f), new Color(0.18f, 0.05f, 0.04f), new Color(1f, 0.28f, 0.08f));
        _cool = MakePlate("CoolPlate", LabAction.Cool, new Vector3(-0.22f, 0.01f, 0.24f), new Vector3(0.16f, 0.02f, 0.12f), new Color(0.04f, 0.08f, 0.16f), new Color(0.2f, 0.45f, 1f));
        BuildThermo();
        BuildAmmonia();
        BuildNo2();
        _cover = MakeCover();
        _cover.Place(new Vector3(-0.18f, 0f, -0.22f));
    }

    void BuildRigs()
    {
        var pcRig = new GameObject("PC Rig");
        pcRig.transform.SetParent(transform, false);
        var camGo = new GameObject("PC Camera");
        camGo.transform.SetParent(pcRig.transform, false);
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.02f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NetFoldTheme.Void;
        camGo.AddComponent<UniversalAdditionalCameraData>();
        if (FindObjectOfType<AudioListener>() == null)
        {
            camGo.AddComponent<AudioListener>();
        }

        var orbit = camGo.AddComponent<PCOrbitCamera>();
        var look = new GameObject("LookTarget").transform;
        look.SetParent(_desk, false);
        look.localPosition = new Vector3(0f, 0.08f, 0f);
        orbit.Bind(Input, look);
        Views.Bind(orbit);
        Input.PcCamera = cam;
        Input.PcRig = pcRig;

        StudioSet.Install(transform, cam, _desk.position);
    }

    public void Toolbar(string action)
    {
        switch (action)
        {
            case "重置": ResetCurrent(); break;
            case "自动演示": PlayAutoDemo(); break;
            case "宏观/微观": ToggleView(); break;
            case "浓度热力图": ToggleHeatmap(); break;
            case "温度曲线":
                if (_launchMode == GameMode.Challenge) ToggleCurve();
                break;
            case "提示":
                if (_launchMode == GameMode.Challenge) Hint();
                break;
        }
    }

    public void SetModeLearn()
    {
        if (_launchMode != GameMode.Learn)
        {
            return;
        }

        Invalidate();
        StopDemo();
        if (UI != null && UI.Result != null)
        {
            UI.Result.Hide();
        }

        Quiz.Stop();
        ApplyLearn(DriftStep.Motion);
    }

    public void SetModeChallenge()
    {
        if (_launchMode != GameMode.Challenge)
        {
            return;
        }

        Invalidate();
        StopDemo();
        if (UI != null && UI.Result != null)
        {
            UI.Result.Hide();
        }

        _challenge = true;
        UI.SetMode(Loc.Get("common.challengeMode"));
        Quiz.Begin(Stars);
    }

    public void RestartChallenge()
    {
        SetModeChallenge();
    }

    public void NextStep()
    {
        Invalidate();
        StopDemo();
        if (_launchMode != GameMode.Learn)
        {
            if (_challenge)
            {
                _state = Loc.Get("common.finishQuestion");
            }

            return;
        }

        if (_beat >= 4)
        {
            _state = Loc.Get("common.lesson.menu");
            if (UI != null)
            {
                UI.SetLesson(Loc.Get("drift.lesson.done.title"), Loc.Get("drift.lesson.done.body"));
            }

            return;
        }

        ApplyBeat(_beat + 1);
    }

    public void PrevStep()
    {
        if (_launchMode != GameMode.Learn)
        {
            return;
        }

        ApplyBeat(_beat > 0 ? _beat - 1 : 0);
    }

    public void ResetCurrent()
    {
        Invalidate();
        if (UI != null && UI.Result != null && UI.Result.IsOpen)
        {
            RestartChallenge();
            return;
        }

        StopDemo();
        if (_launchMode == GameMode.Challenge)
        {
            if (Quiz.Running)
            {
                Quiz.Relayout();
            }
            else
            {
                SetModeChallenge();
            }

            return;
        }

        if (_launchMode == GameMode.Free)
        {
            ApplyFree();
            return;
        }

        ApplyBeat(_beat);
    }

    public void ToggleView()
    {
        Views.Toggle();
    }

    public void ToggleHeatmap()
    {
        _heatmap = !_heatmap;
        ApplyHeatmap();
        _state = _heatmap ? Loc.Get("drift.heat.on") : Loc.Get("drift.heat.off");
    }

    public void ToggleCurve()
    {
        Temperature.CurveVisible = !Temperature.CurveVisible;
        UI.SetCurveVisible(Temperature.CurveVisible);
    }

    public void Hint()
    {
        if (_challenge && Quiz.Running)
        {
            Quiz.Hint();
            _state = Quiz.HintText();
        }
    }

    public void HeatSelected()
    {
        Nudge(8f);
    }

    public void CoolSelected()
    {
        Nudge(-8f);
    }

    public void SetTemperatureFromSlider(float value)
    {
        if (Selected == null)
        {
            return;
        }

        if (_lockTemp)
        {
            SyncSlider();
            return;
        }

        Selected.SetTemperature(value);
        if (!_challenge && Step == DriftStep.Temperature)
        {
            _state = Loc.Get("drift.hotter");
        }
    }

    public void SetVolumeFromSlider(float value)
    {
        Volume01 = Mathf.Clamp01(value / 100f);
    }

    public void DropDye()
    {
        if (_challenge)
        {
            _state = Loc.Get("drift.challenge.follow");
            return;
        }

        if (_launchMode != GameMode.Free && Step != DriftStep.Diffusion)
        {
            _state = Loc.Get("drift.drop.step");
            return;
        }

        BeakerController target = Selected != null && Selected.gameObject.activeInHierarchy && Selected.Role != BeakerRole.Alcohol ? Selected : Hot;
        Diffusion.Drop(target, Volume01);
        SelectBeaker(target);
        _state = Loc.Format("drift.dropping", target.DisplayName);
    }

    public void StartDiffusion()
    {
        if (_challenge)
        {
            if (Quiz.Index == 2)
            {
                StartJudged();
            }
            else
            {
                _state = Loc.Get("drift.answer.current");
            }

            return;
        }

        DropDye();
    }

    public void OpenGas()
    {
        if (_challenge || Step != DriftStep.Diffusion)
        {
            _state = Loc.Get("drift.gas.step");
            return;
        }

        if (_gasStage <= 0)
        {
            _gasStage = 1;
            if (_ammoniaPs != null)
            {
                _ammoniaPs.Play();
            }

            _state = Loc.Get("drift.ammonia");
            return;
        }

        _gasStage = 2;
        if (_no2Ps != null)
        {
            _no2Ps.Play();
        }

        _state = Loc.Get("drift.no2");
    }

    public void StartElectrolysis()
    {
        if (Selected == null || Selected.Motion == null)
        {
            return;
        }

        Selected.Motion.PulseField();
        _state = Loc.Get("drift.ion.state");
    }

    public void OnPropSelected(LabProp prop)
    {
        if (prop == null)
        {
            return;
        }

        switch (prop.Action)
        {
            case LabAction.Beaker:
                var beaker = prop as BeakerController;
                SelectBeaker(beaker);
                if (_challenge && Quiz.Index == 0)
                {
                    AnswerQ1(beaker);
                }

                break;
            case LabAction.Heat:
                HeatSelected();
                break;
            case LabAction.Cool:
                CoolSelected();
                break;
            case LabAction.Ammonia:
            case LabAction.NitrogenDioxide:
                OpenGas();
                break;
        }

        if (Input != null)
        {
            Input.ClearSelection();
        }
    }

    public void OnPropReleased(LabProp prop)
    {
        if (prop == null)
        {
            return;
        }

        if (prop.Action == LabAction.Dropper)
        {
            TryDrop(prop);
            prop.SnapHome();
            return;
        }

        var beaker = prop as BeakerController;
        if (beaker != null && beaker.Role == BeakerRole.Alcohol && !TryMix())
        {
            prop.SnapHome();
        }
    }

    public void PlayAutoDemo()
    {
        if (_launchMode != GameMode.Learn)
        {
            _state = Loc.Get("common.demo.learnOnly");
            return;
        }

        Invalidate();
        if (UI != null && UI.Result != null)
        {
            UI.Result.Hide();
        }

        StopDemo();
        Quiz.Stop();
        _demo = StartCoroutine(DemoRoutine());
    }

    void ApplyBeat(int index)
    {
        StopDemo();
        _beat = Mathf.Clamp(index, 0, 4);
        _challenge = false;
        _mixed = false;
        _lockTemp = true;
        _heatmap = false;
        ResetLiquids();
        ResetGas();
        int epoch = _epoch;
        if (UI != null)
        {
            UI.SetLesson(DriftTitle(_beat), DriftBody(_beat));
            UI.ClearChoices();
        }

        _demo = StartCoroutine(PlayBeat(_beat, epoch));
    }

    IEnumerator PlayBeat(int index, int epoch)
    {
        switch (index)
        {
            case 0:
                ShowOnly(Single.gameObject);
                Single.SetTemperature(25f);
                SelectBeaker(Single);
                Views.Set(false);
                _state = Loc.Get("drift.macro.still");
                yield return new WaitForSeconds(1.8f);
                if (epoch != _epoch) yield break;
                Views.Set(true);
                _state = Loc.Get("drift.micro.move");
                yield return new WaitForSeconds(2.4f);
                break;
            case 1:
                ShowOnly(Single.gameObject);
                Single.SetTemperature(25f);
                SelectBeaker(Single);
                Views.Set(true);
                _state = Loc.Get("drift.random.turn");
                yield return new WaitForSeconds(3f);
                break;
            case 2:
                ShowOnly(Cold.gameObject, Hot.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
                Cold.SetTemperature(12f);
                Hot.SetTemperature(28f);
                SelectBeaker(Hot);
                Views.Set(true);
                _state = Loc.Get("drift.cold.hot");
                yield return new WaitForSeconds(1.4f);
                float temp = 28f;
                while (temp < 86f)
                {
                    if (epoch != _epoch) yield break;
                    temp += Time.deltaTime * 18f;
                    Hot.SetTemperature(temp);
                    SyncSlider();
                    _state = Loc.Get("drift.temp.up");
                    yield return null;
                }

                break;
            case 3:
                ShowOnly(Cold.gameObject, Hot.gameObject, _thermo);
                Cold.SetTemperature(12f);
                Hot.SetTemperature(72f);
                Views.Set(false);
                Step = DriftStep.Diffusion;
                SelectBeaker(Hot);
                Diffusion.Drop(Hot, 0.7f);
                Diffusion.Drop(Cold, 0.7f);
                _heatmap = true;
                ApplyHeatmap();
                _state = Loc.Get("drift.hot.faster");
                yield return new WaitForSeconds(4.2f);
                break;
            default:
                ShowOnly(Water.gameObject, Alcohol.gameObject);
                Water.SetTemperature(22f);
                Alcohol.SetTemperature(22f);
                SelectBeaker(Water);
                Views.Set(false);
                Step = DriftStep.Gaps;
                _state = Loc.Get("drift.volumes");
                yield return new WaitForSeconds(1.3f);
                if (epoch != _epoch) yield break;
                Alcohol.transform.position = Water.transform.position + new Vector3(0.05f, 0.02f, 0f);
                TryMix();
                yield return new WaitForSeconds(1.4f);
                if (epoch != _epoch) yield break;
                Views.Set(true);
                _state = Loc.Get("drift.gaps.fill");
                yield return new WaitForSeconds(2.2f);
                break;
        }

        SetDraggable();
    }

    static string DriftTitle(int index)
    {
        switch (index)
        {
            case 0: return Loc.Get("drift.beat1.title");
            case 1: return Loc.Get("drift.beat2.title");
            case 2: return Loc.Get("drift.beat3.title");
            case 3: return Loc.Get("drift.beat4.title");
            default: return Loc.Get("drift.beat5.title");
        }
    }

    static string DriftBody(int index)
    {
        switch (index)
        {
            case 0:
                return Loc.Get("drift.beat1.body");
            case 1:
                return Loc.Get("drift.beat2.body");
            case 2:
                return Loc.Get("drift.beat3.body");
            case 3:
                return Loc.Get("drift.beat4.body");
            default:
                return Loc.Get("drift.beat5.body");
        }
    }

    void ApplyLearn(DriftStep step)
    {
        _challenge = false;
        Step = step;
        _mixed = false;
        _lockTemp = false;
        _sealed = false;
        _q3Running = false;
        ResetLiquids();
        ResetGas();
        switch (step)
        {
            case DriftStep.Motion:
                ShowOnly(Single.gameObject);
                Single.SetTemperature(25f);
                SelectBeaker(Single);
                break;
            case DriftStep.Temperature:
                ShowOnly(Cold.gameObject, Hot.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
                Cold.SetTemperature(12f);
                Hot.SetTemperature(68f);
                SelectBeaker(Hot);
                break;
            case DriftStep.Diffusion:
                ShowOnly(Cold.gameObject, Hot.gameObject, _dropper.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
                Cold.SetTemperature(12f);
                Hot.SetTemperature(68f);
                SelectBeaker(Hot);
                break;
            default:
                ShowOnly(Water.gameObject, Alcohol.gameObject);
                Water.SetTemperature(22f);
                Alcohol.SetTemperature(22f);
                SelectBeaker(Water);
                break;
        }

        SetDraggable();
        _state = LearnSentence(step);
        UI.SetLearn(step);
        UI.ClearChoices();
        SyncSlider();
        Temperature.ResetCurve(Selected != null ? Selected.Temperature : 25f);
    }

    void ApplyFree()
    {
        _challenge = false;
        _mixed = false;
        _lockTemp = false;
        _sealed = false;
        _q3Running = false;
        _heatmap = false;
        ResetLiquids();
        ResetGas();
        ShowOnly(Cold.gameObject, Hot.gameObject, Water.gameObject, Alcohol.gameObject, _dropper.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
        Cold.Place(new Vector3(-0.46f, 0f, 0.06f));
        Hot.Place(new Vector3(-0.16f, 0f, 0.06f));
        Water.Place(new Vector3(0.16f, 0f, 0.06f));
        Alcohol.Place(new Vector3(0.46f, 0f, 0.06f));
        _dropper.Place(new Vector3(0f, 0f, -0.3f));
        Cold.SetTemperature(12f);
        Hot.SetTemperature(68f);
        Water.SetTemperature(22f);
        Alcohol.SetTemperature(22f);
        SelectBeaker(Hot);
        SetDraggable();
        _state = Loc.Get("drift.state.free");
        if (UI != null)
        {
            UI.SetFree();
            UI.ClearChoices();
        }

        SyncSlider();
        Temperature.ResetCurve(Hot.Temperature);
    }

    void OnPresented(int index, string title, string body)
    {
        Invalidate();
        StopDemo();
        _challenge = true;
        _sealed = false;
        _awaitQ1 = false;
        _awaitQ2 = false;
        _q3Running = false;
        _lockTemp = index != 2;
        _mixed = false;
        ResetLiquids();
        ResetGas();
        UI.SetChallengeGuide(title, body);
        UI.ClearChoices();
        if (index == 0)
        {
            ShowOnly(Cold.gameObject, Hot.gameObject, _thermo);
            Cold.SetTemperature(10f);
            Hot.SetTemperature(75f);
            Cold.BeginDiffusion(18);
            Hot.BeginDiffusion(18);
            SelectBeaker(Hot);
            UI.ShowChoices(new[] { Loc.Get("drift.choice.cold"), Loc.Get("drift.choice.hot") }, i => AnswerQ1(i == 0 ? Cold : Hot));
            _state = Loc.Get("drift.watch.which");
        }
        else if (index == 1)
        {
            ShowOnly(Single.gameObject, _thermo);
            Single.SetTemperature(Quiz.GivenTemperature);
            SelectBeaker(Single);
            UI.ShowChoices(new[] { Loc.Get("band.fast"), Loc.Get("band.mid"), Loc.Get("band.slow") }, AnswerQ2);
            _state = Loc.Format("drift.q2.predict", Quiz.GivenTemperature.ToString("0"));
        }
        else
        {
            ShowOnly(Single.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
            Single.SetTemperature(15f);
            SelectBeaker(Single);
            UI.ShowChoices(new[] { Loc.Get("drift.run") }, _ => StartJudged());
            _state = Loc.Get("drift.q3.adjust");
        }

        SetDraggable();
        SyncSlider();
        Temperature.ResetCurve(Selected.Temperature);
    }

    void OnFinished(int stars, string reason)
    {
        UI.ClearChoices();
        _state = Loc.Get("common.done");
        if (UI.Result != null)
        {
            UI.Result.Show(stars, reason);
        }
        else
        {
            UI.SetChallengeGuide(Loc.Get("common.done"), reason);
        }
    }

    void AnswerQ1(BeakerController picked)
    {
        if (_sealed || !Quiz.TryGradeBeaker(picked, Hot, out bool correct))
        {
            return;
        }

        if (!correct)
        {
            PulseOutline(picked);
            if (FeedbackService.Instance != null && picked != null)
            {
                FeedbackService.Instance.Error(picked.transform.position, picked.transform);
            }

            _state = Loc.Get("drift.q1.hotter");
            return;
        }

        _sealed = true;
        Hot.SetTemperature(96f);
        bool already = Hot.Uniform;
        if (!already)
        {
            Hot.Accelerate();
        }

        if (FeedbackService.Instance != null)
        {
            FeedbackService.Instance.Success(Hot.transform.position);
        }

        UI.ClearChoices();
        _state = Loc.Get("drift.q1.correct");
        if (already)
        {
            StartCoroutine(AdvanceSoon());
        }
        else
        {
            _awaitQ1 = true;
        }
    }

    void AnswerQ2(int band)
    {
        if (_sealed || !Quiz.TryGradeBand(band, out bool correct))
        {
            return;
        }

        if (!correct)
        {
            if (FeedbackService.Instance != null)
            {
                FeedbackService.Instance.Error(Single.transform.position, Single.transform);
            }

            _state = Loc.Get("drift.q2.again");
            return;
        }

        _sealed = true;
        Diffusion.Drop(Single, 0.75f);
        if (FeedbackService.Instance != null)
        {
            FeedbackService.Instance.Success(Single.transform.position);
        }

        UI.ClearChoices();
        _awaitQ2 = true;
        _state = Loc.Format("drift.q2.ok", TemperatureController.BandName(TemperatureController.BandFor(Quiz.GivenTemperature)));
    }

    void StartJudged()
    {
        if (!_challenge || Quiz.Index != 2 || _q3Running || _sealed)
        {
            return;
        }

        _q3Running = true;
        _lockTemp = true;
        Single.SetGreen(false);
        Single.ClearDye();
        Single.BeginDiffusion(22);
        _state = Loc.Get("drift.started");
    }

    void OnUniform(BeakerController beaker)
    {
        if (!_challenge)
        {
            _state = Loc.Format("drift.uniform", beaker.DisplayName);
            return;
        }

        if (_awaitQ1 && beaker == Hot)
        {
            _awaitQ1 = false;
            StartCoroutine(AdvanceSoon());
            return;
        }

        if (_awaitQ2 && beaker == Single)
        {
            _awaitQ2 = false;
            _state = Loc.Format("drift.q2.measured", beaker.DiffusionElapsed.ToString("0.0"), TemperatureController.BandName(TemperatureController.BandFor(Quiz.GivenTemperature)));
            StartCoroutine(AdvanceSoon());
            return;
        }

        if (_q3Running && beaker == Single)
        {
            _q3Running = false;
            bool pass = Quiz.TryGradeTime(beaker.DiffusionElapsed, out string message);
            if (string.IsNullOrEmpty(message) && !pass)
            {
                return;
            }

            _state = message;
            if (pass)
            {
                _sealed = true;
                Single.SetGreen(true);
                Single.Motion.Flash();
                if (FeedbackService.Instance != null)
                {
                    FeedbackService.Instance.Success(Single.transform.position);
                    FeedbackService.Instance.Burst(Single.transform.position + Vector3.up * 0.08f, NetFoldTheme.Success, 48);
                }

                UI.ClearChoices();
                StartCoroutine(AdvanceSoon());
                return;
            }

            _lockTemp = false;
            PulseOutline(Single);
            if (FeedbackService.Instance != null)
            {
                FeedbackService.Instance.Error(Single.transform.position, Single.transform);
            }
        }
    }

    bool TryMix()
    {
        if (_challenge || (_launchMode != GameMode.Free && Step != DriftStep.Gaps) || _mixed || Water == null || Alcohol == null)
        {
            return false;
        }

        if (Flat(Alcohol.transform.position, Water.transform.position) > 0.16f)
        {
            return false;
        }

        _mixed = true;
        Alcohol.gameObject.SetActive(false);
        Water.SetLiquidHeight(0.083f);
        Water.Motion.AddAlcohol(32);
        if (FeedbackService.Instance != null)
        {
            FeedbackService.Instance.Success(Water.transform.position);
        }

        _state = Loc.Get("drift.mixed");
        return true;
    }

    void TryDrop(LabProp dropper)
    {
        if (_challenge || (_launchMode != GameMode.Free && Step != DriftStep.Diffusion))
        {
            return;
        }

        BeakerController best = null;
        float bestD = 0.16f;
        Consider(Cold, dropper.transform.position, ref best, ref bestD);
        Consider(Hot, dropper.transform.position, ref best, ref bestD);
        Consider(Water, dropper.transform.position, ref best, ref bestD);
        if (best == null)
        {
            return;
        }

        Diffusion.Drop(best, Volume01);
        SelectBeaker(best);
        _state = Loc.Format("drift.dropping", best.DisplayName);
    }

    IEnumerator DemoRoutine()
    {
        int epoch = _epoch;
        ApplyLearn(DriftStep.Motion);
        Views.Set(true);
        yield return new WaitForSeconds(2.2f);
        if (epoch != _epoch) yield break;

        ApplyLearn(DriftStep.Temperature);
        float temp = 20f;
        Hot.SetTemperature(temp);
        SelectBeaker(Hot);
        while (temp < 82f)
        {
            if (epoch != _epoch) yield break;
            temp += Time.deltaTime * 24f;
            Hot.SetTemperature(temp);
            SyncSlider();
            _state = Loc.Get("drift.hotter");
            yield return null;
        }

        ApplyLearn(DriftStep.Diffusion);
        Views.Set(false);
        Diffusion.Drop(Hot, 0.7f);
        yield return new WaitForSeconds(1.1f);
        if (epoch != _epoch) yield break;
        Diffusion.Drop(Cold, 0.7f);
        _heatmap = true;
        ApplyHeatmap();
        _state = Loc.Get("drift.hot.faster");
        yield return new WaitForSeconds(2.4f);
        if (epoch != _epoch) yield break;

        ApplyLearn(DriftStep.Gaps);
        Views.Set(true);
        Alcohol.transform.position = Water.transform.position + new Vector3(0.06f, 0.02f, 0f);
        TryMix();
        yield return new WaitForSeconds(2f);
        if (epoch != _epoch) yield break;
        _state = Loc.Get("common.demo.done");
    }

    IEnumerator AdvanceSoon()
    {
        int id = _flow;
        yield return new WaitForSeconds(1.35f);
        if (id != _flow || !_challenge || !Quiz.Running)
        {
            yield break;
        }

        Quiz.Advance();
    }

    IEnumerator ClearOutline(BeakerController beaker, int id)
    {
        yield return new WaitForSeconds(1.15f);
        if (id != _outlineId || beaker == null)
        {
            yield break;
        }

        beaker.ShowOutline(false);
    }

    void PulseOutline(BeakerController beaker)
    {
        if (beaker == null)
        {
            return;
        }

        _outlineId++;
        beaker.ShowOutline(true);
        StartCoroutine(ClearOutline(beaker, _outlineId));
    }

    void Confirm()
    {
        if (UI != null && UI.Result != null && UI.Result.IsOpen)
        {
            return;
        }

        if (_challenge)
        {
            if (Quiz.Index == 2)
            {
                StartJudged();
            }

            return;
        }

        if (_launchMode == GameMode.Free)
        {
            if (!TryMix())
            {
                DropDye();
            }

            return;
        }

        if (Step == DriftStep.Motion)
        {
            Views.Set(true);
        }
        else if (Step == DriftStep.Diffusion)
        {
            DropDye();
        }
        else if (Step == DriftStep.Gaps)
        {
            TryMix();
        }
    }

    void OnViewChanged(bool micro)
    {
        if (_beakers == null)
        {
            return;
        }

        for (int i = 0; i < _beakers.Length; i++)
        {
            _beakers[i].ApplyView(micro);
        }
    }

    void Nudge(float delta)
    {
        if (Selected == null)
        {
            return;
        }

        if (_lockTemp)
        {
            _state = _q3Running ? Loc.Get("drift.temp.locked") : Loc.Get("drift.temp.given");
            return;
        }

        Selected.SetTemperature(Selected.Temperature + delta);
        SyncSlider();
        _state = Loc.Format("drift.nudge", Selected.DisplayName, Selected.Temperature.ToString("0"), delta > 0f ? Loc.Get("band.faster") : Loc.Get("band.slower"));
    }

    void SelectBeaker(BeakerController beaker)
    {
        Selected = beaker;
        SyncSlider();
    }

    void SyncSlider()
    {
        if (UI != null && Selected != null)
        {
            UI.SetTempSlider(Selected.Temperature);
        }
    }

    void ShowOnly(params GameObject[] visible)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            var prop = _all[i].GetComponent<LabProp>();
            if (prop != null)
            {
                prop.RestoreLayout();
            }

            _all[i].SetActive(false);
        }

        for (int i = 0; i < visible.Length; i++)
        {
            if (visible[i] != null)
            {
                visible[i].SetActive(true);
            }
        }

        OnViewChanged(Views.IsMicro);
        ApplyHeatmap();
    }

    void ApplyHeatmap()
    {
        for (int i = 0; i < _beakers.Length; i++)
        {
            _beakers[i].SetHeatmap(_heatmap);
        }
    }

    void SetDraggable()
    {
        _dropper.CanDrag = _launchMode == GameMode.Free;
        Alcohol.CanDrag = _launchMode == GameMode.Free;
        Single.CanDrag = false;
        Cold.CanDrag = false;
        Hot.CanDrag = false;
        Water.CanDrag = false;
    }

    void ResetLiquids()
    {
        for (int i = 0; i < _beakers.Length; i++)
        {
            _beakers[i].ClearDye();
            _beakers[i].SetGreen(false);
            _beakers[i].ShowOutline(false);
        }

        Water.Motion.ClearAlcohol();
        Water.SetLiquidHeight(0.07f);
        Alcohol.SetLiquidHeight(0.07f);
    }

    void ResetGas()
    {
        _gasStage = 0;
        _ammoniaT = 0f;
        _no2T = 0f;
        StopSmoke(_ammoniaPs);
        StopSmoke(_no2Ps);
        if (_paperMats != null)
        {
            for (int i = 0; i < _paperMats.Length; i++)
            {
                SetMat(_paperMats[i], Color.white);
            }
        }

        if (_cloud != null)
        {
            _cloud.enabled = false;
        }
    }

    void TickGas(float dt)
    {
        if (_gasStage >= 1)
        {
            _ammoniaT = Mathf.MoveTowards(_ammoniaT, 1f, dt * 0.22f);
        }

        if (_gasStage >= 2)
        {
            _no2T = Mathf.MoveTowards(_no2T, 1f, dt * 0.18f);
        }

        if (_papers != null)
        {
            for (int i = 0; i < _papers.Length; i++)
            {
                float start = i * 0.18f;
                float local = Mathf.InverseLerp(start, start + 0.3f, _ammoniaT);
                SetMat(_paperMats[i], Color.Lerp(Color.white, new Color(0.86f, 0.14f, 0.36f), local));
            }
        }

        if (_cloud != null)
        {
            _cloud.enabled = _no2T > 0.02f;
            _cloud.transform.localScale = Vector3.one * Mathf.Lerp(0.03f, 0.22f, _no2T);
            SetMat(_cloudMat, new Color(0.52f, 0.26f, 0.1f, Mathf.Lerp(0.5f, 0.1f, _no2T)));
        }
    }

    void UpdateDeskThermo()
    {
        if (_thermoFill == null || Selected == null)
        {
            return;
        }

        float h = Mathf.Lerp(0.03f, 0.16f, Selected.Temperature / 100f);
        _thermoFill.localScale = new Vector3(0.018f, h, 0.018f);
        _thermoFill.localPosition = new Vector3(0f, 0.02f + h * 0.5f, 0f);
        SetMat(_thermoMat, Color.Lerp(new Color(0.25f, 0.5f, 1f), new Color(1f, 0.25f, 0.12f), Selected.Temperature / 100f));
    }

    string TempText()
    {
        if (Selected == null)
        {
            return Loc.Get("drift.temp.empty");
        }

        return Loc.Format("drift.temp.value", Selected.DisplayName, Selected.Temperature.ToString("0"));
    }

    string SpeedText()
    {
        float speed = Selected != null && Selected.Motion != null ? Selected.Motion.AverageSpeed : TemperatureController.MotionScale(25f);
        return Loc.Format("drift.speed", speed.ToString("0.00"));
    }

    string TimeText()
    {
        if (Selected == null || (!Selected.Diffusing && !Selected.Uniform && Selected.DiffusionElapsed <= 0f))
        {
            return Loc.Get("drift.time.empty");
        }

        string text = Loc.Format("drift.time", Selected.DiffusionElapsed.ToString("0.0"));
        if (Selected.Uniform)
        {
            text = Loc.Format("drift.time.even", Selected.DiffusionElapsed.ToString("0.0"));
        }
        else if (Selected.Diffusing)
        {
            text = Loc.Format("drift.time.going", Selected.DiffusionElapsed.ToString("0.0"));
        }

        return text;
    }

    string ConcText()
    {
        if (_mixed)
        {
            return Loc.Get("drift.conc.mix");
        }

        if (!_challenge && Step == DriftStep.Gaps && Water != null && Water.gameObject.activeInHierarchy)
        {
            return Loc.Get("drift.conc.separate");
        }

        if (Selected == null || Selected.Heatmap == null || Selected.Dye01 <= 0.01f)
        {
            return Loc.Get("drift.conc.none");
        }

        if (_heatmap)
        {
            return Loc.Format("drift.conc.heat", Selected.Heatmap.Describe());
        }

        return Loc.Format("drift.conc.progress", Mathf.RoundToInt(Selected.Dye01 * 100f));
    }

    int MoleculeCount()
    {
        int count = 0;
        for (int i = 0; i < _beakers.Length; i++)
        {
            if (_beakers[i].gameObject.activeInHierarchy && _beakers[i].Motion != null)
            {
                count += _beakers[i].Motion.MoleculeCount;
            }
        }

        return count;
    }

    static string LearnSentence(DriftStep step)
    {
        switch (step)
        {
            case DriftStep.Motion: return Loc.Get("drift.state.random");
            case DriftStep.Temperature: return Loc.Get("drift.hotter");
            case DriftStep.Diffusion: return Loc.Get("drift.spread.short");
            default: return Loc.Get("drift.gaps.short");
        }
    }

    BeakerController MakeBeaker(BeakerRole role)
    {
        var go = new GameObject(role.ToString());
        go.transform.SetParent(_desk, false);
        var beaker = go.AddComponent<BeakerController>();
        beaker.Lab = this;
        beaker.Build(role);
        _all.Add(go);
        return beaker;
    }

    LabProp MakeDropper()
    {
        var go = new GameObject("Dropper");
        go.transform.SetParent(_desk, false);
        var prop = go.AddComponent<LabProp>();
        prop.Action = LabAction.Dropper;
        prop.Lab = this;
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.07f, 0f);
        box.size = new Vector3(0.05f, 0.14f, 0.05f);
        var bulb = LabFactory.Lit(new Color(0.85f, 0.05f, 0.28f, 0.95f), true, 0.05f, 0.55f, true, new Color(0.7f, 0.05f, 0.2f));
        LabFactory.Primitive(PrimitiveType.Sphere, "Bulb", go.transform, new Vector3(0f, 0.1f, 0f), Vector3.one * 0.04f, bulb, false);
        var stem = LabFactory.Lit(new Color(0.85f, 0.9f, 0.95f, 0.45f), true, 0.02f, 0.8f);
        LabFactory.Primitive(PrimitiveType.Cylinder, "Stem", go.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.012f, 0.04f, 0.012f), stem, false);
        LabFactory.WorldLabel(go.transform, Loc.Get("world.dropper"), new Vector3(0f, 0.16f, 0f));
        _all.Add(go);
        return prop;
    }

    LabProp MakePlate(string name, LabAction action, Vector3 local, Vector3 scale, Color body, Color glow)
    {
        var mat = LabFactory.Lit(body, false, 0.2f, 0.45f, true, glow);
        var go = LabFactory.Primitive(PrimitiveType.Cube, name, _desk, local, scale, mat, true);
        var prop = go.AddComponent<LabProp>();
        prop.Action = action;
        prop.Lab = this;
        prop.Place(local);
        LabFactory.WorldLabel(go.transform, action == LabAction.Heat ? Loc.Get("world.heat") : Loc.Get("world.cool"), new Vector3(0f, 0.08f, 0f));
        _all.Add(go);
        return prop;
    }

    void BuildThermo()
    {
        var go = new GameObject("Thermometer");
        go.transform.SetParent(_desk, false);
        var prop = go.AddComponent<LabProp>();
        prop.Lab = this;
        var back = LabFactory.Lit(new Color(0.92f, 0.94f, 0.96f), false, 0.05f, 0.4f);
        LabFactory.Primitive(PrimitiveType.Cube, "Back", go.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.03f, 0.18f, 0.02f), back, false);
        _thermoMat = LabFactory.Lit(new Color(0.25f, 0.5f, 1f), false, 0.1f, 0.4f, true, new Color(0.3f, 0.5f, 1f));
        var fill = LabFactory.Primitive(PrimitiveType.Cube, "Fill", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.018f, 0.06f, 0.018f), _thermoMat, false);
        _thermoFill = fill.transform;
        LabFactory.WorldLabel(go.transform, Loc.Get("world.thermo"), new Vector3(0f, 0.22f, 0f));
        prop.Place(new Vector3(0.46f, 0f, 0.02f));
        _thermo = go;
        _all.Add(go);
    }

    void BuildAmmonia()
    {
        _ammonia = MakeBottle("Ammonia", LabAction.Ammonia, new Color(0.75f, 0.9f, 0.55f, 0.8f), Loc.Get("world.ammonia"));
        _ammonia.Place(new Vector3(-0.48f, 0f, -0.22f));
        _ammoniaPs = MakeSmoke(_ammonia.transform, new Color(0.75f, 0.95f, 0.45f, 0.8f), 0.012f, 14f, 0.02f, 1.5f, new Vector3(0.14f, 0.03f, 0f));

        var papers = new GameObject("PhenolPapers");
        papers.transform.SetParent(_desk, false);
        _papersRoot = papers.AddComponent<LabProp>();
        _papersRoot.Lab = this;
        _papers = new Renderer[4];
        _paperMats = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            _paperMats[i] = LabFactory.Lit(Color.white, false, 0f, 0.25f);
            var strip = LabFactory.Primitive(PrimitiveType.Cube, "Paper" + i, papers.transform, new Vector3(-0.06f + i * 0.04f, 0.015f, 0f), new Vector3(0.03f, 0.008f, 0.05f), _paperMats[i], false);
            _papers[i] = strip.GetComponent<MeshRenderer>();
        }

        LabFactory.WorldLabel(papers.transform, Loc.Get("world.papers"), new Vector3(0f, 0.08f, 0f));
        _papersRoot.Place(new Vector3(-0.16f, 0f, -0.22f));
        _all.Add(papers);
    }

    void BuildNo2()
    {
        _no2 = MakeBottle("NitrogenDioxide", LabAction.NitrogenDioxide, new Color(0.62f, 0.28f, 0.12f, 0.85f), Loc.Get("world.no2"));
        _no2.Place(new Vector3(0.5f, 0f, -0.22f));
        _no2Ps = MakeSmoke(_no2.transform, new Color(0.62f, 0.3f, 0.12f, 0.75f), 0.02f, 16f, 0.05f, 2.2f, Vector3.zero);
        _cloudMat = LabFactory.Lit(new Color(0.52f, 0.26f, 0.1f, 0.35f), true, 0f, 0.2f, true, new Color(0.45f, 0.2f, 0.08f));
        var cloud = LabFactory.Primitive(PrimitiveType.Sphere, "No2Cloud", _no2.transform, new Vector3(0f, 0.16f, 0f), Vector3.one * 0.03f, _cloudMat, false);
        _cloud = cloud.GetComponent<MeshRenderer>();
        _cloud.enabled = false;
    }

    LabProp MakeCover()
    {
        var go = new GameObject("GlassCover");
        go.transform.SetParent(_desk, false);
        var prop = go.AddComponent<LabProp>();
        prop.Lab = this;
        var mat = LabFactory.Lit(new Color(0.9f, 0.95f, 0.98f, 0.14f), true, 0f, 0.96f, true, new Color(0.55f, 0.7f, 0.85f, 0.06f));
        LabFactory.Primitive(PrimitiveType.Cylinder, "Dome", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.06f, 0.24f), mat, false);
        LabFactory.WorldLabel(go.transform, Loc.Get("world.cover"), new Vector3(0f, 0.16f, 0f));
        _all.Add(go);
        return prop;
    }

    LabProp MakeBottle(string name, LabAction action, Color liquid, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_desk, false);
        var prop = go.AddComponent<LabProp>();
        prop.Action = action;
        prop.Lab = this;
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.07f, 0f);
        box.size = new Vector3(0.07f, 0.16f, 0.07f);
        var glass = LabFactory.Lit(new Color(0.92f, 0.96f, 0.98f, 0.22f), true, 0.02f, 0.94f);
        LabFactory.Primitive(PrimitiveType.Cylinder, "Bottle", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.05f, 0.05f, 0.05f), glass, false);
        var juice = LabFactory.Lit(liquid, true, 0.05f, 0.5f, true, liquid * 0.3f);
        LabFactory.Primitive(PrimitiveType.Cylinder, "Liquid", go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.038f, 0.03f, 0.038f), juice, false);
        LabFactory.Primitive(PrimitiveType.Sphere, "Cap", go.transform, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.035f, LabFactory.Lit(NetFoldTheme.BrassDeep, false, 0.88f, 0.58f), false);
        LabFactory.WorldLabel(go.transform, label, new Vector3(0f, 0.18f, 0f));
        _all.Add(go);
        return prop;
    }

    ParticleSystem MakeSmoke(Transform parent, Color color, float size, float rate, float speed, float life, Vector3 wind)
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;
        if (wind.sqrMagnitude > 0.0001f)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = wind.x;
            velocity.y = wind.y;
            velocity.z = wind.z;
        }

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = LabFactory.ParticleMat(color);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static void StopSmoke(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    static void Consider(BeakerController beaker, Vector3 from, ref BeakerController best, ref float bestD)
    {
        if (beaker == null || !beaker.gameObject.activeInHierarchy)
        {
            return;
        }

        float d = Flat(from, beaker.transform.position);
        if (d < bestD)
        {
            bestD = d;
            best = beaker;
        }
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static void SetMat(Material mat, Color color)
    {
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", color * 0.25f);
        }
    }

    void Invalidate()
    {
        _flow++;
    }

    void StopDemo()
    {
        _epoch++;
        if (_demo != null)
        {
            StopCoroutine(_demo);
            _demo = null;
        }
    }
}
