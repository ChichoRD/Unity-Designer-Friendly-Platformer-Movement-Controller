using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController2D : PlayerMovementController<float>
{
    private Rigidbody2D _rigidbody;
    [SerializeField] private Dasher2D _dasher;

    protected override void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        var temp = _dasher;
        _dasher = new Dasher2D(temp, _rigidbody);

        base.Awake();
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

    protected override void ApplyJumpForce() => _rigidbody.AddForce(maxJumpImpulse * jumpDirection, ForceMode2D.Impulse);

    protected override bool RigidbodyIsNull() => !TryGetComponent(out _rigidbody);

    protected override void SetMovementInput() => MovementInput = movementAction.action.ReadValue<float>();

    protected override bool GroundCheckHit(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal)
    {
        var hit = Physics2D.Raycast(origin, direction, distance, layerMask);
        normal = hit.normal;
        return hit;
    }

}
