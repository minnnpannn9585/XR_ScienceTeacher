using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// One hand. Near UI is an index-finger (or controller-tip) poke. Far UI is a ray
/// that appears while the pinch or trigger is held. World grabs are reported upward
/// to <see cref="XRInputProvider"/>.
/// </summary>
[DefaultExecutionOrder(-200)]
public class XRHandCursor : MonoBehaviour
{
    public bool Right { get; private set; }
    public Transform TrackingSpace { get; private set; }
    public bool Tracked => _sample.Tracked;
    public bool UsingController => _sample.UsingController;
    public bool Pinching => _sample.Pinching;
    public bool PokeHoveringUi => _pokeUi > 0;
    public Vector3 PokePosition => _sample.PokePosition;
    public XRHandSample Sample => _sample;

    public bool NearHit { get; private set; }
    public IInteractable NearTarget { get; private set; }
    public bool Manipulate { get; private set; }
    public bool UiSelect { get; private set; }

    ActionBasedController _controller;
    XRRayInteractor _ray;
    XRInteractorLineVisual _line;
    XRPokeInteractor _poke;
    Transform _aim;
    Transform _pokePoint;
    Transform _controllerVisual;
    Transform _fingerVisual;
    Transform _thumbVisual;
    InputAction _select;
    InputDevice _device;
    XRHandSample _sample;
    IInteractable _touch;
    int _pokeUi;
    int _rayGrace;
    readonly Collider[] _hits = new Collider[16];

    public void Configure(bool right, Transform trackingSpace, ActionBasedController controller, XRRayInteractor ray, XRInteractorLineVisual line, XRPokeInteractor poke, Transform aim, Transform pokePoint)
    {
        Right = right;
        TrackingSpace = trackingSpace;
        _controller = controller;
        _ray = ray;
        _line = line;
        _poke = poke;
        _aim = aim;
        _pokePoint = pokePoint;
        BuildVisuals();
        EnsureSelect();
    }

    void OnEnable()
    {
        if (_poke != null)
        {
            _poke.uiHoverEntered.AddListener(OnPokeEnter);
            _poke.uiHoverExited.AddListener(OnPokeExit);
        }
    }

    void OnDisable()
    {
        if (_poke != null)
        {
            _poke.uiHoverEntered.RemoveListener(OnPokeEnter);
            _poke.uiHoverExited.RemoveListener(OnPokeExit);
        }
    }

    void OnDestroy()
    {
        if (_select != null)
        {
            _select.Disable();
            _select.Dispose();
        }

        if (_device != null)
        {
            InputSystem.RemoveDevice(_device);
        }
    }

    void Update()
    {
        _sample = XRPoseReader.Read(Right).ToWorldSpace(TrackingSpace);
        if (!_sample.Tracked)
        {
            NearHit = false;
            NearTarget = null;
            Manipulate = false;
            UiSelect = false;
            if (_line != null)
            {
                _line.enabled = false;
            }

            if (_ray != null)
            {
                _ray.enableUIInteraction = false;
            }

            if (_poke != null)
            {
                _poke.enableUIInteraction = false;
            }

            SetVisuals(false);
            QueueSelect(false);
            return;
        }

        transform.SetPositionAndRotation(_sample.GripPosition, _sample.GripRotation);
        _aim.SetPositionAndRotation(_sample.AimOrigin, _sample.AimRotation);
        _pokePoint.SetPositionAndRotation(_sample.PokePosition, _sample.PokeRotation);
        if (_poke != null)
        {
            _poke.enableUIInteraction = true;
        }

        SenseNear();
        bool trigger = _sample.UsingController ? _sample.Trigger > 0.55f : _sample.Pinching;
        bool grip = _sample.Grip > 0.55f;
        Manipulate = _sample.UsingController ? trigger || grip : _sample.Pinching;
        UiSelect = trigger && _pokeUi == 0;
        if (UiSelect)
        {
            _rayGrace = 2;
        }
        else if (_rayGrace > 0)
        {
            _rayGrace--;
        }

        bool showRay = _rayGrace > 0 && _pokeUi == 0;
        if (_ray != null)
        {
            _ray.enableUIInteraction = showRay;
        }

        if (_line != null)
        {
            _line.enabled = showRay;
        }

        QueueSelect(showRay && trigger);
        SetVisuals(true);
        if (PokeHoveringUi || NearHit)
        {
            XRPanelFollow.Hold = true;
        }
    }

