using UnityEngine;

public abstract class HorizontalMovementSetHandler : MonoBehaviour, IMovementSetHandler
{
    [SerializeField] private HorizontalMovementInputter _movementInputter;
    public HorizontalMovementInputter MovementInputter => _movementInputter;

    public abstract void SetMovementInput(Vector2 movementInput);
    public abstract void ApplyMovementForces();
    public abstract void InitialiseMovementController();

    protected virtual void OnValidate()
    {
        InitialiseMovementController();
    }
}
