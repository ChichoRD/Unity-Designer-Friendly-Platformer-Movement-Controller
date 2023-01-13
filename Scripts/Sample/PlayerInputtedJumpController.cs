using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInputtedJumpController : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Rigidbody2D _rigidbody2D;

    [RequireInterface(typeof(IJumpInputter))]
    [SerializeField] private Object _jumpInputter;
    private IJumpInputter JumpInputter => _jumpInputter as IJumpInputter;

    [SerializeField] private Object[] _jumpUpdateableConstrainerObjects;
    private IUpdateableJumpConstrainer[] JumpConstrainers => _jumpUpdateableConstrainerObjects.Select(x => x as IUpdateableJumpConstrainer).ToArray();

    [SerializeField] private HorizontalMovementInputter _horizontalMovementInputter;
    [SerializeField] private InputActionReference _jumpActionReference;
    [SerializeField] private InputActionReference _jumpCancelActionReference;
    [SerializeField] private InputActionReference _movementActionReference;

    private void Awake()
    {
        InitialiseJumpController();
        InitialiseMovementController();

        _jumpActionReference.action.performed += Jump;
        _jumpCancelActionReference.action.performed += CancelJump;
    }


    private void OnEnable()
    {
        _jumpActionReference.action.Enable();
        _jumpCancelActionReference.action.Enable();
        _movementActionReference.action.Enable();
    }

    private void OnDisable()
    {
        _jumpActionReference.action.Disable();
        _jumpCancelActionReference.action.Disable();
        _movementActionReference.action.Disable();
    }

    private void OnDestroy()
    {
        _jumpActionReference.action.performed -= Jump;
        _jumpCancelActionReference.action.performed -= CancelJump;
    }

    private void Update()
    {
        foreach (var updateable in JumpConstrainers)
            updateable.CheckStateAndUpdate();

        JumpInputter.JumpController.UpdateInfo();

        _horizontalMovementInputter.MovementInput = Vector2.right * _movementActionReference.action.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        _rigidbody2D.AddForce(JumpInputter.JumpController.GetGravityDifference(Physics2D.gravity) * _rigidbody2D.mass);
        
        _rigidbody2D.AddForce(_horizontalMovementInputter.MovementController.GetCounterMovementForce() * Vector2.Dot(_rigidbody2D.velocity.normalized, Vector2.right) * Vector2.right);
        _rigidbody2D.AddForce(_horizontalMovementInputter.GetMovementForceDueToInput());
    }

    private void CancelJump(InputAction.CallbackContext obj)
    {
        JumpInputter.CancelJump();
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        JumpInputter.Jump();
    }

    private void InitialiseJumpController()
    {
        if (_rigidbody2D == null) return;
        JumpInputter.Initialise(() => Vector3.up,
                                () => Vector3.down,
                                () => _rigidbody2D.velocity,
                                () => _rigidbody2D.mass,
                                (f) => _rigidbody2D.AddForce(f, ForceMode2D.Force));
    }

    private void InitialiseMovementController()
    {
        if (_rigidbody2D == null) return;
        _horizontalMovementInputter.MovementController.Initialise(() => Mathf.Abs(_rigidbody2D.velocity.x),
                                                                  () => _rigidbody2D.mass);
    }

    private void OnValidate()
    {
        InitialiseJumpController();
    }

    private void OnDrawGizmosSelected()
    {
        JumpInputter.JumpController.DebugJumpArcs(Color.green, Color.blue, () => Vector3.right * _horizontalMovementInputter.MovementController.MaxMovementSpeed);
    }
#endif
}
