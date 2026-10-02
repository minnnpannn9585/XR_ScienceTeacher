using UnityEngine;

public interface IInteractable : ISelectable, IDraggable, IRotatable, IScalable
{
    Transform Transform { get; }
    GameObject GameObject { get; }
}
