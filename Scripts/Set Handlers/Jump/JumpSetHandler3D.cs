using UnityEngine;

public class JumpSetHandler3D : JumpSetHandler
{
    [SerializeField] private Rigidbody _rigidbody;

    public override void ApplyGravitationalForces()
    {
        _rigidbody.AddForce(JumpInputter.JumpController.GetGravityDifference(Physics.gravity) * _rigidbody.mass);
    }

    public override void InitialiseJumpController()
    {
        JumpInputter.Initialise(() => Vector3.up,
                        () => Vector3.down,
                        () => _rigidbody.velocity,
                        () => _rigidbody.mass,
                        (f) => _rigidbody.AddForce(f, ForceMode.Force));
    }
}
