using UnityEngine;

public class HorizontalMovementSetHandler3D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _relativeTransform;

    public override void ApplyMovementForces()
    {
        Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up);
        Vector3 normalisedProjectedVelocity = new Vector3(projectedVelocity.x, 0.0f, projectedVelocity.z).normalized;

        Vector3 counterMovementForce = MovementInputter.MovementController.GetCounterMovementForce() * normalisedProjectedVelocity;
        Vector3 movementForce = YZ(MovementInputter.GetMovementForceDueToInput());

        movementForce = _relativeTransform == null ? movementForce : _relativeTransform.TransformDirection(movementForce);

        _rigidbody.AddForce(counterMovementForce);
        _rigidbody.AddForce(movementForce);

        static Vector3 YZ(Vector3 vec) => new Vector3(vec.x, vec.z, vec.y);
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
