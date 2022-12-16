using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputtedMovementController2D : InputtedMovementController<float>
{
    private Rigidbody2D _rigidbody;

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    protected override void Update()
    {
        base.Update();
        StepCheck(finalMovementInput);
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(GetCounterMovementForce(), ForceMode2D.Force);
        _movementAnimationUpdate.Invoke(GetRawMovement(finalMovementInput));
        _rigidbody.AddForce(GetMovementForce(finalMovementInput), ForceMode2D.Force);
        _rigidbody.AddForce(GetExtraGravityForce(), ForceMode2D.Force);
    }

    public override float MovementInput
    {
        get => movementInput;
        protected set
        {
            float valueAbs = Mathf.Abs(value);
            float movementInputAbs = Mathf.Abs(movementInput);
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

    public override void OnLandMovementBoost() => _rigidbody.AddForce(_customTransformation.MultiplyVector(landMovementBoost * currentMovementForce * Time.fixedDeltaTime * finalMovementInput() * Vector2.right), ForceMode2D.Impulse);

    protected override void AssignMovementInputToCurrent() => finalMovementInput = () => MovementInput;
    protected override void AssignMovementInputToLast() => finalMovementInput = () => lastMovementInput;

    protected override float GetMass() => _rigidbody.mass;

    protected override Vector3 GetVelocity() => _rigidbody.velocity;

#if ENABLE_INPUT_SYSTEM

    protected override void OnDashPerformed(InputAction.CallbackContext obj)
    {
        Vector2 input = dashDirectionAction == null ? finalMovementInput() * Vector2.right : dashDirectionAction.action.ReadValue<Vector2>();
        _dasher.Dash(_customTransformation.MultiplyVector(input));
    }
    
    protected override void OnJumpCanceled(InputAction.CallbackContext obj)
    {
        JumpKill(OnForceAdd);

        void OnForceAdd(float force)
        {
            _rigidbody.AddForce(groundDirection * force, ForceMode2D.Force);
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
    
#endif

    protected override void ApplyJumpForce() => _rigidbody.AddForce(maxJumpImpulse * jumpDirection, ForceMode2D.Impulse);

    protected override bool RigidbodyIsNull() => !TryGetComponent(out _rigidbody);

    protected override void SetMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        MovementInput = movementAction.action.ReadValue<float>();
#endif
    }

    protected override bool GroundCheckHit(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal)
    {
        var hit = Physics2D.Raycast(origin, direction, distance, layerMask);
        normal = hit.normal;
        return hit;
    }

}