    public bool ConsumeTap()
    {
        if (!Tracked || Manipulate || PokeHoveringUi || !NearHit || NearTarget == null)
        {
            _touch = NearHit ? NearTarget : null;
            return false;
        }

        var draggable = NearTarget as IDraggable;
        if (draggable != null && draggable.CanDrag)
        {
            _touch = NearTarget;
            return false;
        }

        bool entered = NearTarget != _touch;
        _touch = NearTarget;
        return entered;
    }

    public bool RayHoveringUi()
    {
        return _ray != null && _ray.enableUIInteraction && _ray.TryGetCurrentUIRaycastResult(out var hit) && hit.gameObject != null;
    }

    void SenseNear()
    {
        NearHit = false;
        NearTarget = null;
        int count = Physics.OverlapSphereNonAlloc(_sample.PokePosition, 0.045f, _hits, ~0, QueryTriggerInteraction.Ignore);
        float best = 0.045f * 0.045f;
        for (int i = 0; i < count; i++)
        {
            if (_hits[i] == null)
            {
                continue;
            }

            var interactable = _hits[i].GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            Vector3 closest = _hits[i].ClosestPoint(_sample.PokePosition);
            float sqr = (closest - _sample.PokePosition).sqrMagnitude;
            if (sqr <= best)
            {
                best = sqr;
                NearHit = true;
                NearTarget = interactable;
            }
        }
    }

    void OnPokeEnter(UIHoverEventArgs args)
    {
        _pokeUi++;
    }

    void OnPokeExit(UIHoverEventArgs args)
    {
        _pokeUi = Mathf.Max(0, _pokeUi - 1);
    }

    void EnsureSelect()
    {
        EnsureLayout();
        _device = InputSystem.AddDevice("XRScienceSelect", Right ? "XR Select Right" : "XR Select Left");
        InputSystem.AddDeviceUsage(_device, Right ? "XRSelectRight" : "XRSelectLeft");
        _select = new InputAction(type: InputActionType.Button);
        _select.AddBinding(Right ? "<XRScienceSelect>{XRSelectRight}/select" : "<XRScienceSelect>{XRSelectLeft}/select");
        _select.Enable();
        if (_controller != null)
        {
            _controller.selectAction = new InputActionProperty(_select);
            _controller.activateAction = new InputActionProperty(_select);
            _controller.uiPressAction = new InputActionProperty(_select);
        }
    }

    void QueueSelect(bool pressed)
    {
        if (_device == null)
        {
            return;
        }

        InputSystem.QueueStateEvent(_device, new SynthesizedSelectState { pressed = pressed ? 1f : 0f });
    }

    static bool _layout;

    static void EnsureLayout()
    {
        if (_layout)
        {
            return;
        }

        InputSystem.RemoveLayout("XRScienceSelect");
        InputSystem.RegisterLayout(@"{
            ""name"": ""XRScienceSelect"",
            ""format"": ""XRS1"",
            ""controls"": [
                { ""name"": ""select"", ""layout"": ""Button"", ""offset"": 0, ""format"": ""FLT"", ""sizeInBits"": 32 }
            ]
        }");
        _layout = true;
    }

    void BuildVisuals()
    {
        _controllerVisual = Marker(transform, PrimitiveType.Cube, new Vector3(0.028f, 0.02f, 0.09f), new Vector3(0f, 0f, 0.03f), NetFoldTheme.StoneDeep);
        _fingerVisual = Marker(_pokePoint, PrimitiveType.Sphere, Vector3.one * 0.014f, Vector3.zero, NetFoldTheme.Accent);
        _thumbVisual = Marker(transform, PrimitiveType.Sphere, Vector3.one * 0.012f, Vector3.zero, NetFoldTheme.Hairline);
    }

    void SetVisuals(bool tracked)
    {
        bool hand = tracked && !_sample.UsingController;
        bool controller = tracked && _sample.UsingController;
        if (_controllerVisual != null)
        {
            _controllerVisual.gameObject.SetActive(controller);
        }

        if (_fingerVisual != null)
        {
            _fingerVisual.gameObject.SetActive(hand);
        }

        if (_thumbVisual != null)
        {
            _thumbVisual.gameObject.SetActive(hand && _sample.HasThumb);
            if (hand && _sample.HasThumb)
            {
                _thumbVisual.position = _sample.ThumbPosition;
            }
        }
    }

    static Transform Marker(Transform parent, PrimitiveType type, Vector3 scale, Vector3 localPos, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = type == PrimitiveType.Sphere ? "Finger" : "ControllerBody";
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = LabFactory.Lit(color, false, 0.15f, 0.55f);
        }

        return go.transform;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SynthesizedSelectState : IInputStateTypeInfo
{
    public float pressed;
    public FourCC format => new FourCC('X', 'R', 'S', '1');
}
