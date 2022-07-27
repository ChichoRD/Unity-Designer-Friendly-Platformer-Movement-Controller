using ChichoExtensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController3D : PlayerMovementController<Vector2>
{
    private Rigidbody _rigidbody;
    private Dasher3D _dasher;

    protected override void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        var temp = _dasher;
        _dasher = new Dasher3D(temp, _rigidbody);

        base.Awake();
    }

    public override Vector2 MovementInput { get => throw new System.NotImplementedException(); protected set => throw new System.NotImplementedException(); }

    public override void OnLandMovementBoost() => _rigidbody.AddForce(_customTransformation.MultiplyVector((landMovementBoost * currentMovementForce * Time.fixedDeltaTime * finalMovementInput()).SwapYZ()), ForceMode.Impulse);

    protected override void ApplyJumpForce()
    {
        _rigidbody.AddForce(maxJumpImpulse * jumpDirection, ForceMode.Impulse);
    }

    protected override void AssignMovementInputToCurrent()
    {
        finalMovementInput = () => MovementInput;
    }

    protected override void AssignMovementInputToLast()
    {
        finalMovementInput = () => lastMovementInput;
    }

    protected override float GetMass()
    {
        return _rigidbody.mass;
    }

    protected override Vector3 GetVelocity()
    {
        return _rigidbody.velocity;
    }

    protected override void OnDashPerformed(InputAction.CallbackContext obj)
    {
        Vector3 input = dashDirectionAction == null ? finalMovementInput() : dashDirectionAction.action.ReadValue<Vector2>().SwapYZ();
        _dasher.Dash(_customTransformation.MultiplyVector(input));
    }

    protected override void OnJumpCanceled(InputAction.CallbackContext obj)
    {
        JumpKill(OnForceAdd);

        void OnForceAdd(float force)
        {
            _rigidbody.AddForce(force * groundDirection, ForceMode.Force);
        }
    }

    protected override void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        if (!obj.ReadValueAsButton()) return;

        if (JumpsRemaining <= 0 && coyoteTimeRoutine == null)
        {
            jumpBufferTimeRoutine ??= StartCoroutine(JumpBufferTimeRoutine());
            return;
        }

        PerformJump();
    }

    protected override bool RigidbodyIsNull()
    {
        return !TryGetComponent(out _rigidbody);
    }

    protected override void SetMovementInput()
    {
        MovementInput = movementAction.action.ReadValue<Vector2>();
    }
}
