using System;
using UnityEngine;
using UnityEngine.Events;

public class EarlyJumpConstrainer : MonoBehaviour, IJumpConstrainer
{
    [SerializeField] private bool _invertEarlyJump;
    private bool _inEarlyJump;
    private IJumpController _jumpController;
    
    public bool HasJumpController => _jumpController != null;
    public Func<Vector3> GetGroundDirection => _jumpController.GetGroundDirection;
    public Func<Vector3> GetJumpDirection => _jumpController.GetJumpDirection;
    [field: SerializeField] public UnityEvent OnJumpAbilityRestored { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpAbilityLost { get; private set; }
    
    public bool InEarlyJump
    {
        get => _inEarlyJump;
        private set
        {
            bool couldJump = CanJump(_inEarlyJump);
            bool canJump = CanJump(value);
            
            _inEarlyJump = value;

            if (couldJump && !canJump)
                OnJumpAbilityLost?.Invoke();

            if (!couldJump && canJump)
                OnJumpAbilityRestored?.Invoke();
        }
    }

    public bool CanJump()
    {
        return CanJump(InEarlyJump);
    }

    public void Initialise(IJumpController jumpController)
    {
        _jumpController = jumpController;

        _jumpController.OnJump.AddListener(SetEarlyJumpState);
        _jumpController.OnDescentStart.AddListener(SetLateJumpState);
    }
    
    private bool CanJump(bool earlyJump)
    {
        return !earlyJump ^ _invertEarlyJump;
    }

    private void SetEarlyJumpState() => InEarlyJump = true;
    private void SetLateJumpState() => InEarlyJump = false;
}
