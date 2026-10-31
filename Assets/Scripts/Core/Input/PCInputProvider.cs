using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class PCInputProvider : MonoBehaviour, IInputProvider
{
    Camera _camera;
    InputAdapter _adapter;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    PointerEventData _pointerData;
    EventSystem _pointerEventSystem;
    InputAction _selectAction;
    Vector2 _pendingPressPosition;
    bool _hasPendingPress;
    bool _ownsUnmergedEvents;
    static int _unmergedEventUsers;
    static InputSettings _mergeSettings;
    static bool _previousMergingDisabled;

    public Ray PointerRay { get; private set; }
    public Ray PressRay { get; private set; }
    public bool PressStartedOverUi { get; private set; }
    public bool SelectPressed { get; private set; }
    public bool SelectHeld { get; private set; }
    public bool SelectReleased { get; private set; }
    public bool SecondaryHeld { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public Vector2 MoveAxis { get; private set; }
    public float ZoomDelta { get; private set; }
    public bool BackPressed { get; private set; }
    public bool ConfirmPressed { get; private set; }
    public bool ResetPressed { get; private set; }
    public bool ViewTogglePressed { get; private set; }
    public bool RayTogglePressed { get; private set; }
    public bool TwoHandActive => false;
    public float TwoHandScaleDelta => 0f;
    public Vector2 TwoHandRotateDelta => Vector2.zero;
    public bool IsPointerOverUi { get; private set; }
    public bool NearContact => false;
    public Vector3 NearPoint => PointerRay.origin;
    public bool ManipulateHeld => SelectHeld;

    public void Initialize(InputAdapter adapter)
    {
        _adapter = adapter;
        _camera = adapter.PcCamera != null ? adapter.PcCamera : Camera.main;
        enabled = true;
        AcquireUnmergedEvents();
        if (_selectAction == null)
        {
            _selectAction = new InputAction("PC pointer press", InputActionType.Button, "<Mouse>/leftButton");
            _selectAction.performed += CapturePress;
        }
        _selectAction.Enable();
    }

    public void Shutdown()
    {
        enabled = false;
    }

    void OnDisable()
    {
        _selectAction?.Disable();
        _hasPendingPress = false;
        ReleaseUnmergedEvents();
    }

    void OnDestroy()
    {
        ReleaseUnmergedEvents();
        if (_selectAction == null) return;
        _selectAction.performed -= CapturePress;
        _selectAction.Dispose();
    }

    void AcquireUnmergedEvents()
    {
        if (_ownsUnmergedEvents) return;
        _ownsUnmergedEvents = true;
        if (_unmergedEventUsers++ != 0) return;
        _mergeSettings = InputSystem.settings;
        _previousMergingDisabled = _mergeSettings.disableRedundantEventsMerging;
        // FastMouse can merge the first press with later movement before actions
        // see it. Keep that event while PC input is active; leave XR's setting alone.
        _mergeSettings.disableRedundantEventsMerging = true;
    }

    void ReleaseUnmergedEvents()
    {
        if (!_ownsUnmergedEvents) return;
        _ownsUnmergedEvents = false;
        if (--_unmergedEventUsers != 0) return;
        if (_mergeSettings != null)
            _mergeSettings.disableRedundantEventsMerging = _previousMergingDisabled;
        _mergeSettings = null;
    }

    void CapturePress(InputAction.CallbackContext context)
    {
        if (!(context.control.device is Mouse mouse)) return;
        // Input events can contain a press followed by a long move (and release)
        // before Update. Preserve the position at the press, not the final position.
        _pendingPressPosition = mouse.position.ReadValue();
        _hasPendingPress = true;
    }

    void Update()
    {
        if (_camera == null)
        {
            _camera = _adapter != null && _adapter.PcCamera != null ? _adapter.PcCamera : Camera.main;
        }

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
        PointerRay = _camera != null ? _camera.ScreenPointToRay(mousePos) : new Ray(Vector3.zero, Vector3.forward);

        bool left = mouse != null ? mouse.leftButton.isPressed : Input.GetMouseButton(0);
        SelectPressed = _hasPendingPress || (mouse != null ? mouse.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0));
        Vector2 pressPosition = _hasPendingPress ? _pendingPressPosition : mousePos;
        PressRay = _camera != null ? _camera.ScreenPointToRay(pressPosition) : PointerRay;
        _hasPendingPress = false;
        SelectHeld = left;
        SelectReleased = mouse != null ? mouse.leftButton.wasReleasedThisFrame : Input.GetMouseButtonUp(0);
        SecondaryHeld = mouse != null ? mouse.rightButton.isPressed : Input.GetMouseButton(1);
        LookDelta = SecondaryHeld && mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        if (SecondaryHeld && mouse == null)
        {
            LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 12f;
        }

        ZoomDelta = 0f;
        if (mouse != null)
        {
            ZoomDelta = mouse.scroll.ReadValue().y / 120f;
        }
        else
        {
            ZoomDelta = Input.mouseScrollDelta.y;
        }

        Vector2 move = Vector2.zero;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            BackPressed = keyboard.escapeKey.wasPressedThisFrame;
            ConfirmPressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            ResetPressed = keyboard.rKey.wasPressedThisFrame;
            ViewTogglePressed = keyboard.tabKey.wasPressedThisFrame;
            RayTogglePressed = keyboard.lKey.wasPressedThisFrame;
        }
        else
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            BackPressed = Input.GetKeyDown(KeyCode.Escape);
            ConfirmPressed = Input.GetKeyDown(KeyCode.Return);
            ResetPressed = Input.GetKeyDown(KeyCode.R);
            ViewTogglePressed = Input.GetKeyDown(KeyCode.Tab);
            RayTogglePressed = Input.GetKeyDown(KeyCode.L);
        }

        MoveAxis = Vector2.ClampMagnitude(move, 1f);
        IsPointerOverUi = IsOverUi(mousePos);
        PressStartedOverUi = SelectPressed && IsOverUi(pressPosition);
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var field = selected != null ? selected.GetComponentInParent<TMP_InputField>() : null;
        bool typing = field != null && field.isFocused;
        if (IsPointerOverUi || typing || ResultPanel.HasOpenPanel)
        {
            SecondaryHeld = false;
            LookDelta = Vector2.zero;
            ZoomDelta = 0f;
        }
        if (typing || ResultPanel.HasOpenPanel)
        {
            MoveAxis = Vector2.zero;
            ResetPressed = false;
            ViewTogglePressed = false;
            RayTogglePressed = false;
            // Enter still submits a numerical answer through InputManager.
            if (ResultPanel.HasOpenPanel) ConfirmPressed = false;
            if (typing) BackPressed = false;
        }
    }

    bool IsOverUi(Vector2 screenPos)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        // Raycast the current position instead of using last frame's UI state.
        if (_pointerData == null || _pointerEventSystem != EventSystem.current)
        {
            _pointerEventSystem = EventSystem.current;
            _pointerData = new PointerEventData(_pointerEventSystem);
        }
        _pointerData.Reset();
        _pointerData.position = screenPos;
        _uiHits.Clear();
        EventSystem.current.RaycastAll(_pointerData, _uiHits);
        for (int i = 0; i < _uiHits.Count; i++)
            if (_uiHits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
}
