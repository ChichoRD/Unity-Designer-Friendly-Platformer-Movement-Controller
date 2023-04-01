using UnityEngine;

public abstract class JumpSetHandler : MonoBehaviour, IJumpSetHandler
{
    [RequireInterface(typeof(IJumpInputter))]
    [SerializeField] private Object _jumpInputter;
    public IJumpInputter JumpInputter => _jumpInputter as IJumpInputter;

    public abstract void ApplyGravitationalForces();
    public abstract void InitialiseJumpController();

    protected virtual void OnValidate()
    {
        if (JumpInputter == null) return;
        InitialiseJumpController();
    }
}