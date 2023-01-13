using UnityEngine;

public class JumpSetHandler2D : JumpSetHandler
{
    [SerializeField] private Rigidbody2D _rigidbody2D;

    public override void ApplyGravitationalForces()
    {
        _rigidbody2D.AddForce(JumpInputter.JumpController.GetGravityDifference(Physics2D.gravity) * _rigidbody2D.mass);
    }

    public override void InitialiseJumpController()
    {
        JumpInputter.Initialise(() => Vector3.up,
                                () => Vector3.down,
                                () => _rigidbody2D.velocity,
                                () => _rigidbody2D.mass,
                                (f) => _rigidbody2D.AddForce(f, ForceMode2D.Force));
    }
}
