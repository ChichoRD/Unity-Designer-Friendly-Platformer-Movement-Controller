using UnityEngine;

public class PlatformerSetHandler : MonoBehaviour, IJumpSetHandler, IMovementSetHandler
{
    [RequireInterface(typeof(IJumpSetHandler))]
    [SerializeField] private Object _jumpSetHandler;
    public IJumpSetHandler JumpSetHandler => _jumpSetHandler as IJumpSetHandler;
    public IJumpInputter JumpInputter => JumpSetHandler.JumpInputter;

    [RequireInterface(typeof(IMovementSetHandler))]
    [SerializeField] private Object _movementSetHandler;
    public IMovementSetHandler MovementSetHandler => _movementSetHandler as IMovementSetHandler;
    public HorizontalMovementInputter MovementInputter => MovementSetHandler.MovementInputter;

    public void SetMovementInput(Vector2 movementInput)
    {
        MovementSetHandler.SetMovementInput(movementInput);
    }

    public void ApplyMovementForces()
    {
        MovementSetHandler.ApplyMovementForces();
    }

    public void InitialiseMovementController()
    {
        MovementSetHandler.InitialiseMovementController();
    }

    public void InitialiseJumpController()
    {
        JumpSetHandler.InitialiseJumpController();
    }

    public void ApplyGravitationalForces()
    {
        JumpSetHandler.ApplyGravitationalForces();
    }

    private void OnDrawGizmosSelected()
    {
        if (JumpSetHandler == null || JumpInputter == null) return;

        JumpInputter.JumpController.DebugJumpArcs(Color.green, Color.blue, () => Vector3.right * MovementInputter.MovementController.MaxMovementSpeed);
        JumpInputter.JumpController.DebugJumpHeights(Color.green, Color.blue);
    }
}