using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class HorizontalMovementInputter : MonoBehaviour
{
    [SerializeField] private float _inputThreshold;
    [SerializeField] private HorizontalMovementController _movementController;

    private bool _activeInputting;
    private Vector2 _lastActiveInput;
    private Vector2 _movementInput;
    public Vector2 MovementInput
    {
        get => _movementInput;
        set
        {
            if (HasInputAppeared(_movementInput, value))
                OnMovementInputAppeared?.Invoke();

            if (HasInputVanished(_movementInput, value))
                OnMovementInputVanished?.Invoke();

            _movementInput = value;
        }
    }

    [field: SerializeField] public UnityEvent OnMovementInputAppeared { get; private set; }
    [field: SerializeField] public UnityEvent OnMovementInputVanished { get; private set; }

    protected float InputThreshold { get => _inputThreshold; }
    public HorizontalMovementController MovementController { get => _movementController; }

    protected abstract bool HasInputAppeared(Vector2 oldInput, Vector2 newInput);
    protected abstract bool HasInputVanished(Vector2 oldInput, Vector2 newInput);

    protected virtual void Awake()
    {
        OnMovementInputAppeared.AddListener(_movementController.BeginAcceleration);
        OnMovementInputAppeared.AddListener(ActiveInputtingStart);

        OnMovementInputVanished.AddListener(_movementController.BeginDecceleration);
        OnMovementInputVanished.AddListener(CacheLastActiveInput);
        OnMovementInputVanished.AddListener(ActiveInputtingStop);
    }

    private void CacheLastActiveInput()
    {
        _lastActiveInput = _movementInput;
    }

    private void ActiveInputtingStart()
    {
        _activeInputting = true;
    }

    private void ActiveInputtingStop()
    {
        _activeInputting = false;
    }

    public Vector2 GetCurrentMovementInput()
    {
        return _activeInputting ? MovementInput : _lastActiveInput;
    }

    public Vector2 GetMovementForceDueToInput()
    {
        return _movementController.GetForceDueToIncrementNeeded() * GetCurrentMovementInput();
    }

    public void InitialiseController(Func<float> getSpeed, Func<float> getMass)
    {
        _movementController.Initialise(getSpeed, getMass);
    }
}
