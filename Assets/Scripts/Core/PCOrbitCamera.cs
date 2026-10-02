using UnityEngine;

public class PCOrbitCamera : MonoBehaviour
{
    public Transform Target;
    public float Distance = 2.15f;
    public float MinDistance = 0.9f;
    public float MaxDistance = 4.2f;
    public float Yaw = 20f;
    public float Pitch = 22f;
    public Vector3 TargetOffset = new Vector3(0f, 0.18f, 0f);

    InputAdapter _input;

    public void Bind(InputAdapter input, Transform target)
    {
        _input = input;
        Target = target;
    }

    void LateUpdate()
    {
        if (_input == null || _input.UseXR || Target == null || _input.Provider == null)
        {
            return;
        }

        var p = _input.Provider;
        if (p.SecondaryHeld)
        {
            Yaw += p.LookDelta.x * 0.18f;
            Pitch = Mathf.Clamp(Pitch - p.LookDelta.y * 0.18f, 8f, 80f);
        }

        Distance = Mathf.Clamp(Distance - p.ZoomDelta * 0.18f, MinDistance, MaxDistance);
        Vector3 right = transform.right;
        Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Target.position += (right * p.MoveAxis.x + flat * p.MoveAxis.y) * Time.deltaTime * 1.1f;

        Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
        Vector3 focus = Target.position + TargetOffset;
        transform.position = focus - rot * Vector3.forward * Distance;
        transform.rotation = rot;
    }
}
