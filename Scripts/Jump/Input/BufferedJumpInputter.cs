using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public class BufferedJumpInputter : MonoBehaviour, IJumpInputter
{
    [SerializeField][Min(0f)] private float _jumpBufferTime;
    private bool InJumpBuffer => _jumpBufferCoroutine != null;
    private Coroutine _jumpBufferCoroutine;
        
    [RequireInterface(typeof(IConstrainedJumpController))]
    [SerializeField] private Object _constrainedJumpControllerObject;
    private IConstrainedJumpController ConstrainedJumpController => _constrainedJumpControllerObject as IConstrainedJumpController;
    public IJumpController JumpController => _constrainedJumpControllerObject as IJumpController;

    [field: SerializeField] public UnityEvent OnJumpInputDenied { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpCancelInputDenied { get; private set; }
    [field: SerializeField] public UnityEvent OnBufferedJump { get; private set; }

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

    protected virtual void Awake()
    {
        OnJumpInputDenied.AddListener(JumpBufferStart);

        ConstrainedJumpController.JumpConstrainer.OnJumpAbilityRestored.AddListener(BufferedJump);
        ConstrainedJumpController.JumpConstrainer.OnJumpAbilityRestored.AddListener(JumpBufferStop);
    }

    private void JumpBufferStop()
    {
        if (_jumpBufferCoroutine == null) return;
        StopCoroutine(_jumpBufferCoroutine);
        _jumpBufferCoroutine = null;
    }

    private void JumpBufferStart()
    {
        _jumpBufferCoroutine ??= StartCoroutine(JumpBufferCoroutine());
    }

    private IEnumerator JumpBufferCoroutine()
    {
        yield return new WaitForSeconds(_jumpBufferTime);
        _jumpBufferCoroutine = null;
    }

    private void BufferedJump()
    {
        if (!InJumpBuffer) return;
        
        Jump();
        OnBufferedJump?.Invoke();
    }
}
