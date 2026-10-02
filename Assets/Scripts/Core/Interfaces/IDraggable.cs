using UnityEngine;

public interface IDraggable
{
    void OnDragStart(Vector3 worldPoint, Ray pointerRay);
    void OnDrag(Vector3 worldPoint, Ray pointerRay);
    void OnDragEnd();
    bool CanDrag { get; }
}
