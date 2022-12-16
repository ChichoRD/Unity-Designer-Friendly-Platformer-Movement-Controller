using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public abstract class InputtedMovementController<T> : MovementController<T> where T : struct
{
#if ENABLE_INPUT_SYSTEM

    #region Input

    [Header("Input")]
    [SerializeField] protected InputActionReference movementAction;
    [SerializeField] private InputActionReference _jumpAction;

    #endregion

    #region Initialization
    protected override void Awake()
    {
        base.Awake();

        //Jump Inputs
        if (_jumpAction != null)
        {
            _jumpAction.action.performed += OnJumpPerformed;
            _jumpAction.action.canceled += OnJumpCanceled;
        }
    }

    protected virtual void OnEnable()
    {
        EnableAllMovement();
    }

    #endregion

    #region Finalization

    protected virtual void OnDisable()
    {
        DisableAllMovement();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_jumpAction != null)
        {
            _jumpAction.action.performed -= OnJumpPerformed;
            _jumpAction.action.canceled -= OnJumpCanceled;
        }
    }

    #endregion

    #region Input Actions Enabling/Disabling
    private void EnableAllMovement()
    {
        EnablePlaneMovement();
        EnableJumpAction();
        EnableDashDirectionAction();
    }

    
    private void EnablePlaneMovement()
    {
        movementAction.action.Enable();
    }

    public void EnableJumpAction()
    {
        _jumpAction.action.Enable();
    }

    private void DisableAllMovement()
    {
        DisablePlaneMovement();
        DisableJumpAction();
        DisableDashDirectionAction();
    }

    private void DisablePlaneMovement()
    {
        movementAction.action.Disable();
    }

    public void DisableJumpAction()
    {
        _jumpAction.action.Disable();
    }

    #endregion
    
#endif
}
