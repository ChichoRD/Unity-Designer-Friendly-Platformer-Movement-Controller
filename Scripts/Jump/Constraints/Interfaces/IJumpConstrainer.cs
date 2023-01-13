using System;
using UnityEngine;
using UnityEngine.Events;

public interface IJumpConstrainer
{
    Func<Vector3> GetGroundDirection { get; }
    Func<Vector3> GetJumpDirection { get; }

    UnityEvent OnJumpAbilityRestored { get; }
    UnityEvent OnJumpAbilityLost { get; }

    bool CanJump();
    void Initialise(IJumpController jumpController);
}