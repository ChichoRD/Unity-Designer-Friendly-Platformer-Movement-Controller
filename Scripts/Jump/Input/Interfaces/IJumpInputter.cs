using System;
using UnityEngine;
using UnityEngine.Events;

public interface IJumpInputter
{
    IJumpController JumpController { get; }

    UnityEvent OnJumpInputDenied { get; }
    UnityEvent OnJumpCancelInputDenied { get; }

    void Jump();
    void CancelJump();
    void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd);
    void UpdateJumpControllerInfo();
}
