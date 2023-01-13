using UnityEngine;

public class HorizontalMovementSetHandler2D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody2D _rigidbody2D;

    public override void ApplyMovementForces()
    {
        Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody2D.velocity, Vector3.up);
        Vector2 normalisedProjectedVelocity = new Vector2(projectedVelocity.x, 0f).normalized;
        
        _rigidbody2D.AddForce(MovementInputter.MovementController.GetCounterMovementForce() * normalisedProjectedVelocity);
        _rigidbody2D.AddForce(MovementInputter.GetMovementForceDueToInput());
    }

    public override void InitialiseMovementController()
    {
        MovementInputter.InitialiseController(() => Mathf.Abs(_rigidbody2D.velocity.x),
                                              () => _rigidbody2D.mass);
    }

    public override void SetMovementInput(Vector2 movementInput)
    {
        MovementInputter.MovementInput = movementInput;
    }
}
