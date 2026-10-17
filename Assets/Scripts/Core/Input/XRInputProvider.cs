using UnityEngine;

[DefaultExecutionOrder(-80)]
public class XRInputProvider : MonoBehaviour, IInputProvider
{
    InputAdapter _adapter;
    XRHandCursor[] _hands = System.Array.Empty<XRHandCursor>();
    XRHandCursor _active;
    bool _manipulate;
    bool _nearGrab;

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
    public bool TwoHandActive { get; private set; }
    public float TwoHandScaleDelta { get; private set; }
    public Vector2 TwoHandRotateDelta { get; private set; }
    public bool IsPointerOverUi { get; private set; }
    public bool NearContact { get; private set; }
    public Vector3 NearPoint { get; private set; }
    public bool ManipulateHeld { get; private set; }

    float _prevSpan = -1f;

    public void Initialize(InputAdapter adapter)
    {
        _adapter = adapter;
        _hands = adapter != null && adapter.XrOrigin != null
            ? adapter.XrOrigin.GetComponentsInChildren<XRHandCursor>(true)
            : System.Array.Empty<XRHandCursor>();
        enabled = true;
    }

    public void Shutdown()
    {
        enabled = false;
    }

    void Update()
    {
        _active = ChooseHand();
        ReadButtons();
        ReadGesture();
        ReadPointer();
        if (ResultPanel.HasOpenPanel)
        {
            SelectPressed = SelectHeld = ManipulateHeld = false;
            SelectReleased |= _manipulate;
            _manipulate = false;
            _nearGrab = false;
            TwoHandActive = false;
            TwoHandScaleDelta = 0f;
            BackPressed = ConfirmPressed = ResetPressed = ViewTogglePressed = RayTogglePressed = false;
        }
    }

    XRHandCursor ChooseHand()
    {
        XRHandCursor best = null;
        int bestScore = -1;
        for (int i = 0; i < _hands.Length; i++)
        {
            var hand = _hands[i];
            if (hand == null || !hand.Tracked)
            {
                continue;
            }

            int score = hand.Right ? 1 : 0;
            if (hand.Manipulate) score += 4;
            if (hand.PokeHoveringUi || hand.RayHoveringUi()) score += 3;
            if (hand.NearHit) score += 2;
            if (score > bestScore)
            {
                best = hand;
                bestScore = score;
            }
        }

        return best;
    }

    void ReadButtons()
    {
        BackPressed = false;
        ConfirmPressed = false;
        ResetPressed = false;
        ViewTogglePressed = false;
        RayTogglePressed = false;
        var right = XRPoseReader.FindController(true);
        var left = XRPoseReader.FindController(false);
        BackPressed = XRPoseReader.WasPressed(left, "menu") || XRPoseReader.WasPressed(right, "menu");
        ConfirmPressed = XRPoseReader.WasPressed(right, "primaryButton");
        ViewTogglePressed = XRPoseReader.WasPressed(right, "secondaryButton");
        ResetPressed = XRPoseReader.WasPressed(left, "primaryButton");
        RayTogglePressed = XRPoseReader.WasPressed(left, "secondaryButton");
        MoveAxis = Vector2.zero;
        LookDelta = Vector2.zero;
        ZoomDelta = 0f;
        TwoHandRotateDelta = Vector2.zero;
    }

    void ReadGesture()
    {
        bool leftGrab = Grabbing(false);
        bool rightGrab = Grabbing(true);
        TwoHandActive = leftGrab && rightGrab;
        TwoHandScaleDelta = 0f;
        if (TwoHandActive && TryPoint(false, out Vector3 a) && TryPoint(true, out Vector3 b))
        {
            float span = Vector3.Distance(a, b);
            if (_prevSpan > 0f)
            {
                TwoHandScaleDelta = span - _prevSpan;
            }

            _prevSpan = span;
        }
        else
        {
            _prevSpan = -1f;
        }

        bool manipulate = _active != null && _active.Manipulate && !TwoHandActive;
        bool tap = _active != null && _active.ConsumeTap();
        if (manipulate && !_manipulate)
        {
            _nearGrab = _active.NearHit;
        }

        if (!manipulate)
        {
            _nearGrab = false;
        }

        SelectPressed = (manipulate && !_manipulate) || tap;
        SelectHeld = manipulate;
        SelectReleased = !manipulate && _manipulate;
        _manipulate = manipulate;
        ManipulateHeld = manipulate;
        NearContact = _nearGrab;
        NearPoint = _active != null ? _active.PokePosition : Vector3.zero;
        SecondaryHeld = false;
    }

    void ReadPointer()
    {
        IsPointerOverUi = false;
        for (int i = 0; i < _hands.Length; i++)
        {
            if (_hands[i] != null && _hands[i].Tracked && (_hands[i].PokeHoveringUi || _hands[i].RayHoveringUi()))
            {
                IsPointerOverUi = true;
                break;
            }
        }

        if (_active != null && _active.Tracked)
        {
            if (_nearGrab)
            {
                PointerRay = new Ray(_active.PokePosition + Vector3.up * 0.25f, Vector3.down);
                return;
            }

            PointerRay = _active.PointerRay;
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

    bool Grabbing(bool right)
    {
        for (int i = 0; i < _hands.Length; i++)
        {
            var hand = _hands[i];
            if (hand == null || hand.Right != right || !hand.Tracked)
            {
                continue;
            }

            return hand.Manipulate;
        }

        return false;
    }

    bool TryPoint(bool right, out Vector3 point)
    {
        for (int i = 0; i < _hands.Length; i++)
        {
            var hand = _hands[i];
            if (hand != null && hand.Right == right && hand.Tracked)
            {
                point = hand.PokePosition;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }
}
