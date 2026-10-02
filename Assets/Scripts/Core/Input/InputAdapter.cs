using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public enum InteractTool
{
    Select,
    Rotate,
    Scale
}

public class InputAdapter : MonoBehaviour
{
    public static InputAdapter Instance { get; private set; }

    public Camera PcCamera;
    public GameObject PcRig;
    public GameObject XrOrigin;
    public Transform XrHead;
    public UnityEngine.XR.Interaction.Toolkit.XRRayInteractor RightRay;
    public UnityEngine.XR.Interaction.Toolkit.XRRayInteractor LeftRay;
    public LayerMask InteractMask = ~0;

    public IInputProvider Provider { get; private set; }
    public bool UseXR { get; private set; }
    public InteractTool ActiveTool = InteractTool.Select;
    public bool AllowWorldManipulate = true;
    public IInteractable Current { get; private set; }

    public event Action<IInteractable> SelectionChanged;
    public event Action BackPressed;

    IDraggable _drag;
    bool _dragging;
    PCInputProvider _pc;
    XRInputProvider _xr;

    public static bool DetectXR()
    {
        if (XRSettings.isDeviceActive)
        {
            return true;
        }

        var general = XRGeneralSettings.Instance;
        if (general != null && general.Manager != null && general.Manager.activeLoader != null)
        {
            return XRSettings.isDeviceActive;
        }

        return false;
    }

    void Awake()
    {
        Instance = this;
        ApplyPlatform();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyPlatform()
    {
        UseXR = DetectXR();
        if (PcRig != null)
        {
            PcRig.SetActive(!UseXR);
        }

        if (XrOrigin != null)
        {
            XrOrigin.SetActive(UseXR);
        }

        if (_pc != null)
        {
            _pc.Shutdown();
        }

        if (_xr != null)
        {
            _xr.Shutdown();
        }

        if (UseXR)
        {
            _xr = GetComponent<XRInputProvider>();
            if (_xr == null)
            {
                _xr = gameObject.AddComponent<XRInputProvider>();
            }

            Provider = _xr;
        }
        else
        {
            _pc = GetComponent<PCInputProvider>();
            if (_pc == null)
            {
                _pc = gameObject.AddComponent<PCInputProvider>();
            }

            Provider = _pc;
        }

        Provider.Initialize(this);
    }

    void Update()
    {
        if (Provider == null)
        {
            return;
        }

        if (Provider.BackPressed)
        {
            BackPressed?.Invoke();
        }

        if (Current != null)
        {
            if (Provider.TwoHandActive)
            {
                Current.Rotate(Provider.TwoHandRotateDelta);
                Current.Scale(Provider.TwoHandScaleDelta * 2.2f);
            }
            else if (ActiveTool == InteractTool.Rotate && Provider.SelectHeld && !Provider.IsPointerOverUi)
            {
                Current.Rotate(Provider.LookDelta.sqrMagnitude > 0.01f
                    ? Provider.LookDelta * 0.12f
                    : new Vector2(Provider.MoveAxis.x, Provider.MoveAxis.y) * 2.4f);
            }
            else if (ActiveTool == InteractTool.Scale)
            {
                Current.Scale(Provider.ZoomDelta * 0.12f);
            }
        }

        if (UseXR && Current != null && !Provider.TwoHandActive)
        {
            Current.Scale(Provider.ZoomDelta * 0.08f);
        }

        HandlePointer();
    }

    void HandlePointer()
    {
        if (!AllowWorldManipulate)
        {
            return;
        }

        if (Provider.IsPointerOverUi && !_dragging)
        {
            return;
        }

        Ray ray = Provider.PointerRay;
        bool hitInteractable = Physics.Raycast(ray, out RaycastHit hit, 30f, InteractMask, QueryTriggerInteraction.Ignore);
        IInteractable hovered = null;
        IDraggable draggable = null;
        if (hitInteractable)
        {
            var choice = hit.collider.GetComponentInParent<ChallengeChoice>();
            hovered = choice != null ? choice : hit.collider.GetComponentInParent<IInteractable>();
            draggable = hit.collider.GetComponentInParent<IDraggable>();
        }

        if (Provider.SelectPressed)
        {
            if (hovered != null)
            {
                Select(hovered);
            }

            IDraggable dragTarget = ActiveTool == InteractTool.Select ? (hovered as IDraggable ?? draggable) : null;
            if (dragTarget != null && dragTarget.CanDrag)
            {
                _drag = dragTarget;
                _dragging = true;
                _drag.OnDragStart(hit.point, ray);
            }
        }

        if (_dragging && _drag != null)
        {
            Vector3 point = hitInteractable ? hit.point : ray.origin + ray.direction * 1.6f;
            if (Provider.SelectHeld)
            {
                _drag.OnDrag(point, ray);
            }

            if (Provider.SelectReleased)
            {
                _drag.OnDragEnd();
                _dragging = false;
                _drag = null;
            }
        }
        else if (Provider.SelectReleased)
        {
            _dragging = false;
            _drag = null;
        }
    }

    public void Select(IInteractable target)
    {
        if (Current == target)
        {
            return;
        }

        if (Current != null)
        {
            Current.OnDeselect();
        }

        Current = target;
        if (Current != null)
        {
            Current.OnSelect();
        }

        SelectionChanged?.Invoke(Current);
    }

    public void ClearSelection()
    {
        Select(null);
    }
}
