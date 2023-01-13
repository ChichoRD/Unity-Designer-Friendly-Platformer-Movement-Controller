using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

[RequireComponent(typeof(IJumpInputter))]
public class MultiJumpInputter : MonoBehaviour, IJumpInputter
{
    [SerializeField] private Object[] _jumpInputterObjects;
    private IJumpInputter[] JumpInputters => _jumpInputterObjects.Select(o => o as IJumpInputter).ToArray();

    public IJumpController JumpController => JumpInputters[0].JumpController;
    [field: SerializeField] public UnityEvent OnJumpInputDenied { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpCancelInputDenied { get; private set; }

    private void Awake()
    {
        foreach (var inputter in JumpInputters)
        {
            inputter.OnJumpInputDenied.AddListener(OnJumpInputDenied.Invoke);
            inputter.OnJumpCancelInputDenied.AddListener(OnJumpCancelInputDenied.Invoke);
        }
    }

    public void CancelJump()
    {
        foreach (var jumpInputter in JumpInputters)
            jumpInputter.CancelJump();
    }

    public void Jump()
    {
        foreach (var jumpInputter in JumpInputters)
            jumpInputter.Jump();
    }

    public void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd)
    {
        foreach (var inputter in JumpInputters)
            inputter.Initialise(getJumpDirection, getGroundDirection, getVelocity, getMass, onForceAdd);
    }

    public void UpdateJumpControllerInfo()
    {
        foreach (var inputter in JumpInputters)
            inputter.UpdateJumpControllerInfo();
    }
}
