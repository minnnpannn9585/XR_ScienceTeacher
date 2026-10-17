using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Read-only bridge from Unity's official interactors to the experiment interfaces.
/// Tracking, gestures, stabilization, UI selection and visuals belong to the XRI rig.
/// </summary>
[DefaultExecutionOrder(-90)]
public class XRHandCursor : MonoBehaviour
{
    [SerializeField] bool _right;
    [SerializeField] bool _usingController;
    [SerializeField] ActionBasedController _controller;
    [SerializeField] XRRayInteractor _ray;
    [SerializeField] XRPokeInteractor _poke;
    [SerializeField] Transform _contactPoint;
    [SerializeField] XRHandTrackingEvents _handTracking;
    [SerializeField] InputActionProperty _select;
    [SerializeField] InputActionProperty _activate;

    public bool Right => _right;
    public bool UsingController => _usingController;
    public bool Tracked => isActiveAndEnabled && (_usingController
        ? _controller != null && _controller.currentControllerState.isTracked
        : _handTracking != null && _handTracking.handIsTracked);
    public bool Pinching => Tracked && !_usingController && Pressed(_select);
    public bool Manipulate => Tracked && (Pressed(_select) || (_usingController && Pressed(_activate)));
    public bool PokeHoveringUi => Tracked && _pokeUi > 0;
    public Vector3 PokePosition => _contactPoint != null ? _contactPoint.position : transform.position;
    public XRRayInteractor RayInteractor => _ray;
    public Ray PointerRay
    {
        get
        {
            var origin = _ray.rayOriginTransform != null ? _ray.rayOriginTransform : _ray.transform;
            return new Ray(origin.position, origin.forward);
        }
    }
    public bool NearHit { get; private set; }
    public IInteractable NearTarget { get; private set; }

    IInteractable _touch;
    int _pokeUi;
    readonly Collider[] _hits = new Collider[16];

    public void Configure(bool right, bool usingController, ActionBasedController controller, XRRayInteractor ray,
        XRPokeInteractor poke, Transform contactPoint, XRHandTrackingEvents handTracking,
        InputActionProperty select, InputActionProperty activate)
    {
        _right = right;
        _usingController = usingController;
        _controller = controller;
        _ray = ray;
        _poke = poke;
        _contactPoint = contactPoint;
        _handTracking = handTracking;
        _select = select;
        _activate = activate;
    }

    void OnEnable()
    {
        if (_poke == null) return;
        _poke.uiHoverEntered.AddListener(OnPokeEnter);
        _poke.uiHoverExited.AddListener(OnPokeExit);
    }

    void OnDisable()
    {
        if (_poke != null)
        {
            _poke.uiHoverEntered.RemoveListener(OnPokeEnter);
            _poke.uiHoverExited.RemoveListener(OnPokeExit);
        }
        _pokeUi = 0;
        NearHit = false;
        NearTarget = null;
        _touch = null;
    }

    void Update()
    {
        NearHit = false;
        NearTarget = null;
        if (!Tracked) return;

        // Experiments still implement IInteractable/IDraggable. This proximity
        // query only feeds those domain objects; it never drives the UI or hand.
        int count = Physics.OverlapSphereNonAlloc(PokePosition, 0.045f, _hits, ~0, QueryTriggerInteraction.Ignore);
        float best = 0.045f * 0.045f;
        for (int i = 0; i < count; i++)
        {
            var candidate = _hits[i].GetComponentInParent<IInteractable>();
            if (candidate == null) continue;
            float distance = (_hits[i].ClosestPoint(PokePosition) - PokePosition).sqrMagnitude;
            if (distance > best) continue;
            best = distance;
            NearTarget = candidate;
            NearHit = true;
        }
    }

    public bool ConsumeTap()
    {
        if (!Tracked || Manipulate || PokeHoveringUi || !NearHit || NearTarget == null ||
            (NearTarget is IDraggable draggable && draggable.CanDrag))
        {
            _touch = NearHit ? NearTarget : null;
            return false;
        }
        bool entered = NearTarget != _touch;
        _touch = NearTarget;
        return entered;
    }

    public bool RayHoveringUi() => Tracked && _ray != null && _ray.isActiveAndEnabled &&
        _ray.enableUIInteraction && _ray.TryGetCurrentUIRaycastResult(out var hit) && hit.gameObject != null;

    static bool Pressed(InputActionProperty input) => input.action != null && input.action.enabled && input.action.IsPressed();
    void OnPokeEnter(UIHoverEventArgs _) => _pokeUi++;
    void OnPokeExit(UIHoverEventArgs _) => _pokeUi = Mathf.Max(0, _pokeUi - 1);
}
