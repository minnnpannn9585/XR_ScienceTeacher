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
    string _state = "分子在不停做无规则运动";

    public void Bootstrap(GameMode startMode)
    {
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
            string view = Views != null && Views.IsMicro ? "微观" : "宏观";
            UI.Refresh(TempText(), SpeedText(), TimeText(), ConcText(), "分子数  " + MoleculeCount(), "当前状态  " + view + " · " + _state, "滴加体积  " + Mathf.RoundToInt(Volume01 * 100f) + "%");
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

        var desk = LabFactory.Primitive(PrimitiveType.Cube, "VirtualDesk", anchor, new Vector3(0f, -0.02f, 0f), new Vector3(1.7f, 0.04f, 1.05f), LabFactory.Lit(new Color(0.4f, 0.27f, 0.16f), false, 0.05f, 0.32f), true);
        desk.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        LabFactory.Primitive(PrimitiveType.Cube, "Floor", transform, new Vector3(0f, -0.01f, 0f), new Vector3(8f, 0.02f, 8f), LabFactory.Lit(new Color(0.05f, 0.07f, 0.1f), false, 0f, 0.15f), false);

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
        cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f);
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

        var key = new GameObject("KeyLight").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.15f;
        key.color = new Color(1f, 0.96f, 0.9f);
        key.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        var fill = new GameObject("FillLight").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.35f;
        fill.color = new Color(0.55f, 0.7f, 1f);
        fill.transform.rotation = Quaternion.Euler(18f, 140f, 0f);
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
        UI.SetMode("挑战模式");
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
                _state = "请先完成当前挑战题";
            }

            return;
        }

        if (_beat >= 4)
        {
            _state = "讲解已完成。自由实验和挑战从主菜单进入。";
            if (UI != null)
            {
                UI.SetLesson("讲解完成", "五步讲解已完成。想自己调节温度、滴品红或混合酒精，从主菜单进入自由实验。");
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
        _state = _heatmap ? "浓度热力图已打开，红色高、蓝色低" : "浓度热力图已关闭";
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
            _state = "温度越高，分子运动越剧烈";
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
            _state = "挑战中请按题目操作";
            return;
        }

        if (_launchMode != GameMode.Free && Step != DriftStep.Diffusion)
        {
            _state = "请进入步骤 3，再滴入品红";
            return;
        }

        BeakerController target = Selected != null && Selected.gameObject.activeInHierarchy && Selected.Role != BeakerRole.Alcohol ? Selected : Hot;
        Diffusion.Drop(target, Volume01);
        SelectBeaker(target);
        _state = target.DisplayName + "中，品红正在从高浓度向低浓度扩散";
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
                _state = "请按当前题目作答";
            }

            return;
        }

        DropDye();
    }

    public void OpenGas()
    {
        if (_challenge || Step != DriftStep.Diffusion)
        {
            _state = "请在步骤 3 打开气体瓶，观察氨分子和二氧化氮扩散";
            return;
        }

        if (_gasStage <= 0)
        {
            _gasStage = 1;
            if (_ammoniaPs != null)
            {
                _ammoniaPs.Play();
            }

            _state = "氨分子扩散，酚酞试纸由近及远变红";
            return;
        }

        _gasStage = 2;
        if (_no2Ps != null)
        {
            _no2Ps.Play();
        }

        _state = "二氧化氮气体正在扩散";
    }

    public void StartElectrolysis()
    {
        if (Selected == null || Selected.Motion == null)
        {
            return;
        }

        Selected.Motion.PulseField();
        _state = "示意：带电微粒在电场中定向移动";
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
            _state = "自动演示在讲解关卡中播放";
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
                _state = "宏观下，水面看起来是静止的";
                yield return new WaitForSeconds(1.8f);
                if (epoch != _epoch) yield break;
                Views.Set(true);
                _state = "微观下，水分子在不停运动";
                yield return new WaitForSeconds(2.4f);
                break;
            case 1:
                ShowOnly(Single.gameObject);
                Single.SetTemperature(25f);
                SelectBeaker(Single);
                Views.Set(true);
                _state = "分子没有固定方向，碰撞后就转向";
                yield return new WaitForSeconds(3f);
                break;
            case 2:
                ShowOnly(Cold.gameObject, Hot.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
                Cold.SetTemperature(12f);
                Hot.SetTemperature(28f);
                SelectBeaker(Hot);
                Views.Set(true);
                _state = "冷水分子慢，热水分子快";
                yield return new WaitForSeconds(1.4f);
                float temp = 28f;
                while (temp < 86f)
                {
                    if (epoch != _epoch) yield break;
                    temp += Time.deltaTime * 18f;
                    Hot.SetTemperature(temp);
                    SyncSlider();
                    _state = "温度升高，分子运动加快";
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
                _state = "热水中的品红扩散更快";
                yield return new WaitForSeconds(4.2f);
                break;
            default:
                ShowOnly(Water.gameObject, Alcohol.gameObject);
                Water.SetTemperature(22f);
                Alcohol.SetTemperature(22f);
                SelectBeaker(Water);
                Views.Set(false);
                Step = DriftStep.Gaps;
                _state = "50 mL 水 + 50 mL 酒精";
                yield return new WaitForSeconds(1.3f);
                if (epoch != _epoch) yield break;
                Alcohol.transform.position = Water.transform.position + new Vector3(0.05f, 0.02f, 0f);
                TryMix();
                yield return new WaitForSeconds(1.4f);
                if (epoch != _epoch) yield break;
                Views.Set(true);
                _state = "分子进入彼此的间隔，总体积变小";
                yield return new WaitForSeconds(2.2f);
                break;
        }

        SetDraggable();
    }

    static string DriftTitle(int index)
    {
        switch (index)
        {
            case 0: return "步骤 1 / 5    宏观和微观";
            case 1: return "步骤 2 / 5    无规则运动";
            case 2: return "步骤 3 / 5    温度的影响";
            case 3: return "步骤 4 / 5    扩散";
            default: return "步骤 5 / 5    分子间的间隔";
        }
    }

    static string DriftBody(int index)
    {
        switch (index)
        {
            case 0:
                return "一杯水放在桌上，水面平静，好像没有变化。水其实由大量分子组成，分子非常小，肉眼看不见。切到微观以后，就能看见这些分子在运动。宏观现象和微观粒子是同一杯水的两个层次。";
            case 1:
                return "分子在不停地做无规则运动：没有固定路线，碰到别的分子就改变方向。只要不是绝对零度，这种运动就不会停。扩散、蒸发、溶解，都和分子的运动有关。";
            case 2:
                return "温度反映分子的平均动能。冷水里分子运动慢，热水里分子运动快。把热水继续加热，温度计上升，粒子速度和曲线也一起变快。所以同样的变化，在热水里总是比冷水更快。";
            case 3:
                return "扩散是不同物质的分子彼此进入对方，方向是从浓度高的地方到浓度低的地方。品红同时滴进冷水和热水：热水中分子运动更剧烈，品红散得更快，先变得均匀。热力图里红色浓度高，蓝色浓度低。";
            default:
                return "分子并不是紧紧挤在一起，中间有空隙。50 mL 水加上 50 mL 酒精，如果没有间隔，混合后应是 100 mL。实际大约只有 92 mL，因为两种分子钻进了彼此的空隙。";
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
        _state = "可以自由调节温度、观察扩散，或把酒精拖进水里";
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
            UI.ShowChoices(new[] { "冷水扩散更快", "热水扩散更快" }, i => AnswerQ1(i == 0 ? Cold : Hot));
            _state = "观察哪一杯品红扩散更快";
        }
        else if (index == 1)
        {
            ShowOnly(Single.gameObject, _thermo);
            Single.SetTemperature(Quiz.GivenTemperature);
            SelectBeaker(Single);
            UI.ShowChoices(new[] { "很快", "中等", "很慢" }, AnswerQ2);
            _state = "预测 " + Quiz.GivenTemperature.ToString("0") + " ℃ 下的扩散快慢";
        }
        else
        {
            ShowOnly(Single.gameObject, _thermo, _heat.gameObject, _cool.gameObject);
            Single.SetTemperature(15f);
            SelectBeaker(Single);
            UI.ShowChoices(new[] { "开始扩散" }, _ => StartJudged());
            _state = "调节温度，使均匀时间落在 5.5–8.5 秒";
        }

        SetDraggable();
        SyncSlider();
        Temperature.ResetCurve(Selected.Temperature);
    }

    void OnFinished(int stars, string reason)
    {
        UI.ClearChoices();
        _state = "挑战完成";
        UI.Result.Show(stars, reason);
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

            _state = "温度越高，扩散越快";
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
        _state = "正确。热水中分子运动更剧烈，颜色会更快变均匀";
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

            _state = "再看温度：高温很快，低温很慢";
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
        _state = "预测正确，正在播放扩散。预期「" + TemperatureController.BandName(TemperatureController.BandFor(Quiz.GivenTemperature)) + "」";
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
        _state = "扩散已开始，请等待溶液均匀";
    }

    void OnUniform(BeakerController beaker)
    {
        if (!_challenge)
        {
            _state = beaker.DisplayName + "已扩散均匀";
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
            _state = "实测 " + beaker.DiffusionElapsed.ToString("0.0") + " 秒，与「" + TemperatureController.BandName(TemperatureController.BandFor(Quiz.GivenTemperature)) + "」相符";
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

        _state = "分子之间有间隔。50 mL + 50 mL = 100 mL，混合后只有 92 mL";
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
        _state = best.DisplayName + "中，品红正在从高浓度向低浓度扩散";
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
            _state = "温度越高，分子运动越剧烈";
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
        _state = "热水中的品红扩散更快";
        yield return new WaitForSeconds(2.4f);
        if (epoch != _epoch) yield break;

        ApplyLearn(DriftStep.Gaps);
        Views.Set(true);
        Alcohol.transform.position = Water.transform.position + new Vector3(0.06f, 0.02f, 0f);
        TryMix();
        yield return new WaitForSeconds(2f);
        if (epoch != _epoch) yield break;
        _state = "自动演示结束。可以自己操作。自由实验和挑战从主菜单进入。";
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
            _state = _q3Running ? "扩散进行中，温度已锁定" : "本题水温已给定";
            return;
        }

        Selected.SetTemperature(Selected.Temperature + delta);
        SyncSlider();
        _state = Selected.DisplayName + " " + Selected.Temperature.ToString("0") + " ℃，分子运动" + (delta > 0f ? "加快" : "减慢");
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
            return "温度  --";
        }

        return "温度  " + Selected.DisplayName + "  " + Selected.Temperature.ToString("0") + " ℃";
    }

    string SpeedText()
    {
        float speed = Selected != null && Selected.Motion != null ? Selected.Motion.AverageSpeed : TemperatureController.MotionScale(25f);
        return "粒子平均速度  " + speed.ToString("0.00") + "  （相对）";
    }

    string TimeText()
    {
        if (Selected == null || (!Selected.Diffusing && !Selected.Uniform && Selected.DiffusionElapsed <= 0f))
        {
            return "扩散时间  --";
        }

        string text = "扩散时间  " + Selected.DiffusionElapsed.ToString("0.0") + " 秒";
        if (Selected.Uniform)
        {
            text += "  已均匀";
        }
        else if (Selected.Diffusing)
        {
            text += "  扩散中";
        }

        return text;
    }

    string ConcText()
    {
        if (_mixed)
        {
            return "浓度分布  50+50=100 mL，实际 92 mL";
        }

        if (!_challenge && Step == DriftStep.Gaps && Water != null && Water.gameObject.activeInHierarchy)
        {
            return "浓度分布  水 50 mL，酒精 50 mL";
        }

        if (Selected == null || Selected.Heatmap == null || Selected.Dye01 <= 0.01f)
        {
            return "浓度分布  尚无品红";
        }

        if (_heatmap)
        {
            return "浓度分布  " + Selected.Heatmap.Describe();
        }

        return "浓度分布  扩散进度 " + Mathf.RoundToInt(Selected.Dye01 * 100f) + "%";
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
            case DriftStep.Motion: return "分子在不停做无规则运动";
            case DriftStep.Temperature: return "温度越高，分子运动越剧烈";
            case DriftStep.Diffusion: return "品红从高浓度区域向低浓度区域扩散";
            default: return "分子之间有间隔";
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
        LabFactory.WorldLabel(go.transform, "品红滴管", new Vector3(0f, 0.16f, 0f));
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
        LabFactory.WorldLabel(go.transform, action == LabAction.Heat ? "加热台" : "冷却台", new Vector3(0f, 0.08f, 0f));
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
        LabFactory.WorldLabel(go.transform, "温度计", new Vector3(0f, 0.22f, 0f));
        prop.Place(new Vector3(0.46f, 0f, 0.02f));
        _thermo = go;
        _all.Add(go);
    }

    void BuildAmmonia()
    {
        _ammonia = MakeBottle("Ammonia", LabAction.Ammonia, new Color(0.75f, 0.9f, 0.55f, 0.8f), "氨水");
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

        LabFactory.WorldLabel(papers.transform, "酚酞试纸", new Vector3(0f, 0.08f, 0f));
        _papersRoot.Place(new Vector3(-0.16f, 0f, -0.22f));
        _all.Add(papers);
    }

    void BuildNo2()
    {
        _no2 = MakeBottle("NitrogenDioxide", LabAction.NitrogenDioxide, new Color(0.62f, 0.28f, 0.12f, 0.85f), "二氧化氮");
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
        var mat = LabFactory.Lit(new Color(0.8f, 0.92f, 1f, 0.18f), true, 0.02f, 0.9f, true, new Color(0.4f, 0.6f, 0.85f, 0.1f));
        LabFactory.Primitive(PrimitiveType.Cylinder, "Dome", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.06f, 0.24f), mat, false);
        LabFactory.WorldLabel(go.transform, "玻璃罩", new Vector3(0f, 0.16f, 0f));
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
        var glass = LabFactory.Lit(new Color(0.85f, 0.93f, 1f, 0.28f), true, 0.02f, 0.85f);
        LabFactory.Primitive(PrimitiveType.Cylinder, "Bottle", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.05f, 0.05f, 0.05f), glass, false);
        var juice = LabFactory.Lit(liquid, true, 0.05f, 0.5f, true, liquid * 0.3f);
        LabFactory.Primitive(PrimitiveType.Cylinder, "Liquid", go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.038f, 0.03f, 0.038f), juice, false);
        LabFactory.Primitive(PrimitiveType.Sphere, "Cap", go.transform, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.035f, LabFactory.Lit(new Color(0.2f, 0.22f, 0.26f), false, 0.4f, 0.6f), false);
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
