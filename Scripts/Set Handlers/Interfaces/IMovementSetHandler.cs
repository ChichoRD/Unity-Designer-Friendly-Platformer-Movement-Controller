using UnityEngine;

public interface IMovementSetHandler
{
    public HorizontalMovementInputter MovementInputter { get; }
    void SetMovementInput(Vector2 movementInput);
    void ApplyMovementForces();
    void InitialiseMovementController();
}