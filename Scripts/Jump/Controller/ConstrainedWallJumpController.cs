using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

//[RequireComponent(typeof(IRaycastCheckJumpConstrainer))]
public class ConstrainedWallJumpController : MonoBehaviour, IConstrainedJumpController
{
    [SerializeField][Min(0)] private float _wallJumpSpeed;

    [RequireInterface(typeof(IJumpController))]
    [SerializeField] private Object _jumpControllerObject;
    private IJumpController JumpController => _jumpControllerObject as IJumpController;

    [RequireInterface(typeof(IJumpConstrainer))]
    [SerializeField] private Object _mainJumpConstrainerObject;
    public IJumpConstrainer JumpConstrainer => _mainJumpConstrainerObject as IJumpConstrainer;
    
    [RequireInterface(typeof(IRaycastCheckJumpConstrainer))]
    [SerializeField] private Object _raycastCheckConstrainerObject;
    private IRaycastCheckJumpConstrainer RaycastCheckJumpConstrainer => _raycastCheckConstrainerObject as IRaycastCheckJumpConstrainer;

    public Func<Vector3> GetVelocity => JumpController.GetVelocity;
    public Func<Vector3> GetJumpDirection { get => JumpController.GetJumpDirection; set => JumpController.GetJumpDirection = value; }
    public Func<Vector3> GetGroundDirection { get => JumpController.GetGroundDirection; set => JumpController.GetGroundDirection = value; }
    public Func<float> GetMass => JumpController.GetMass;
    public Action<Vector3> OnForceAdd => JumpController.OnForceAdd;

    [field: SerializeField] public UnityEvent OnFailedToJump { get; private set; }
    [field: SerializeField] public UnityEvent OnWallJump { get; private set; }

    public UnityEvent OnJump => JumpController.OnJump;
    public UnityEvent OnDescentStart => JumpController.OnDescentStart;

    public bool PerformJump()
    {
        if (!JumpConstrainer.CanJump())
        {
            OnFailedToJump?.Invoke();
            return false;
        }

        return PerformWallJump();
    }

    public void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd)
    {
        JumpController.Initialise(getJumpDirection, getGroundDirection, getVelocity, getMass, onForceAdd);
        JumpConstrainer.Initialise(JumpController);
    }

    public bool CancelJump()
    {
        return JumpController.CancelJump();
    }

    public void AdjustJumpParameters()
    {
        JumpController.AdjustJumpParameters();
    }

    public void UpdateInfo()
    {
        JumpController.UpdateInfo();
    }

    public Vector3 GetGravityDifference(Vector3 defaultAcceleration)
    {
        return JumpController.GetGravityDifference(defaultAcceleration);
    }

    public void DebugJumpHeights(Color minJumpHeightColor, Color maxJumpHeightColor)
    {
        JumpController.DebugJumpHeights(minJumpHeightColor, maxJumpHeightColor);
    }

    public void DebugJumpArcs(Color minJumpHeightColor, Color maxJumpHeightColor, Func<Vector3> getHorizontalVelocity)
    {
        JumpController.DebugJumpArcs(minJumpHeightColor, maxJumpHeightColor, getHorizontalVelocity);
    }

    public bool PerformWallJump()
    {
        float mass = GetMass();
        Vector3 horizontalStoppingForce = mass * (-Vector3.ProjectOnPlane(GetVelocity(), GetJumpDirection())) / Time.fixedDeltaTime;

        Vector3 wallDirection = RaycastCheckJumpConstrainer.LastNormal;
        Vector3 horizontalWallJumpForce = mass * _wallJumpSpeed * wallDirection / Time.fixedDeltaTime;

        OnForceAdd?.Invoke(horizontalStoppingForce);
        OnForceAdd?.Invoke(horizontalWallJumpForce);
        bool jump = JumpController.PerformJump();
        OnWallJump?.Invoke();

        return jump;
    }
}