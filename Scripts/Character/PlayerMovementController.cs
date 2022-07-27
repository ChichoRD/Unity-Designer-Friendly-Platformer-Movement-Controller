using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlayerMovementController<T> : MovementController<T> where T : struct
{
    #region Input
    [Header("Input")]
    [SerializeField] protected InputActionReference movementAction;
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;
    [SerializeField] protected InputActionReference dashDirectionAction;

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

            ResetRemainingJumps();
            OnLanded.AddListener(ResetRemainingJumps);
            OnJump += DecreaseRemainingJumps;
        }

        //Dash Inputs
        if (_dashAction != null)
        {
            _dashAction.action.performed += OnDashPerformed;
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

            OnLanded.RemoveListener(ResetRemainingJumps);
            OnJump -= DecreaseRemainingJumps;
        }

        if (_dashAction != null)
        {
            _dashAction.action.performed -= OnDashPerformed;
        }
    }

    #endregion

    #region Input Actions Enabling/Disabling
    private void EnableAllMovement()
    {
        EnablePlaneMovement();
        EnableJumpAction();
        EnableDashAction();
        EnableDashDirectionAction();
    }

    private void EnablePlaneMovement()
    {
        movementAction.action.Enable();
    }

    public void EnableDashAction()
    {
        _dashAction.action.Enable();
    }

    public void EnableJumpAction()
    {
        _jumpAction.action.Enable();
    }

    private void EnableDashDirectionAction()
    {
        dashDirectionAction.action.Enable();
    }

    private void DisableAllMovement()
    {
        DisablePlaneMovement();
        DisableJumpAction();
        DisableDashAction();
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

    public void DisableDashAction()
    {
        _dashAction.action.Disable();
    }

    private void DisableDashDirectionAction()
    {
        dashDirectionAction.action.Disable();
    }

    #endregion
}
