using DG.Tweening;
using UnityEngine;

public enum LabAction
{
    None,
    Beaker,
    Dropper,
    Heat,
    Cool,
    Ammonia,
    NitrogenDioxide
}

public class LabProp : MonoBehaviour, IInteractable
{
    public LabAction Action;
    public bool CanDrag { get; set; }
    public bool IsSelected { get; private set; }
    public Vector3 LayoutLocal;
    public Vector3 HomeWorld;
    public ExperimentController Lab;

    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    public void Place(Vector3 local)
    {
        LayoutLocal = local;
        transform.localPosition = local;
        RememberHome();
    }

    public void RememberHome()
    {
        HomeWorld = transform.position;
    }

    public void RestoreLayout()
    {
        transform.localPosition = LayoutLocal;
        RememberHome();
    }

    public virtual void OnSelect()
    {
        IsSelected = true;
        if (Lab != null)
        {
            Lab.OnPropSelected(this);
        }
    }

    public virtual void OnDeselect()
    {
        IsSelected = false;
    }

    public virtual void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
    }

    public virtual void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        if (!CanDrag)
        {
            return;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, HomeWorld.y, 0f));
        if (!plane.Raycast(pointerRay, out float dist))
        {
            return;
        }

        Vector3 p = pointerRay.GetPoint(dist);
        p.y = HomeWorld.y + (Action == LabAction.Dropper ? 0.09f : 0.02f);
        transform.position = p;
    }

    public virtual void OnDragEnd()
    {
        if (Lab != null)
        {
            Lab.OnPropReleased(this);
        }
    }

    public void Rotate(Vector2 delta)
    {
    }

    public void Scale(float delta)
    {
    }

    public void SnapHome()
    {
        transform.DOMove(HomeWorld, 0.28f);
    }
}
