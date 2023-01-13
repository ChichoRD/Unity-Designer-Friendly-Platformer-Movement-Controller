using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public class JumpInputter : MonoBehaviour, IJumpInputter
{
    [RequireInterface(typeof(IJumpController))]
    [SerializeField] private Object _jumpController;
    public IJumpController JumpController => _jumpController as IJumpController;

    [field: SerializeField] public UnityEvent OnJumpInputDenied { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpCancelInputDenied { get; private set; }

    public void Jump()
    {
        if (JumpController.PerformJump()) return;

        OnJumpInputDenied?.Invoke();
    }

    public void CancelJump()
    {
        if (JumpController.CancelJump()) return;

        OnJumpCancelInputDenied?.Invoke();
    }

    public void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd)
    {
        JumpController.Initialise(getJumpDirection, getGroundDirection, getVelocity, getMass, onForceAdd);
    }

    public void UpdateJumpControllerInfo()
    {
        JumpController.UpdateInfo();
    }
}