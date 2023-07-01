using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(PlatformerSetHandler))]
public class PlayerInputtedPlatformerSetHandler : MonoBehaviour
#if ENABLE_INPUT_SYSTEM
    , IJumpSetHandler, IMovementSetHandler
#endif
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private PlatformerSetHandler _platformerSetHandler;
    public IJumpInputter JumpInputter => ((IJumpSetHandler)_platformerSetHandler).JumpInputter;
    public HorizontalMovementInputter MovementInputter => ((IMovementSetHandler)_platformerSetHandler).MovementInputter;

    [SerializeField] private InputActionReference _jumpStartActionReference;
    [SerializeField] private InputActionReference _jumpCancelActionReference;
    [SerializeField] private InputActionReference _movementActionReference;

    private Func<InputAction, Vector2> _movementActionRead;

    private void Awake()
    {
        _movementActionRead = _platformerSetHandler.MovementSetHandler switch
        {
            HorizontalMovementSetHandler2D => (a) => Vector2.right * a.ReadValue<float>(),
            HorizontalMovementSetHandler3D => (a) => a.ReadValue<Vector2>(),
            _ => (a) => Vector2.right * a.ReadValue<float>(),
        };

        _jumpStartActionReference.action.performed += Jump;
        _jumpCancelActionReference.action.performed += CancelJump;

        InitialiseJumpController();
        InitialiseMovementController();
    }

    private void OnEnable()
    {
        _jumpStartActionReference.action.Enable();
        _jumpCancelActionReference.action.Enable();
        _movementActionReference.action.Enable();
    }

    private void OnDisable()
    {
        _jumpStartActionReference.action.Disable();
        _jumpCancelActionReference.action.Disable();
        _movementActionReference.action.Disable();
    }

    private void OnDestroy()
    {
        _jumpStartActionReference.action.performed -= Jump;
        _jumpCancelActionReference.action.performed -= CancelJump;
    }

    private void Update()
    {
        SetMovementInput(_movementActionRead(_movementActionReference.action));
    }

    private void FixedUpdate()
    {
        ApplyGravitationalForces();
        ApplyMovementForces();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        JumpInputter.Jump();
    }
    
    private void CancelJump(InputAction.CallbackContext obj)
    {
        JumpInputter.CancelJump();
    }

    public void ApplyMovementForces()
    {
        _platformerSetHandler.ApplyMovementForces();
    }

    public void InitialiseJumpController()
    {
        _platformerSetHandler.InitialiseJumpController();
    }

    public void InitialiseMovementController()
    {
        _platformerSetHandler.InitialiseMovementController();
    }

    public void SetMovementInput(Vector2 movementInput)
    {
        _platformerSetHandler.SetMovementInput(movementInput);
    }

    public void ApplyGravitationalForces()
    {
        ((IJumpSetHandler)_platformerSetHandler).ApplyGravitationalForces();
    }
#endif
}