using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class XRInputProvider : MonoBehaviour, IInputProvider
{
    InputAdapter _adapter;
    XRRayInteractor _rightRay;
    XRRayInteractor _leftRay;
    InputAction _leftStick;
    InputAction _rightStick;
    InputAction _rightTrigger;
    InputAction _leftTrigger;
    InputAction _rightGrip;
    InputAction _leftGrip;
    InputAction _menu;
    InputAction _rightPos;
    InputAction _leftPos;
    bool _selectHeld;
    bool _wasSelect;
    float _prevTwoHandDist = -1f;
    Vector3 _prevTwoHandDir;

    public Ray PointerRay { get; private set; }
    public bool SelectPressed { get; private set; }
    public bool SelectHeld { get; private set; }
    public bool SelectReleased { get; private set; }
    public bool SecondaryHeld { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public Vector2 MoveAxis { get; private set; }
    public float ZoomDelta { get; private set; }
    public bool BackPressed { get; private set; }
    public bool ConfirmPressed => false;
    public bool ResetPressed => false;
    public bool ViewTogglePressed => false;
    public bool RayTogglePressed => false;
    public bool TwoHandActive { get; private set; }
    public float TwoHandScaleDelta { get; private set; }
    public Vector2 TwoHandRotateDelta { get; private set; }
    public bool IsPointerOverUi { get; private set; }

    public void Initialize(InputAdapter adapter)
    {
        _adapter = adapter;
        _rightRay = adapter.RightRay;
        _leftRay = adapter.LeftRay;
        BuildActions();
        enabled = true;
    }

    public void Shutdown()
    {
        DisposeActions();
        enabled = false;
    }

    void OnDestroy()
    {
        DisposeActions();
    }

    void BuildActions()
    {
        _leftStick = NewAction("<XRController>{LeftHand}/thumbstick", "<XRController>{LeftHand}/primary2DAxis", "<XRController>{LeftHand}/joystick");
        _rightStick = NewAction("<XRController>{RightHand}/thumbstick", "<XRController>{RightHand}/primary2DAxis", "<XRController>{RightHand}/joystick");
        _rightTrigger = NewAction("<XRController>{RightHand}/trigger", "<XRController>{RightHand}/triggerButton", "<XRHandDevice>{RightHand}/pinchReady");
        _leftTrigger = NewAction("<XRController>{LeftHand}/trigger", "<XRController>{LeftHand}/triggerButton", "<XRHandDevice>{LeftHand}/pinchReady");
        _rightGrip = NewAction("<XRController>{RightHand}/grip", "<XRController>{RightHand}/gripButton");
        _leftGrip = NewAction("<XRController>{LeftHand}/grip", "<XRController>{LeftHand}/gripButton");
        _menu = NewAction("<XRController>{LeftHand}/menu", "<XRController>{RightHand}/menu", "<XRController>/menuButton");
        _rightPos = NewAction("<XRController>{RightHand}/devicePosition");
        _leftPos = NewAction("<XRController>{LeftHand}/devicePosition");
    }

    static InputAction NewAction(params string[] bindings)
    {
        var action = new InputAction(type: InputActionType.Value);
        for (int i = 0; i < bindings.Length; i++)
        {
            action.AddBinding(bindings[i]);
        }

        action.Enable();
        return action;
    }

    void DisposeActions()
    {
        Dispose(_leftStick);
        Dispose(_rightStick);
        Dispose(_rightTrigger);
        Dispose(_leftTrigger);
        Dispose(_rightGrip);
        Dispose(_leftGrip);
        Dispose(_menu);
        Dispose(_rightPos);
        Dispose(_leftPos);
    }

    static void Dispose(InputAction action)
    {
        if (action == null)
        {
            return;
        }

        action.Disable();
        action.Dispose();
    }

    void Update()
    {
        UpdatePointer();
        bool trigger = ReadPressed(_rightTrigger) || ReadPressed(_leftTrigger);
        SelectPressed = trigger && !_wasSelect;
        SelectHeld = trigger;
        SelectReleased = !trigger && _wasSelect;
        _wasSelect = trigger;
        _selectHeld = trigger;

        bool leftGrip = ReadPressed(_leftGrip);
        bool rightGrip = ReadPressed(_rightGrip);
        TwoHandActive = leftGrip && rightGrip;
        TwoHandScaleDelta = 0f;
        TwoHandRotateDelta = Vector2.zero;

        if (TwoHandActive && _leftPos != null && _rightPos != null)
        {
            Vector3 a = _leftPos.ReadValue<Vector3>();
            Vector3 b = _rightPos.ReadValue<Vector3>();
            float dist = Vector3.Distance(a, b);
            Vector3 dir = b - a;
            if (_prevTwoHandDist > 0f)
            {
                TwoHandScaleDelta = dist - _prevTwoHandDist;
                Vector3 delta = Vector3.Cross(_prevTwoHandDir.normalized, dir.normalized);
                TwoHandRotateDelta = new Vector2(delta.y, -delta.x) * 80f;
            }

            _prevTwoHandDist = dist;
            _prevTwoHandDir = dir;
        }
        else
        {
            _prevTwoHandDist = -1f;
        }

        Vector2 right = _rightStick != null ? _rightStick.ReadValue<Vector2>() : Vector2.zero;
        Vector2 left = _leftStick != null ? _leftStick.ReadValue<Vector2>() : Vector2.zero;
        MoveAxis = left.sqrMagnitude > 0.01f ? left : right;
        LookDelta = right;
        ZoomDelta = right.y * (rightGrip ? 1f : 0f);
        SecondaryHeld = rightGrip && !leftGrip;
        BackPressed = _menu != null && _menu.WasPressedThisFrame();
        IsPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void UpdatePointer()
    {
        if (_rightRay != null && _rightRay.TryGetCurrent3DRaycastHit(out var hit))
        {
            PointerRay = new Ray(_rightRay.rayOriginTransform.position, (hit.point - _rightRay.rayOriginTransform.position).normalized);
            return;
        }

        if (_rightRay != null)
        {
            Transform origin = _rightRay.rayOriginTransform != null ? _rightRay.rayOriginTransform : _rightRay.transform;
            PointerRay = new Ray(origin.position, origin.forward);
            return;
        }

        if (_adapter != null && _adapter.XrHead != null)
        {
            PointerRay = new Ray(_adapter.XrHead.position, _adapter.XrHead.forward);
            return;
        }

        Camera cam = Camera.main;
        PointerRay = cam != null ? new Ray(cam.transform.position, cam.transform.forward) : new Ray(Vector3.zero, Vector3.forward);
    }

    static bool ReadPressed(InputAction action)
    {
        if (action == null)
        {
            return false;
        }

        if (action.expectedControlType == "Button")
        {
            return action.IsPressed();
        }

        return action.ReadValue<float>() > 0.7f || action.IsPressed();
    }
}
