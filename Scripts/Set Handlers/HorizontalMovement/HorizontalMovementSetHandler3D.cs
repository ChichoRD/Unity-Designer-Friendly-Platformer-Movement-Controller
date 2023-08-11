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
        //const float DEBUG_FACTOR = 0.1f;

        Vector3 movementForce = YZ(MovementInputter.GetMovementForceDueToInput());
        movementForce = _getRelativeMovementForce(movementForce);
        movementForce = movementForce.magnitude * Vector3.ProjectOnPlane(movementForce, Vector3.up).normalized;
        //Debug.DrawLine(transform.position, transform.position + movementForce * DEBUG_FACTOR, Color.red);

        Vector3 projectedNormalisedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).normalized;
        Vector3 counterMovementForce = MovementInputter.MovementController.GetCounterMovementForce() * projectedNormalisedVelocity;
        //Debug.DrawLine(transform.position, transform.position + counterMovementForce * DEBUG_FACTOR, Color.blue);

        _rigidbody.AddForce(counterMovementForce + movementForce);
        //_rigidbody.AddForce(movementForce);

        //Debug.Log(_rigidbody.velocity.magnitude);
        //TODO - Relative transform transformation fails when looking up or down, countermovement gets way reduced while movement does not, hence speeeeed

        static Vector3 YZ(Vector3 vec) => new Vector3(vec.x, vec.z, vec.y);
    }

    public override void InitialiseMovementController()
    {
        _getMovementPlaneNormal = _relativeTransform == null ? () => Vector3.up : () => _relativeTransform.up;
        _getRelativeMovementForce = _relativeTransform == null ? (vec) => vec : (vec) => _relativeTransform.TransformDirection(vec);

        MovementInputter.InitialiseController(_relativeTransform == null ?
                                                () => Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up).magnitude :
                                                () => Vector3.ProjectOnPlane(_rigidbody.velocity, Vector3.up/*_relativeTransform.up*/).magnitude,
                                              () => _rigidbody.mass);
    }

    public override void SetMovementInput(Vector2 movementInput)
    {
        MovementInputter.MovementInput = movementInput;
    }
}
