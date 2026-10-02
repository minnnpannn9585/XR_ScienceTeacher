using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class PCInputProvider : MonoBehaviour, IInputProvider
{
    Camera _camera;
    InputAdapter _adapter;

    public Ray PointerRay { get; private set; }
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

    public void Initialize(InputAdapter adapter)
    {
        _adapter = adapter;
        _camera = adapter.PcCamera != null ? adapter.PcCamera : Camera.main;
        enabled = true;
    }

    public void Shutdown()
    {
        enabled = false;
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
        SelectPressed = mouse != null ? mouse.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
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
    }

    static bool IsOverUi(Vector2 screenPos)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (EventSystem.current.currentInputModule is InputSystemUIInputModule)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
