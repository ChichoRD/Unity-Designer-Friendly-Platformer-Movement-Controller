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
    
    protected override void Update()
    {
        base.Update();
        StepCheck(finalMovementInput);
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(GetCounterMovementForce(), ForceMode.Force);
        _movementAnimationUpdate.Invoke(GetRawMovement(finalMovementInput));
        _rigidbody.AddForce(GetMovementForce(finalMovementInput), ForceMode.Force);
        _rigidbody.AddForce(GetExtraGravityForce(), ForceMode.Force);
    }

    public override Vector2 MovementInput
    {
        get => movementInput;
        protected set
        {
            float valueAbs = value.sqrMagnitude;
            float movementInputAbs = movementInput.sqrMagnitude;
            if (valueAbs > 0 && movementInputAbs <= 0)
            {
                OnMovementInputStarted?.Invoke();
            }

            if (movementInputAbs > 0 && valueAbs <= 0)
            {
                lastMovementInput = movementInput;
                OnMovementInputEnded?.Invoke();
            }

            movementInput = value;
        }
    }

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

    protected override bool GroundCheckHit(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal)
    {
        var hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, distance, layerMask);
        normal = hitInfo.normal;
        return hit;
    }
}
