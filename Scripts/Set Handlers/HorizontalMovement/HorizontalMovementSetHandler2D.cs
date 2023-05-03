using UnityEngine;

public class HorizontalMovementSetHandler2D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private Transform _relativeTransform;

    public override void ApplyMovementForces()
    {
        Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody2D.velocity, Vector3.up);
        Vector2 normalisedProjectedVelocity = new Vector2(projectedVelocity.x, 0f).normalized;

        Vector2 counterMovementForce = MovementInputter.MovementController.GetCounterMovementForce() * normalisedProjectedVelocity;
        Vector2 movementForce = MovementInputter.GetMovementForceDueToInput();

        movementForce = _relativeTransform == null ? movementForce : _relativeTransform.TransformDirection(movementForce);

        _rigidbody2D.AddForce(counterMovementForce);
        _rigidbody2D.AddForce(movementForce);
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
