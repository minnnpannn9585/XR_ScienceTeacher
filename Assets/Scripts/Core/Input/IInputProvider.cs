using UnityEngine;

public interface IInputProvider
{
    void Initialize(InputAdapter adapter);
    void Shutdown();
    Ray PointerRay { get; }
    bool SelectPressed { get; }
    bool SelectHeld { get; }
    bool SelectReleased { get; }
    bool SecondaryHeld { get; }
    Vector2 LookDelta { get; }
    Vector2 MoveAxis { get; }
    float ZoomDelta { get; }
    bool BackPressed { get; }
    bool ConfirmPressed { get; }
    bool ResetPressed { get; }
    bool ViewTogglePressed { get; }
    bool RayTogglePressed { get; }
    bool TwoHandActive { get; }
    float TwoHandScaleDelta { get; }
    Vector2 TwoHandRotateDelta { get; }
    bool IsPointerOverUi { get; }
}
