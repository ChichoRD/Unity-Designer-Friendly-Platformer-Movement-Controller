using ChichoExtensions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MovementController3D : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _movementAction;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;

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
    [SerializeField] private Dasher3D _dasher;

    [SerializeField][Min(0)] private float _maxMovementSpeed = 35f;
    private float _movementSpeed;
    [SerializeField][Range(0f, 1f)] private float _counterMovementFactor = 0.05f;

    [SerializeField][Min(0)] private float _deccelerationTime = 0.3f;
    [SerializeField] private AnimationCurve _deccelerationCurve;
    private Coroutine _deccelerationRoutine;
    [SerializeField][Min(0)] private float _landMovementBoost = 8f;

    private Rigidbody _rigidbody;
    private float _elapsedMovementTime;
    private float _elapsedStopedTime;
    private Vector2 _lastMovementInput;
    private Vector2 _movementInput;
    private Matrix4x4 _cameraTransformation;
    private Action _transformationAssignment;

    public Vector2 MovementInput
    {
        get => _movementInput;
        private set
        {
            if (value.sqrMagnitude > 0 && _movementInput.sqrMagnitude <= 0)
            {
                OnMovementStarted?.Invoke(value);
            }

            if (_movementInput.sqrMagnitude > 0 && value.sqrMagnitude <= 0)
            {
                _lastMovementInput = _movementInput;
                OnMovementEnded?.Invoke(_movementInput);
            }

            _movementInput = value;
        }
    }
    private Func<Vector2> _finalMovementInput = () => Vector2.zero;

    public event Action<Vector2> OnMovementStarted;
    public event Action<Vector2> OnMovementEnded;

    [Header("Jump Checks")]
    [SerializeField] private Vector2 _groundChecksExtents = Vector2.one;
    [SerializeField][Min(10e-4f)] private float _raycastDensity = 3f;
    [SerializeField][Min(0)] private float _raycastsLength = 0.3f;
    [SerializeField] private LayerMask _jumpableLayers;
    [SerializeField] private Transform _groundCheckCenter;

    [Space]
    [SerializeField] private Transform _minStepHeightCheck;
    [SerializeField][Min(0)] private float _maxStepHeight = 0.6f;
    [SerializeField][Min(0)] private float _maxStepDistance = 0.5f;
    [SerializeField] private LayerMask _steppableLayers;
    [SerializeField] private bool _elevateOnly = false;

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

    [Space]
#if UNITY_EDITOR
    [SerializeField] private bool _adjustAllJumpParameters = true;
    [SerializeField][Min(0)] private float _maxJumpHeight = 8f;
    [SerializeField][Min(0)] private float _maxJumpTime = 1f;
    [SerializeField][Min(0)] private float _minJumpHeight = 2f;
