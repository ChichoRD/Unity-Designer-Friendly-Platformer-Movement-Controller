using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

//[RequireComponent(typeof(RaycastCheckJumpConstrainer))]
public class CoyoteTimeJumpConstrainer : MonoBehaviour, IJumpConstrainer
{
    [RequireInterface(typeof(IJumpConstrainer))]
    [SerializeField] private Object _jumpConstrainerObject;
    private IJumpConstrainer JumpConstrainer => _jumpConstrainerObject as IJumpConstrainer;

    [SerializeField][Min(0)] private float _coyoteTime;
    private Coroutine _coyoteTimeCoroutine;
    private bool InCoyoteTime => _coyoteTimeCoroutine != null;

    public Func<Vector3> GetGroundDirection => JumpConstrainer.GetGroundDirection;
    public Func<Vector3> GetJumpDirection => JumpConstrainer.GetJumpDirection;
    public UnityEvent OnJumpAbilityRestored => JumpConstrainer.OnJumpAbilityRestored;
    [field: SerializeField] public UnityEvent OnJumpAbilityLost { get; private set; }

    public bool CanJump()
    {
        return JumpConstrainer.CanJump() || InCoyoteTime;
    }

    public void Initialise(IJumpController jumpController)
    {
        JumpConstrainer.Initialise(jumpController);
        
        JumpConstrainer.OnJumpAbilityLost.AddListener(CoyoteTimeStart);
        JumpConstrainer.OnJumpAbilityRestored.AddListener(CoyoteTimeStop);

        jumpController.OnJump.AddListener(RemoveCoyoteTimeStartListenerOnLeftGround);
        jumpController.OnJump.AddListener(CoyoteTimeStop);
        jumpController.OnJump.AddListener(OnJumpAbilityLost.Invoke);
        jumpController.OnDescentStart.AddListener(AddCoyoteTimeStartListenerOnLeftGround);
    }

    private void CoyoteTimeStart()
    {
        _coyoteTimeCoroutine ??= StartCoroutine(CoyoteTimeCoroutine());
    }

    private void CoyoteTimeStop()
    {
        if (_coyoteTimeCoroutine == null) return;
        StopCoroutine(_coyoteTimeCoroutine);
        _coyoteTimeCoroutine = null;
    }

    private IEnumerator CoyoteTimeCoroutine()
    {
        yield return new WaitForSeconds(_coyoteTime);
        _coyoteTimeCoroutine = null;
        OnJumpAbilityLost?.Invoke();
    }

    private void AddCoyoteTimeStartListenerOnLeftGround()
    {
        JumpConstrainer.OnJumpAbilityLost.RemoveListener(CoyoteTimeStart);
        JumpConstrainer.OnJumpAbilityLost.AddListener(CoyoteTimeStart);
    }

    private void RemoveCoyoteTimeStartListenerOnLeftGround()
    {
        JumpConstrainer.OnJumpAbilityLost.RemoveListener(CoyoteTimeStart);
    }
}