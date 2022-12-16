using ChichoExtensions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public abstract class MovementController<TMovementDimension> : MonoBehaviour where TMovementDimension : struct
{
    #region Movement
    [Header("Movement")]
    [SerializeField][Min(0)] private float _accelerationTime = 0.2f;
    [SerializeField] private AnimationCurve _accelerationCurve;
    private Coroutine _accelerationRoutine;

    [Space]
    [SerializeField][Min(0)] private float _deccelerationTime = 0.3f;
    [SerializeField] private AnimationCurve _deccelerationCurve;
    private Coroutine _deccelerationRoutine;

    [Space]
    [SerializeField][Min(0)] protected float _maxMovementForce;
    protected float currentMovementForce;

    [SerializeField][Min(0)] private float _maxMovementSpeed = 12.08f;
    [SerializeField][Range(0f, 1f)] protected float _counterMovementFactor = 0.05f;
    public float MaxMovementSpeed { get => _maxMovementSpeed; set => _maxMovementSpeed = value; }

    [Space]
    [SerializeField] protected Transform _customTransformator;
    [SerializeField] private bool _useCustomTransformation = true;
    protected Matrix4x4 _customTransformation;
    protected Action _transformationAssignment;

    public bool UseCustomTransformation
    {
        get => _useCustomTransformation;
        set
        {
            _useCustomTransformation = value;
        }
    }

    private float _elapsedMovementTime;
    private float _elapsedStopedTime;

    public Action OnMovementInputStarted;
    public Action OnMovementInputEnded;

    [SerializeField] private UnityEvent _onMovementInputStarted;
    [SerializeField] private UnityEvent _onMovementInputEnded;

    protected TMovementDimension lastMovementInput;
    protected TMovementDimension movementInput;
    public abstract TMovementDimension MovementInput
    {
        get;
        protected set;
    }
    protected Func<TMovementDimension> finalMovementInput = () => default;

    #endregion

    #region Jump
    [Header("Jump Checks")]
    [SerializeField] private Vector2 _groundChecksExtents = Vector2.one;
    [SerializeField][Min(10e-4f)] private float _raycastDensity = 3f;
    [SerializeField][Min(0)] private float _raycastsLength = 0.3f;
    [SerializeField] private LayerMask _jumpableLayers;
    [SerializeField] private Transform _groundCheckCenter;
    [SerializeField][Range(0f, 90f)] private float _maxJumpAngle = 45f;
    [SerializeField][Min(0)] private int _defaultJumpsAmount = 1;
    public int JumpsRemaining { get; set; }

    [Space]
    [SerializeField] private Transform _minStepHeightCheck;
    [SerializeField][Min(0)] private float _maxStepHeight = 0.6f;
    [SerializeField][Min(0)] private float _maxStepDistance = 0.5f;
    [SerializeField] private LayerMask _steppableLayers;
    [SerializeField] private bool _elevateOnly = false;
    [SerializeField][Range(0f, 90f)] private float _minStepAngle = 85f;

    [Header("Jump Settings")]
    [SerializeField] protected float maxJumpImpulse;
    [SerializeField][Min(0)] private float _minJumpTime;
    [SerializeField] private float _defaultExtraGravityForce;
    [SerializeField][Min(0)] private float _fallExtraGravityMultiplier = 2.5f;
    private float _extraGravityForce;
    [SerializeField][Min(0)] private float _jumpKillDelay = 0.2f;

    [Space]
    [SerializeField][Min(0)] private float _coyoteTime = 0.35f;
    protected Coroutine coyoteTimeRoutine;
    [SerializeField][Min(0)] private float _jumpBufferTime = 0.35f;
    protected Coroutine jumpBufferTimeRoutine;
    [SerializeField][Min(0)] protected float landMovementBoost = 8f;

    [Space]
    [SerializeField] private bool _adjustAllJumpParameters = true;
    [SerializeField][Min(0)] private float _maxJumpHeight = 8f;
    [SerializeField][Min(0)] private float _maxJumpTime = 1f;
    [SerializeField][Min(0)] private float _minJumpHeight = 2f;
    [SerializeField][Min(0)] private float _minDescentTime = 1f;

    private float _jumpTimeElapsed;
    private Coroutine _jumpTimeRoutine;
    protected Vector3 jumpDirection = Vector3.up;
    protected Vector3 groundDirection = Vector3.down;
    private bool _onGround;
    public bool OnGround
    {
        get => _onGround;
        private set
        {
            if (value && !_onGround)
            {
                OnLanded?.Invoke();
            }

            if (_onGround && !value)
            {
                OnLeftGround?.Invoke();
            }

            _onGround = value;
        }
    }


    [field: SerializeField] public UnityEvent OnLanded { get; private set; }
    [field: SerializeField] public UnityEvent OnLeftGround { get; private set; }

    private float _velocityY;
    public float VelocityY
    {
        get => _velocityY;
        private set
        {
            if (value < 0 && _velocityY > 0)
            {
                OnDescentStarted?.Invoke();
            }

            _velocityY = value;
        }
    }
    public event Action OnDescentStarted;
    public event Action OnJump;

    #endregion

    #region Animation
    [Header("Animation")]
    [SerializeField][Range(0f, 1f)] private float _rotationSpeed = 0f;
    [SerializeField] private string _movementVelocityYParameter = "MovementVelocityY";
    [SerializeField] private string _movementVelocityXParameter = "MovementVelocityX";
    [SerializeField] private string _movementVelocityMagnitudeParameter = "MovementSpeed";
    [SerializeField] private string _onJumpParameter = "OnJump";
    [SerializeField] private string _onLandParameter = "OnLand";
    protected Animator _animator;
    protected Action<Vector2> _movementAnimationUpdate;

    #endregion

    #region Initialization
    protected virtual void Awake()
    {
        //Animation Events
        _animator = GetComponent<Animator>();
        AnimationEventsSet();

        //Extra Fall Gravity
        OnDescentStarted += ApplyFallGravity;
        OnLanded.AddListener(ResetExtraGravity);

        //Coyote Time
        OnLeftGround.AddListener(OnLeftGroundCoyoteTime);

        //Jump Buffer
        OnLanded.AddListener(BufferedJump);

        //Movement Input reading source
        OnMovementInputStarted += AssignMovementInputToCurrent;
        OnMovementInputEnded += AssignMovementInputToLast;

        //Accelerations
        OnMovementInputStarted += BeginAcceleration;
        OnMovementInputEnded += BeginDecceleration;

        //Extra Events On Movement
        OnMovementInputStarted += _onMovementInputStarted.Invoke;
        OnMovementInputEnded += _onMovementInputEnded.Invoke;

        //Jump Count
        ResetRemainingJumps();
        OnLanded.AddListener(ResetRemainingJumps);
        OnJump += DecreaseRemainingJumps;


        //First Person and Custom Transformation
        if (UseCustomTransformation)
        {
            TransformationToCustomFoward();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        TransformationToWorldDefault();

        void AnimationEventsSet()
        {
            if (_animator == null)
            {
                _movementAnimationUpdate = _ => { };
                return;
            }

            _movementAnimationUpdate += AnimatorMovementSet;
            OnLeftGround.AddListener(AnimatorJumpSet);
            OnLanded.AddListener(AnimatorLandSet);
        }
    }

    #endregion

    #region Finalization

    protected virtual void OnDestroy()
    {
        AnimatorEventsUnset();

        OnDescentStarted -= ApplyFallGravity;
        OnLanded.RemoveListener(ResetExtraGravity);

        OnLeftGround.RemoveListener(OnLeftGroundCoyoteTime);
        OnLanded.RemoveListener(BufferedJump);

        OnMovementInputStarted -= AssignMovementInputToCurrent;
        OnMovementInputEnded -= AssignMovementInputToLast;

        OnMovementInputStarted -= BeginAcceleration;
        OnMovementInputEnded -= BeginDecceleration;

        OnLanded.RemoveListener(ResetRemainingJumps);
        OnJump -= DecreaseRemainingJumps;

        void AnimatorEventsUnset()
        {
            if (_animator == null) return;

            _movementAnimationUpdate -= AnimatorMovementSet;
            OnLeftGround.RemoveListener(AnimatorJumpSet);
            OnLanded.RemoveListener(AnimatorLandSet);
        }
    }

    #endregion

    #region Visual Debugging/Parameters Setting
    protected virtual void OnValidate()
    {
        AdjustJumpParameters();

        void AdjustJumpParameters()
        {
            if (!_adjustAllJumpParameters) return;

            if (RigidbodyIsNull()) return;

            //v = 2 * h / t
            //g = -2 * h / t^2

            float maxJumpVelocity = 2 * _maxJumpHeight / _maxJumpTime;
            maxJumpImpulse = maxJumpVelocity * GetMass();

            float desiredExtraGravity = (-2 * _maxJumpHeight / Mathf.Pow(_maxJumpTime, 2)) - Physics2D.gravity.y;
            _defaultExtraGravityForce = desiredExtraGravity * GetMass();

            float minJumpTime = Mathf.Max((2 * _minJumpHeight) / maxJumpVelocity - _jumpKillDelay, 0f);
            _minJumpTime = minJumpTime;

            //(v - dv) * r = f * dt / m
            //(v - dv) * r = dv
            //(v * r - dv * r) = dv
            //v * r = dv * (r + 1)
            //(v * r) / (r + 1) = dv
            //(v * r * m) / ((r + 1) * dt) = f
            float maxMovementForce = (_maxMovementSpeed * _counterMovementFactor * GetMass()) / (Time.fixedDeltaTime);
            _maxMovementForce = maxMovementForce;

            float extraFallGravity = (-2 * _maxJumpHeight / Mathf.Pow(_minDescentTime, 2)) - Physics2D.gravity.y;
            float extraFallGravityForce = extraFallGravity * GetMass();
            _fallExtraGravityMultiplier = extraFallGravityForce / _defaultExtraGravityForce;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        DebugGroundChecks();

        DrawJumpDistances();

        DrawStepChecks();

        DrawMaximumJumpArc(maxJumpImpulse);

        void DebugGroundChecks()
        {
            if (_groundCheckCenter == null || _raycastDensity == 0) return;

            float delta = 1 / _raycastDensity;

            for (float i = 0; i <= _groundChecksExtents.x; i += delta)
            {
                for (float j = 0; j <= _groundChecksExtents.y; j += delta)
                {
                    Vector3 origin = _groundCheckCenter.position + new Vector3(i, 0, j) - (_groundChecksExtents / 2).SwapYZ();
                    Debug.DrawLine(origin, origin + groundDirection * _raycastsLength, Color.red);
                }
            }
        }

        void DrawJumpDistances()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + jumpDirection * _maxJumpHeight);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + jumpDirection * _minJumpHeight);
        }

        void DrawStepChecks()
        {
            if (_minStepHeightCheck == null) return;

            Vector3 minStepOrigin = _minStepHeightCheck.position;
            Vector3 maxStepOrigin = minStepOrigin + jumpDirection * _maxStepHeight;

            Vector3 size = new Vector3(_maxStepDistance * 2, Time.fixedDeltaTime, _maxStepDistance * 2);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(minStepOrigin, size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(maxStepOrigin, size);
        }

        void DrawMaximumJumpArc(float impulse)
        {
            float jumpVelocity = impulse / GetMass();
            float addedAcceleration = Physics2D.gravity.y + _defaultExtraGravityForce / GetMass();
            float dt = Time.fixedDeltaTime;

            Vector3 lastPosition;
            Vector3 position = transform.position;
            Vector3 velocity = new Vector3(_maxMovementSpeed, jumpVelocity);
            Vector3 acceleration = new Vector3(0, addedAcceleration);

            for (float t = 0; t < _maxJumpTime; t += dt)
            {
                lastPosition = position;
                position += velocity * dt /*Verlet*/ + 0.5f * dt * dt * acceleration;
                velocity += acceleration * dt;

                Gizmos.DrawLine(lastPosition, position);
            }

            addedAcceleration = Physics2D.gravity.y + _defaultExtraGravityForce * _fallExtraGravityMultiplier / GetMass();
            acceleration = new Vector3(0, addedAcceleration);

            float descentTime = Mathf.Sqrt(-2 * _maxJumpHeight / addedAcceleration);

            for (float t = 0; t < descentTime; t += dt)
            {
                lastPosition = position;
                position += velocity * dt /*Verlet*/ + 0.5f * dt * dt * acceleration;
                velocity += acceleration * dt;

                Gizmos.DrawLine(lastPosition, position);
            }
        }
    }

    protected abstract bool RigidbodyIsNull();
    protected abstract float GetMass();

    #endregion

    #region Update Functions

    protected abstract void SetMovementInput();
    protected abstract Vector3 GetVelocity();

    protected virtual Vector3 GetCounterMovementForce()
    {
        Vector3 countermovementForce = (-GetVelocity().NoY() * GetMass() / Time.fixedDeltaTime) * _counterMovementFactor;
        return countermovementForce;
    }

    protected virtual Vector3 GetRawMovement(Func<float> finalMovementInput)
    {
        Vector2 rawMovement = currentMovementForce * finalMovementInput() / _maxMovementForce * Vector2.right;
        return rawMovement;
    }

    protected virtual Vector3 GetRawMovement(Func<Vector2> finalMovementInput)
    {
        Vector3 rawMovement = finalMovementInput() * currentMovementForce / _maxMovementForce;
        return rawMovement;
    }

    protected virtual Vector3 GetMovementForce(Func<float> finalMovementInput)
    {
        Vector3 movement = _customTransformation.MultiplyVector(currentMovementForce * finalMovementInput() * Vector2.right);
        return movement;
    }

    protected virtual Vector3 GetMovementForce(Func<Vector3> finalMovementInput)
    {
        Vector3 movement = _customTransformation.MultiplyVector(finalMovementInput().SwapXY()).NoY().normalized * currentMovementForce;
        return movement;
    }

    protected virtual Vector3 GetMovementForce(Func<Vector2> finalMovementInput)
    {
        Vector3 movement = _customTransformation.MultiplyVector(finalMovementInput().SwapYZ()).NoY().normalized * currentMovementForce;
        return movement;
    }

    protected virtual Vector3 GetExtraGravityForce()
    {
        Vector3 extraGravityForce = -_extraGravityForce * groundDirection;
        return extraGravityForce;
    }

    #endregion

    #region Update Loops

    protected virtual void Update()
    {
        _transformationAssignment.Invoke();

        SetMovementInput();
        VelocityY = GetVelocity().y;

        GroundCheck();
        void GroundCheck()
        {
            if (_groundCheckCenter == null || _raycastDensity == 0) return;

            float delta = 1 / _raycastDensity;

            Vector3 averageNormal = Vector3.zero;
            for (float i = 0; i <= _groundChecksExtents.x; i += delta)
            {
                for (float j = 0; j <= _groundChecksExtents.y; j += delta)
                {
                    Vector3 origin = _groundCheckCenter.position + new Vector3(i, 0, j) - (_groundChecksExtents / 2).SwapYZ();

                    var hit = GroundCheckHit(origin, groundDirection, _raycastsLength, _jumpableLayers, out Vector3 normal);
                    if (!hit) continue;

                    averageNormal += normal;
                }
            }

            averageNormal.Normalize();

            float angle = Vector3.Angle(jumpDirection, averageNormal);
            OnGround = averageNormal != Vector3.zero && angle < _maxJumpAngle && VelocityY <= 0;
        }
    }

    protected abstract bool GroundCheckHit(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal);

    #endregion

    #region Jump Functions
#if ENABLE_INPUT_SYSTEM
    protected abstract void OnJumpPerformed(InputAction.CallbackContext obj);
#endif

    protected IEnumerator JumpBufferTimeRoutine()
    {
        yield return new WaitForSeconds(_jumpBufferTime);
        jumpBufferTimeRoutine = null;
    }

    protected void PerformJump()
    {
        OnJump?.Invoke();
        ApplyJumpForce();

        StopJumpBufferTimeRoutine();
        StopCoyoteTimeRoutine();

        if (_jumpTimeRoutine != null)
            StopCoroutine(_jumpTimeRoutine);
        _jumpTimeRoutine = StartCoroutine(JumpTimeRoutine());
    }

    protected abstract void ApplyJumpForce();

    private IEnumerator JumpTimeRoutine()
    {
        _jumpTimeElapsed = 0;
        while (_jumpTimeElapsed < _maxJumpTime)
        {
            _jumpTimeElapsed += Time.deltaTime;
            yield return null;
        }

        _jumpTimeRoutine = null;
    }

#if ENABLE_INPUT_SYSTEM
    protected abstract void OnJumpCanceled(InputAction.CallbackContext obj);
#endif

    protected void JumpKill(Action<float> onForceAdd)
    {
        if (OnGround || VelocityY <= 0 || onForceAdd == null) return;

        StartCoroutine(JumpKillRoutine());

        IEnumerator JumpKillRoutine()
        {
            yield return new WaitForSeconds((_jumpTimeElapsed < _minJumpTime ? (_minJumpTime - _jumpTimeElapsed) : 0));

            const float KILL_FACTOR = 0.85f;
            for (float t = 0; t < _jumpKillDelay; t += Time.fixedDeltaTime)
            {
                float killingForce = Mathf.Abs(VelocityY) * GetMass() * KILL_FACTOR / _jumpKillDelay;
                onForceAdd(killingForce);
                yield return null;
            }
        }
    }

    private void StopCoyoteTimeRoutine()
    {
        if (coyoteTimeRoutine == null) return;

        StopCoroutine(coyoteTimeRoutine);
        coyoteTimeRoutine = null;
    }

    protected void OnLeftGroundCoyoteTime()
    {
        if (VelocityY > 0) return;
        StopCoyoteTimeRoutine();
        coyoteTimeRoutine = StartCoroutine(CoyoteTimeRoutine());

        IEnumerator CoyoteTimeRoutine()
        {
            yield return new WaitForSeconds(_coyoteTime);
            coyoteTimeRoutine = null;
        }
    }

    private void StopJumpBufferTimeRoutine()
    {
        if (jumpBufferTimeRoutine == null) return;

        StopCoroutine(jumpBufferTimeRoutine);
        jumpBufferTimeRoutine = null;
    }

    protected void BufferedJump()
    {
        if (jumpBufferTimeRoutine == null) return;
        PerformJump();
    }

    protected void ResetRemainingJumps() => JumpsRemaining = _defaultJumpsAmount;

    protected void DecreaseRemainingJumps() => JumpsRemaining--;

    #endregion

    #region Acceleration Functions
    private IEnumerator AccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedMovementTime = 0f;
        float min = currentMovementForce;
        float max = _maxMovementForce;

        float nMin = min / max;

        while (currentMovementForce < _maxMovementForce)
        {
            _elapsedMovementTime += Time.fixedDeltaTime;
            currentMovementForce = (_accelerationCurve.Evaluate(_elapsedMovementTime / _accelerationTime) * (1 - nMin) + nMin) * max;
            yield return wait;
        }

        currentMovementForce = _maxMovementForce;
        _accelerationRoutine = null;
    }

    private IEnumerator DeccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedStopedTime = 0f;
        float max = currentMovementForce;

        while (currentMovementForce > 0f)
        {
            _elapsedStopedTime += Time.fixedDeltaTime;
            currentMovementForce = _deccelerationCurve.Evaluate(_elapsedStopedTime / _deccelerationTime) * max;
            yield return wait;
        }

        currentMovementForce = 0f;
        _deccelerationRoutine = null;
    }

    protected void BeginAcceleration()
    {
        StopAllAccelerations();

        _accelerationRoutine = StartCoroutine(AccelerationRoutine());
    }

    protected void BeginDecceleration()
    {
        StopAllAccelerations();

        _deccelerationRoutine = StartCoroutine(DeccelerationRoutine());
    }

    private void StopAllAccelerations()
    {
        if (_accelerationRoutine != null)
            StopCoroutine(_accelerationRoutine);
        if (_deccelerationRoutine != null)
            StopCoroutine(_deccelerationRoutine);
    }

    #endregion

    #region Movement Logic
    protected abstract void AssignMovementInputToCurrent();
    protected abstract void AssignMovementInputToLast();

    private void ResetExtraGravity() => _extraGravityForce = _defaultExtraGravityForce;
    private void ApplyFallGravity() => _extraGravityForce = _fallExtraGravityMultiplier * _defaultExtraGravityForce;

    public abstract void OnLandMovementBoost();

    protected void TransformationToCustomFoward() => _transformationAssignment = () => _customTransformation = new Matrix4x4(_customTransformator.right, _customTransformator.up, _customTransformator.forward, new Vector4(0, 0, 0, 1));

    protected void TransformationToWorldDefault() => _transformationAssignment = () => _customTransformation = Matrix4x4.identity;

    protected void StepCheck(Func<float> finalMovementInput)
    {
        Vector3 minStepOrigin = _minStepHeightCheck.position;
        Vector3 maxStepOrigin = minStepOrigin + jumpDirection * _maxStepHeight;

        Vector3 direction = _customTransformation.MultiplyVector(finalMovementInput() * Vector2.right).normalized;

        var hitMin = Physics2D.Raycast(minStepOrigin, direction, _maxStepDistance, _steppableLayers);
        var hitMax = Physics2D.Raycast(maxStepOrigin, direction, _maxStepDistance, _steppableLayers);
        if (VelocityY < 0 || !hitMin || hitMax) return;

        float angle = Vector3.Angle(jumpDirection, hitMin.normal);
        if (angle < _minStepAngle) return;

        Vector3 offset = -_minStepHeightCheck.localPosition;
        Vector3 tp = new Vector2(_elevateOnly ? minStepOrigin.x : hitMin.point.x, maxStepOrigin.y);
        transform.position = tp + offset;
    }

    protected void StepCheck(Func<Vector2> finalMovementInput)
    {
        Vector3 minStepOrigin = _minStepHeightCheck.position;
        Vector3 maxStepOrigin = minStepOrigin + jumpDirection * _maxStepHeight;

        Vector3 direction = _customTransformation.MultiplyVector(finalMovementInput().SwapYZ()).NoY().normalized;

        if (VelocityY < 0 || !Physics.Raycast(minStepOrigin, direction, out RaycastHit hit, _maxStepDistance, _steppableLayers) || Physics.Raycast(maxStepOrigin, direction, _maxStepDistance, _steppableLayers)) return;

        float angle = Vector3.Angle(jumpDirection, hit.normal);
        if (angle < _minStepAngle) return;

        Vector3 offset = -_minStepHeightCheck.localPosition;
        Vector3 tp = new Vector3(_elevateOnly ? minStepOrigin.x : hit.point.x, maxStepOrigin.y, _elevateOnly ? minStepOrigin.z : hit.point.z);
        transform.position = tp + offset;
    }

    #endregion

    #region Animation Logic
    public void AnimatorJumpSet() => _animator.SetTrigger(_onJumpParameter);
    public void AnimatorLandSet() => _animator.SetTrigger(_onLandParameter);
    public void AnimatorMovementSet(Vector2 movement)
    {
        _animator.SetFloat(_movementVelocityXParameter, movement.x);
        _animator.SetFloat(_movementVelocityYParameter, movement.y);
        _animator.SetFloat(_movementVelocityMagnitudeParameter, movement.sqrMagnitude);
    }

    protected void RoationSet(Vector3 movement)
    {
        const float THRESHOLD = 0.1f;
        if (movement.sqrMagnitude < THRESHOLD) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement, jumpDirection), _rotationSpeed);
    }

    #endregion   
}
