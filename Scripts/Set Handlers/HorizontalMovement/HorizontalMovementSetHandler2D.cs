using System;
using UnityEngine;

public class HorizontalMovementSetHandler2D : HorizontalMovementSetHandler
{
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private Transform _relativeTransform;
    private Func<Vector3> _getMovementPlaneNormal;
    private Func<Vector2, Vector2> _getRelativeMovementForce;

    public override void ApplyMovementForces()
    {
        Vector2 projectedNormalisedVelocity = Vector3.ProjectOnPlane(_rigidbody2D.velocity, _getMovementPlaneNormal()).normalized;
        Vector2 counterMovementForce = MovementInputter.MovementController.GetCounterMovementForce() * projectedNormalisedVelocity;

        Vector2 movementForce = MovementInputter.GetMovementForceDueToInput();
        movementForce = _getRelativeMovementForce(movementForce);

        _rigidbody2D.AddForce(counterMovementForce);
        _rigidbody2D.AddForce(movementForce);
    }

    public override void InitialiseMovementController()
    {
        _getMovementPlaneNormal = _relativeTransform == null ? () => Vector3.up : () => _relativeTransform.up;
        _getRelativeMovementForce = _relativeTransform == null ? (vec) => vec : (vec) => _relativeTransform.TransformDirection(vec);

        MovementInputter.InitialiseController(_relativeTransform == null ? () => Mathf.Abs(_rigidbody2D.velocity.x) : () => Mathf.Abs(Vector3.ProjectOnPlane(_rigidbody2D.velocity, _relativeTransform.up).x),
                                              () => _rigidbody2D.mass);
    }

    public override void SetMovementInput(Vector2 movementInput)
    {
        MovementInputter.MovementInput = movementInput;
    }
}
