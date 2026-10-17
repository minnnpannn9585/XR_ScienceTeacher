using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Maps official analog actions onto the supplied Oculus animator's two parameters.</summary>
public class XRControllerHandAnimation : MonoBehaviour
{
    public Animator Animator;
    public InputActionProperty Grip;
    public InputActionProperty Trigger;
    static readonly int GripParameter = UnityEngine.Animator.StringToHash("Grip");
    static readonly int TriggerParameter = UnityEngine.Animator.StringToHash("Trigger");

    void LateUpdate()
    {
        if (Animator == null) return;
        Animator.SetFloat(GripParameter, Read(Grip));
        Animator.SetFloat(TriggerParameter, Read(Trigger));
    }

    static float Read(InputActionProperty input) => input.action != null && input.action.enabled ? input.action.ReadValue<float>() : 0f;
}
