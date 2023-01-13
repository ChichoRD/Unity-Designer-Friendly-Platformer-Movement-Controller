using UnityEngine;

public class HorizontalMovementSetHandler3D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody2D _rigidbody;

    public override void ApplyMovementForces()
    {
        Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up);
        Vector2 normalisedProjectedVelocity = new Vector2(projectedVelocity.x, projectedVelocity.z).normalized;
        
        _rigidbody.AddForce(MovementInputter.MovementController.GetCounterMovementForce() * normalisedProjectedVelocity);
        _rigidbody.AddForce(MovementInputter.GetMovementForceDueToInput());
    }

    public override void InitialiseMovementController()
    {
        MovementInputter.InitialiseController(() => _rigidbody.velocity.magnitude,
                                              () => _rigidbody.mass);
    }

    public override void SetMovementInput(Vector2 movementInput)
    {
        MovementInputter.MovementInput = movementInput;
    }
}
