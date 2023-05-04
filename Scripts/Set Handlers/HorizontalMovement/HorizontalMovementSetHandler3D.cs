using System;
using UnityEngine;

public class HorizontalMovementSetHandler3D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _relativeTransform;
    private Func<Vector3> _getMovementPlaneNormal;
    private Func<Vector3, Vector3> _getRelativeMovementForce;

    public override void ApplyMovementForces()
    {
        Vector3 projectedNormalisedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, _getMovementPlaneNormal()).normalized;
        Vector3 counterMovementForce = MovementInputter.MovementController.GetCounterMovementForce() * projectedNormalisedVelocity;

        Vector3 movementForce = YZ(MovementInputter.GetMovementForceDueToInput());
        movementForce = _getRelativeMovementForce(movementForce);

        _rigidbody.AddForce(counterMovementForce);
        _rigidbody.AddForce(movementForce);

        static Vector3 YZ(Vector3 vec) => new Vector3(vec.x, vec.z, vec.y);
    }

    public override void InitialiseMovementController()
    {
        _getMovementPlaneNormal = _relativeTransform == null ? () => Vector3.up : () => _relativeTransform.up;
        _getRelativeMovementForce = _relativeTransform == null ? (vec) => vec : (vec) => _relativeTransform.TransformDirection(vec);

        MovementInputter.InitialiseController(_relativeTransform == null ? () => Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).magnitude : () => Vector3.ProjectOnPlane(_rigidbody.velocity, _relativeTransform.up).magnitude,
                                              () => _rigidbody.mass);
    }

    public override void SetMovementInput(Vector2 movementInput)
    {
        MovementInputter.MovementInput = movementInput;
    }
}
