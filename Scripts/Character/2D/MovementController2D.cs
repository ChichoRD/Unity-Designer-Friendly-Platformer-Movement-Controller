using ChichoExtensions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[Obsolete("Use the new InputtedMovementController2D instead")]
[RequireComponent(typeof(Rigidbody2D))]
public class MovementController2D : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM

    #region Input
    [Header("Input")]
    [SerializeField] private InputActionReference _movementAction;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;
    [SerializeField] private InputActionReference _dashDirectionAction;

    #endregion

    #region Movement
    [Header("Movement")]
    [SerializeField] private Transform _customTransformator;
    [SerializeField] private bool _useCustomTransformation = true;

    public bool UseCustomTransformation
    {
        get => _useCustomTransformation;
        set
        {
            _useCustomTransformation = value;
        }
    }

    [SerializeField][Min(0)] private float _accelerationTime = 0.2f;
    [SerializeField] private AnimationCurve _accelerationCurve;
    private Coroutine _accelerationRoutine;
    [SerializeField] private Dasher2D _dasher;
    public Dasher2D Dasher { get => _dasher; }

    [SerializeField][Min(0)] private float _deccelerationTime = 0.3f;
    [SerializeField] private AnimationCurve _deccelerationCurve;
    private Coroutine _deccelerationRoutine;
    [SerializeField][Min(0)] private float _landMovementBoost = 8f;

    [SerializeField][Min(0)] private float _maxMovementForce;
    public float MaxMovementSpeed { get => _maxMovementSpeed; set => _maxMovementSpeed = value; }
    private float _currentMovementForce;
    
    [Space]
    [SerializeField][Range(0f, 1f)] private float _counterMovementFactor = 0.05f;
    [SerializeField][Min(0)] private float _maxMovementSpeed = 12.08f;

    private Rigidbody2D _rigidbody;
    private float _elapsedMovementTime;
    private float _elapsedStopedTime;
    private float _lastMovementInput;
    private float _movementInput;
    private Matrix4x4 _customTransformation;
    private Action _transformationAssignment;

    public float MovementInput
    {
        get => _movementInput;
        private set
        {
            float valueAbs = Mathf.Abs(value);
            float movementInputAbs = Mathf.Abs(_movementInput);
            if (valueAbs > 0 && movementInputAbs <= 0)
            {
                OnMovementInputStarted?.Invoke(value);
            }

            if (movementInputAbs > 0 && valueAbs <= 0)
            {
                _lastMovementInput = _movementInput;
                OnMovementInputEnded?.Invoke(_movementInput);
            }

            _movementInput = value;
        }
    }
    private Func<float> _finalMovementInput = () => 0;

    public event Action<float> OnMovementInputStarted;
    public event Action<float> OnMovementInputEnded;

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
    [SerializeField] private float _maxJumpImpulse;
    [SerializeField][Min(0)] private float _minJumpTime;
    [SerializeField] private float _defaultExtraGravityForce;
    [SerializeField][Min(0)] private float _fallExtraGravityMultiplier = 2.5f;
    private float _extraGravityForce;
    [SerializeField][Min(0)] private float _jumpKillDelay = 0.2f;

    [Space]
    [SerializeField][Min(0)] private float _coyoteTime = 0.35f;
    private Coroutine _coyoteTimeRoutine;
    [SerializeField][Min(0)] private float _jumpBufferTime = 0.35f;
    private Coroutine _jumpBufferTimeRoutine;

    [Space]
    [SerializeField] private bool _adjustAllJumpParameters = true;
    [SerializeField][Min(0)] private float _maxJumpHeight = 8f;
    [SerializeField][Min(0)] private float _maxJumpTime = 1f;
    [SerializeField][Min(0)] private float _minJumpHeight = 2f;
    [SerializeField][Min(0)] private float _minDescentTime = 1f;

    private float _jumpTimeElapsed;
    private Coroutine _jumpTimeRoutine;
    private Vector3 _jumpDirection = Vector3.up;
    private Vector3 _groundDirection = Vector3.down;
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
    private Animator _animator;
    private Action<Vector2> _movementAnimationUpdate;

    #endregion

    #region Events Initialization
    private void Awake()
    {
        //Componets Cache in
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        var temp = _dasher;
        _dasher = new Dasher2D(temp, _rigidbody);

        //Movement Input reading source
        OnMovementInputStarted += AssignMovementInputToCurrent;
        OnMovementInputEnded += AssignMovementInputToLast;

        //Accelerations
        OnMovementInputStarted += BeginAcceleration;
        OnMovementInputEnded += BeginDecceleration;

        //Jump Inputs
        if (_jumpAction != null)
        {
            _jumpAction.action.performed += OnJumpPerformed;
            _jumpAction.action.canceled += OnJumpCanceled;

            ResetRemainingJumps();
            OnLanded.AddListener(ResetRemainingJumps);
            OnJump += DecreaseRemainingJumps;
        }

        //Dash Inputs
        if (_dashAction != null)
        {
            _dashAction.action.performed += OnDashPerformed;
        }
        
        //Extra Fall Gravity
        OnDescentStarted += ApplyFallGravity;
        OnLanded.AddListener(ResetExtraGravity);
        
        //Coyote Time
        OnLeftGround.AddListener(OnLeftGroundCoyoteTime);

        //Jump Buffer
        OnLanded.AddListener(BufferedJump);
        
        //Animation Events Triggering Sources
        AnimationEvents();

        //First Person and Custom Transformation
        if (UseCustomTransformation)
        {
            TransformationToCustomFoward();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }
        
        TransformationToWorldDefault();

        void AnimationEvents()
        {
            if (_animator != null)
            {
                OnLanded.AddListener(AnimatorLandSet);
                OnLeftGround.AddListener(AnimatorJumpSet);
                _movementAnimationUpdate = AnimatorMovementSet;

                return;
            }

            _movementAnimationUpdate = _ => { };
        }
    }

    private void OnEnable()
    {
        EnableAllMovement();
    }

    #endregion

    #region Events Finalization
    private void OnDisable()
    {
        DisableAllMovement();
    }
    
    private void OnDestroy()
    {
        OnMovementInputStarted -= AssignMovementInputToCurrent;
        OnMovementInputEnded -= AssignMovementInputToLast;

        OnMovementInputStarted -= BeginAcceleration;
        OnMovementInputEnded -= BeginDecceleration;

        if (_jumpAction != null)
        {
            _jumpAction.action.performed -= OnJumpPerformed;
            _jumpAction.action.canceled -= OnJumpCanceled;

            OnLanded.RemoveListener(ResetRemainingJumps);
            OnJump -= DecreaseRemainingJumps;
        }

        if (_dashAction != null)
        {
            _dashAction.action.performed -= OnDashPerformed;
        }

        OnDescentStarted -= ApplyFallGravity;
        OnLanded.RemoveListener(ResetExtraGravity);

        OnLeftGround.RemoveListener(OnLeftGroundCoyoteTime);
        OnLanded.RemoveListener(BufferedJump);

        if (_animator != null)
        {
            OnLanded.RemoveListener(AnimatorLandSet);
            OnLeftGround.RemoveListener(AnimatorJumpSet);
        }
    }

    #endregion

    #region Input Actions Enabling/Disabling
    private void EnableAllMovement()
    {
        EnablePlaneMovement();
        EnableJumpAction();
        EnableDashAction();
    }

    private void EnablePlaneMovement()
    {
        _movementAction.action.Enable();
    }

    public void EnableDashAction()
    {
        _dashAction.action.Enable();
    }

    public void EnableJumpAction()
    {
        _jumpAction.action.Enable();
    }

    private void DisableAllMovement()
    {
        DisablePlaneMovement();
        DisableJumpAction();
        DisableDashAction();
    }

    private void DisablePlaneMovement()
    {
        _movementAction.action.Disable();
    }

    public void DisableJumpAction()
    {
        _jumpAction.action.Disable();
    }

    public void DisableDashAction()
    {
        _dashAction.action.Disable();
    }

    #endregion

    #region Update Loops
    private void Update()
    {
        _transformationAssignment.Invoke();

        MovementInput = _movementAction.action.ReadValue<float>();
        VelocityY = _rigidbody.velocity.y;

        GroundCheck();

        StepCheck();

        void GroundCheck()
        {
            if (_groundCheckCenter == null || _raycastDensity == 0) return;

            float delta = 1 / _raycastDensity;

            Vector2 averageNormal = Vector3.zero;
            for (float i = 0; i <= _groundChecksExtents.x; i += delta)
            {
                for (float j = 0; j <= _groundChecksExtents.y; j += delta)
                {
                    Vector3 origin = _groundCheckCenter.position + new Vector3(i, 0, j) - (_groundChecksExtents / 2).SwapYZ();

                    var hit = Physics2D.Raycast(origin, _groundDirection, _raycastsLength, _jumpableLayers);
                    if (!hit) continue;

                    averageNormal += hit.normal;
                }
            }
            
            averageNormal.Normalize();

            float angle = Vector3.Angle(_jumpDirection, averageNormal);
            OnGround = averageNormal != Vector2.zero && angle < _maxJumpAngle && VelocityY <= 0;
        }
    }

    private void FixedUpdate()
    {
        Vector3 countermovementForce = (-_rigidbody.velocity.NoY() * _rigidbody.mass / Time.fixedDeltaTime) * _counterMovementFactor;
        _rigidbody.AddForce(countermovementForce, ForceMode2D.Force);

        Vector2 rawMovement = _currentMovementForce * _finalMovementInput() * Vector2.right / _maxMovementForce;
        _movementAnimationUpdate.Invoke(rawMovement);

        Vector3 movement = _customTransformation.MultiplyVector(_currentMovementForce * _finalMovementInput() * Vector2.right);
        _rigidbody.AddForce(movement, ForceMode2D.Force);

        Vector3 extraGravityForce = -_extraGravityForce * _groundDirection;
        _rigidbody.AddForce(extraGravityForce, ForceMode2D.Force);

        RoationSet(movement);
    }

    #endregion

    #region Visual Debugging/Parameters Setting
    private void OnValidate()
    {
        AdjustJumpParameters();

        void AdjustJumpParameters()
        {
            if (!_adjustAllJumpParameters) return;

            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();

            //v = 2 * h / t
            //g = -2 * h / t^2

            float maxJumpVelocity = 2 * _maxJumpHeight / _maxJumpTime;
            _maxJumpImpulse = maxJumpVelocity * _rigidbody.mass;

            float desiredExtraGravity = (-2 * _maxJumpHeight / Mathf.Pow(_maxJumpTime, 2)) - Physics2D.gravity.y;
            _defaultExtraGravityForce = desiredExtraGravity * _rigidbody.mass;

            float minJumpTime = Mathf.Max((2 * _minJumpHeight) / maxJumpVelocity - _jumpKillDelay, 0f);
            _minJumpTime = minJumpTime;

            //(v - dv) * r = f * dt / m
            //(v - dv) * r = dv
            //(v * r - dv * r) = dv
            //v * r = dv * (r + 1)
            //(v * r) / (r + 1) = dv
            //(v * r * m) / ((r + 1) * dt) = f
            float maxMovementForce = (_maxMovementSpeed * _counterMovementFactor * _rigidbody.mass) / (Time.fixedDeltaTime);
            _maxMovementForce = maxMovementForce;
            
            float extraFallGravity = (-2 * _maxJumpHeight / Mathf.Pow(_minDescentTime, 2)) - Physics2D.gravity.y;
            float extraFallGravityForce = extraFallGravity * _rigidbody.mass;
            _fallExtraGravityMultiplier = extraFallGravityForce / _defaultExtraGravityForce;
        }
    }

    private void OnDrawGizmosSelected()
    {
        DebugGroundChecks();

        DrawJumpDistances();

        DrawStepChecks();

        DrawMaximumJumpArc(_maxJumpImpulse);

        void DebugGroundChecks()
        {
            if (_groundCheckCenter == null || _raycastDensity == 0) return;

            float delta = 1 / _raycastDensity;

            for (float i = 0; i <= _groundChecksExtents.x; i += delta)
            {
                for (float j = 0; j <= _groundChecksExtents.y; j += delta)
                {
                    Vector3 origin = _groundCheckCenter.position + new Vector3(i, 0, j) - (_groundChecksExtents / 2).SwapYZ();
                    Debug.DrawLine(origin, origin + _groundDirection * _raycastsLength, Color.red);
                }
            }
        }

        void DrawJumpDistances()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _jumpDirection * _maxJumpHeight);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + _jumpDirection * _minJumpHeight);
        }

        void DrawStepChecks()
        {
            if (_minStepHeightCheck == null || _customTransformation == null) return;

            Vector3 minStepOrigin = _minStepHeightCheck.position;
            Vector3 maxStepOrigin = minStepOrigin + _jumpDirection * _maxStepHeight;

            Vector3 size = new Vector3(_maxStepDistance * 2, Time.fixedDeltaTime, _maxStepDistance * 2);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(minStepOrigin, size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(maxStepOrigin, size);
        }

        void DrawMaximumJumpArc(float impulse)
        {
            float jumpVelocity = impulse / _rigidbody.mass;
            float addedAcceleration = Physics2D.gravity.y + _defaultExtraGravityForce / _rigidbody.mass;
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

            addedAcceleration = Physics2D.gravity.y + _defaultExtraGravityForce * _fallExtraGravityMultiplier / _rigidbody.mass;
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

    #endregion

    #region Jump Logic
    private void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        if (!obj.ReadValueAsButton()) return;
        
        if (JumpsRemaining <= 0 && _coyoteTimeRoutine == null)
        {
            _jumpBufferTimeRoutine ??= StartCoroutine(JumpBufferTimeRoutine());
            return;
        }

        PerformJump();
    }

    private IEnumerator JumpBufferTimeRoutine()
    {
        yield return new WaitForSeconds(_jumpBufferTime);
        _jumpBufferTimeRoutine = null;
    }

    private void PerformJump()
    {
        //Debug.Log("Invoker is " + invoker);
        OnJump?.Invoke();
        _rigidbody.AddForce(_jumpDirection * _maxJumpImpulse, ForceMode2D.Impulse);

        StopJumpBufferTimeRoutine();
        StopCoyoteTimeRoutine();

        if (_jumpTimeRoutine != null)
            StopCoroutine(_jumpTimeRoutine);
        _jumpTimeRoutine = StartCoroutine(JumpTimeRoutine());
    }

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

    private void OnJumpCanceled(InputAction.CallbackContext obj)
    {
        //StopJumpBufferTimeRoutine();
        JumpKill();
    }

    private void JumpKill()
    {
        if (OnGround || VelocityY <= 0) return;
        
        StartCoroutine(JumpKillRoutine());

        IEnumerator JumpKillRoutine()
        {
            yield return new WaitForSeconds((_jumpTimeElapsed < _minJumpTime ? (_minJumpTime - _jumpTimeElapsed) : 0));

            const float KILL_FACTOR = 0.85f;
            for (float t = 0; t < _jumpKillDelay; t += Time.fixedDeltaTime)
            {
                float killingForce = Mathf.Abs(VelocityY) * _rigidbody.mass * KILL_FACTOR / _jumpKillDelay;
                _rigidbody.AddForce(_groundDirection * killingForce, ForceMode2D.Force);
                yield return null;
            }
        }
    }

    private void StopCoyoteTimeRoutine()
    {
        if (_coyoteTimeRoutine == null) return;

        StopCoroutine(_coyoteTimeRoutine);
        _coyoteTimeRoutine = null;
    }

    private void OnLeftGroundCoyoteTime()
    {
        if (VelocityY > 0) return;
        StopCoyoteTimeRoutine();
        _coyoteTimeRoutine = StartCoroutine(CoyoteTimeRoutine());

        IEnumerator CoyoteTimeRoutine()
        {
            yield return new WaitForSeconds(_coyoteTime);
            _coyoteTimeRoutine = null;
        }
    }

    private void StopJumpBufferTimeRoutine()
    {
        if (_jumpBufferTimeRoutine == null) return;

        StopCoroutine(_jumpBufferTimeRoutine);
        _jumpBufferTimeRoutine = null;
    }

    private void BufferedJump()
    {
        if (_jumpBufferTimeRoutine == null) return;
        PerformJump();
    }

    private void ResetRemainingJumps() => JumpsRemaining = _defaultJumpsAmount;

    private void DecreaseRemainingJumps() => JumpsRemaining--;

    #endregion

    #region Accelerations Logic
    private IEnumerator AccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedMovementTime = 0f;
        float min = _currentMovementForce;
        float max = _maxMovementForce;

        float nMin = min / max;

        while (_currentMovementForce < _maxMovementForce)
        {
            _elapsedMovementTime += Time.fixedDeltaTime;
            _currentMovementForce = (_accelerationCurve.Evaluate(_elapsedMovementTime / _accelerationTime) * (1 - nMin) + nMin) * max;
            yield return wait;
        }

        _currentMovementForce = _maxMovementForce;
        _accelerationRoutine = null;
    }

    private IEnumerator DeccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedStopedTime = 0f;
        float max = _currentMovementForce;
        
        while (_currentMovementForce > 0f)
        {
            _elapsedStopedTime += Time.fixedDeltaTime;
            _currentMovementForce = _deccelerationCurve.Evaluate(_elapsedStopedTime / _deccelerationTime) * max;
            yield return wait;
        }

        _currentMovementForce = 0f;
        _deccelerationRoutine = null;
    }
        
    private void BeginAcceleration(float _)
    {
        StopAllAccelerations();

        _accelerationRoutine = StartCoroutine(AccelerationRoutine());
    }

    private void BeginDecceleration(float _)
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
    private void AssignMovementInputToCurrent(float _) => _finalMovementInput = () => MovementInput;
    private void AssignMovementInputToLast(float _) => _finalMovementInput = () => _lastMovementInput;

    private void ResetExtraGravity() => _extraGravityForce = _defaultExtraGravityForce;
    private void ApplyFallGravity() => _extraGravityForce = _fallExtraGravityMultiplier * _defaultExtraGravityForce;

    public void OnLandMovementBoost()
    {
        _rigidbody.AddForce(_customTransformation.MultiplyVector(_landMovementBoost * _currentMovementForce * Time.fixedDeltaTime * _finalMovementInput() * Vector2.right), ForceMode2D.Impulse);
    }

    private void OnDashPerformed(InputAction.CallbackContext obj)
    {
        Vector2 input = _dashDirectionAction == null ? _finalMovementInput() * Vector2.right : _dashDirectionAction.action.ReadValue<Vector2>();
        _dasher.Dash(_customTransformation.MultiplyVector(input));
    }

    private void TransformationToCustomFoward() => _transformationAssignment = () => _customTransformation = new Matrix4x4(_customTransformator.right, _customTransformator.up, _customTransformator.forward, new Vector4(0, 0, 0, 1));

    private void TransformationToWorldDefault() => _transformationAssignment = () => _customTransformation = Matrix4x4.identity;

    private void StepCheck()
    {
        Vector3 minStepOrigin = _minStepHeightCheck.position;
        Vector3 maxStepOrigin = minStepOrigin + _jumpDirection * _maxStepHeight;

        Vector3 direction = _customTransformation.MultiplyVector(_finalMovementInput() * Vector2.right).normalized;

        var hitMin = Physics2D.Raycast(minStepOrigin, direction, _maxStepDistance, _steppableLayers);
        var hitMax = Physics2D.Raycast(maxStepOrigin, direction, _maxStepDistance, _steppableLayers);
        if (VelocityY < 0 || !hitMin || hitMax) return;

        float angle = Vector3.Angle(_jumpDirection, hitMin.normal);
        if (angle < _minStepAngle) return;

        Vector3 offset = -_minStepHeightCheck.localPosition;
        Vector3 tp = new Vector2(_elevateOnly ? minStepOrigin.x : hitMin.point.x, maxStepOrigin.y);
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

    private void RoationSet(Vector3 movement)
    {
        const float THRESHOLD = 0.1f;
        if (movement.sqrMagnitude < THRESHOLD) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement, _jumpDirection), _rotationSpeed);
    }

    #endregion

#endif

}
