using UnityEngine;

/// <summary>InputAdapter supplies both desktop mouse rays and XR hand/controller rays.</summary>
public class TriangleVertexHandle : MonoBehaviour, IInteractable
{
    public TriangleAnglesLab Lab;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public bool CanDrag => true;
    public bool IsSelected { get; private set; }
    public void OnSelect() => IsSelected = true;
    public void OnDeselect() => IsSelected = false;
    public void Rotate(Vector2 delta) { }
    public void Scale(float delta) { }
    public void OnDragStart(Vector3 point, Ray ray) => OnDrag(point, ray);
    public void OnDrag(Vector3 point, Ray ray)
    {
        var provider = InputAdapter.Instance != null ? InputAdapter.Instance.Provider : null;
        if (provider != null && provider.NearContact) Lab.DragVertexNear(provider.NearPoint);
        else Lab.DragVertex(ray);
    }
    public void OnDragEnd() { }
}