#endif

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


    [Header("Animation")]
    [SerializeField][Range(0f, 1f)] private float _rotationSpeed = 0.1f;
    [SerializeField] private string _movementVelocityYParameter = "MovementVelocityY";
    [SerializeField] private string _movementVelocityXParameter = "MovementVelocityX";
    [SerializeField] private string _movementVelocityMagnitudeParameter = "MovementSpeed";
    [SerializeField] private string _onJumpParameter = "OnJump";
    [SerializeField] private string _onLandParameter = "OnLand";
    private Animator _animator;
    private Action<Vector2> _movementAnimationUpdate;
        
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();

        var temp = _dasher;
        _dasher = new Dasher3D(temp, _rigidbody);

        OnMovementStarted += AssignMovementInputToCurrent;
        OnMovementEnded += AssignMovementInputToLast;

        OnMovementStarted += BeginAcceleration;
        OnMovementEnded += BeginDecceleration;

        if (_jumpAction != null)
        {
            _jumpAction.action.performed += OnJumpPerformed;
            _jumpAction.action.canceled += OnJumpCanceled;
        }

        if (_dashAction != null)
        {
            _dashAction.action.performed += OnDashPerformed;
        }

        OnDescentStarted += ApplyFallGravity;

        OnLanded.AddListener(ResetExtraGravity);
        OnLeftGround.AddListener(OnLeftGroundCoyoteTime);

        AnimationEvents();

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
        _movementAction.action.Enable();
        _jumpAction.action.Enable();
        _dashAction.action.Enable();
    }

    private void OnDisable()
    {
        _movementAction.action.Disable();
        _jumpAction.action.Disable();
        _dashAction.action.Disable();
    }

    private void OnDestroy()
    {
        OnMovementStarted -= AssignMovementInputToCurrent;
        OnMovementEnded -= AssignMovementInputToLast;

        OnMovementStarted -= BeginAcceleration;
        OnMovementEnded -= BeginDecceleration;

        if (_jumpAction != null)
        {
            _jumpAction.action.performed -= OnJumpPerformed;
            _jumpAction.action.canceled -= OnJumpCanceled;
        }

        if (_dashAction != null)
        {
            _dashAction.action.performed -= OnDashPerformed;
        }
        
        OnDescentStarted -= ApplyFallGravity;
        
        OnLanded.RemoveListener(ResetExtraGravity);
        OnLeftGround.RemoveListener(OnLeftGroundCoyoteTime);

        if (_animator != null)
        {
            OnLanded.RemoveListener(AnimatorLandSet);
            OnLeftGround.RemoveListener(AnimatorJumpSet);
        }
    }

    private void Update()
    {
        _transformationAssignment.Invoke();

        MovementInput = _movementAction.action.ReadValue<Vector2>();
        VelocityY = _rigidbody.velocity.y;

        GroundCheck();

        StepCheck();

        void GroundCheck()
        {
            if (_groundCheckCenter == null || _raycastDensity == 0) return;

            float delta = 1 / _raycastDensity;

            for (float i = 0; i <= _groundChecksExtents.x; i += delta)
            {
                for (float j = 0; j <= _groundChecksExtents.y; j += delta)
                {
                    Vector3 origin = _groundCheckCenter.position + new Vector3(i, 0, j) - (_groundChecksExtents / 2).SwapYZ();

                    if (Physics.Raycast(origin, _groundDirection, _raycastsLength, _jumpableLayers))
                    {
                        OnGround = true;
                        return;
                    }
                }
            }

            OnGround = false;
        }
    }

    private void FixedUpdate()
    {
        Vector3 countermovementForce = (-_rigidbody.velocity.NoY() * _rigidbody.mass / Time.fixedDeltaTime) * _counterMovementFactor;
        _rigidbody.AddForce(countermovementForce, ForceMode.Force);

        Vector2 rawMovement = _finalMovementInput() * _movementSpeed / _maxMovementSpeed;
        _movementAnimationUpdate.Invoke(rawMovement);

        Vector3 movement = _cameraTransformation.MultiplyVector((_finalMovementInput()).SwapYZ()).NoY().normalized * _movementSpeed;
        _rigidbody.AddForce(movement, ForceMode.Force);

        Vector3 extraGravityForce = -_extraGravityForce * _groundDirection;
        _rigidbody.AddForce(extraGravityForce, ForceMode.Force);

        RoationSet(movement);
    }

    private void OnValidate()
    {
        AdjustJumpParameters();

        void AdjustJumpParameters()
        {
            if (!_adjustAllJumpParameters) return;

            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();

            //v = 2 * h / t
            //g = -2 * h / t^2
            //h = v * t + g * t^2 / 2
            //t = (-v - sqrt(v^2 - 2 * g * h))/g

            float maxJumpVelocity = 2 * _maxJumpHeight / _maxJumpTime;
            _maxJumpImpulse = maxJumpVelocity * _rigidbody.mass;

            float desiredExtraGravity = (-2 * _maxJumpHeight / Mathf.Pow(_maxJumpTime, 2)) - Physics.gravity.y;
            _defaultExtraGravityForce = desiredExtraGravity * _rigidbody.mass;

            float minJumpTime = (2 * _minJumpHeight) / maxJumpVelocity;
            _minJumpTime = minJumpTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        DebugGroundChecks();

        DrawJumpDistances();

        DrawStepChecks();

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
            if (_minStepHeightCheck == null || _cameraTransformation == null) return;
            
            Vector3 minStepOrigin = _minStepHeightCheck.position;
            Vector3 maxStepOrigin = minStepOrigin + _jumpDirection * _maxStepHeight;

            Vector3 size = new Vector3(_maxStepDistance * 2, Time.fixedDeltaTime, _maxStepDistance * 2);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(minStepOrigin, size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(maxStepOrigin, size);
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        if (!OnGround && _coyoteTimeRoutine == null) return;

        PerformJump();
    }

    private void PerformJump()
    {
        _rigidbody.AddForce(_jumpDirection * _maxJumpImpulse, ForceMode.Impulse);

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
        JumpKill();
    }

    private void JumpKill()
    {
        StartCoroutine(JumpKillRoutine());

        IEnumerator JumpKillRoutine()
        {
            yield return new WaitForSeconds((_jumpTimeElapsed < _minJumpTime ? (_minJumpTime - _jumpTimeElapsed) : 0));

            const float KILL_FACTOR = 0.85f;
            for (float t = 0; t < _jumpKillDelay; t += Time.fixedDeltaTime)
            {
                float killingForce = Mathf.Abs(VelocityY) * _rigidbody.mass * KILL_FACTOR / _jumpKillDelay;
                _rigidbody.AddForce(_groundDirection * killingForce, ForceMode.Force);
            }
        }
    }

    private IEnumerator AccelerationRoutine()
    {
        //_movementSpeed = 0f;
        _elapsedMovementTime = 0f;
        while (_movementSpeed < _maxMovementSpeed)
        {
            _elapsedMovementTime += Time.fixedDeltaTime;
            _movementSpeed = _accelerationCurve.Evaluate(_elapsedMovementTime / _accelerationTime) * _maxMovementSpeed;
            yield return null;
        }

        _movementSpeed = _maxMovementSpeed;
        _accelerationRoutine = null;
    }

    private IEnumerator DeccelerationRoutine()
    {
        //_movementSpeed = _maxMovementSpeed;
        _elapsedStopedTime = 0f;
        while (_movementSpeed > 0f)
        {
            _elapsedStopedTime += Time.fixedDeltaTime;
            _movementSpeed = _deccelerationCurve.Evaluate(_elapsedStopedTime / _deccelerationTime) * _maxMovementSpeed;
            yield return null;
        }

        _movementSpeed = 0f;
        _deccelerationRoutine = null;
    }

    private void BeginAcceleration(Vector2 _)
    {
        StopAllAccelerations();

        _accelerationRoutine = StartCoroutine(AccelerationRoutine());
    }

    private void BeginDecceleration(Vector2 _)
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

    private void AssignMovementInputToCurrent(Vector2 _) => _finalMovementInput = () => MovementInput;
    private void AssignMovementInputToLast(Vector2 _) => _finalMovementInput = () => _lastMovementInput;

    private void ResetExtraGravity() => _extraGravityForce = _defaultExtraGravityForce;
    private void ApplyFallGravity() => _extraGravityForce = _fallExtraGravityMultiplier * _defaultExtraGravityForce;

    public void OnLandMovementBoost()
    {
        _rigidbody.AddForce(_cameraTransformation.MultiplyVector((_landMovementBoost * _movementSpeed * Time.fixedDeltaTime * _finalMovementInput()).SwapYZ()), ForceMode.Impulse);
    }

    private void OnDashPerformed(InputAction.CallbackContext obj)
    {
        _dasher.Dash(_cameraTransformation.MultiplyVector(_finalMovementInput().SwapYZ()).NoY());
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

    private void TransformationToCustomFoward() => _transformationAssignment = () => _cameraTransformation = new Matrix4x4(_customTransformator.right, _customTransformator.up, _customTransformator.forward, new Vector4(0, 0, 0, 1));

    private void TransformationToWorldDefault() => _transformationAssignment = () => _cameraTransformation = Matrix4x4.identity;

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

    private void StepCheck()
    {
        Vector3 minStepOrigin = _minStepHeightCheck.position;
        Vector3 maxStepOrigin = minStepOrigin + _jumpDirection * _maxStepHeight;

        Vector3 direction = _cameraTransformation.MultiplyVector((_finalMovementInput()).SwapYZ()).NoY().normalized;

        if (!Physics.Raycast(minStepOrigin, direction, out RaycastHit hit, _maxStepDistance, _steppableLayers) || Physics.Raycast(maxStepOrigin, direction, _maxStepDistance, _steppableLayers) || VelocityY < 0) return;

        Vector3 offset = -_minStepHeightCheck.localPosition;
        Vector3 tp = new Vector3(_elevateOnly ? minStepOrigin.x : hit.point.x, maxStepOrigin.y, _elevateOnly ? minStepOrigin.z : hit.point.z);
        transform.position = tp + offset;
    }

}
