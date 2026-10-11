using UnityEngine;

/// <summary>
/// Pinch the lens rim and lift or lower the hand to change focal length.
/// A trigger ray does the same by moving up and down. The slider stays available.
/// </summary>
public class LensBarrelGrab : MonoBehaviour, IInteractable
{
    public OpticalBenchController Bench;
    public bool CanDrag => Bench != null && !Bench.LockFocal;
    public bool IsSelected { get; private set; }
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    float _startY;
    float _startFocal;

    public void OnSelect()
    {
        IsSelected = true;
    }

    public void OnDeselect()
    {
        IsSelected = false;
    }

    public void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
        _startFocal = Bench != null ? Bench.Focal : 0.15f;
        _startY = SampleY(worldPoint);
    }

    public void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        if (!CanDrag)
        {
            return;
        }

        float y = SampleY(worldPoint);
        Bench.SetFocalMeters(_startFocal + (y - _startY) * 0.45f);
    }

    public void OnDragEnd()
    {
    }

    public void Rotate(Vector2 delta)
    {
        if (!CanDrag)
        {
            return;
        }

        Bench.SetFocalMeters(Bench.Focal - delta.y * 0.0015f);
    }

    public void Scale(float delta)
    {
    }

    float SampleY(Vector3 worldPoint)
    {
        var provider = InputAdapter.Instance != null ? InputAdapter.Instance.Provider : null;
        if (provider != null && provider.NearContact)
        {
            return provider.NearPoint.y;
        }

        return worldPoint.y;
    }
}
