using UnityEngine.Events;

internal interface IConstrainedJumpController : IJumpController
{
    IJumpConstrainer JumpConstrainer { get; }
    UnityEvent OnFailedToJump { get; }
}
