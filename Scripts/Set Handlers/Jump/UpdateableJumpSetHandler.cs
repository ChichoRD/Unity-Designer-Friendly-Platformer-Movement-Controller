using System.Linq;
using UnityEngine;

[RequireComponent(typeof(JumpSetHandler))]
public class UpdateableJumpSetHandler : MonoBehaviour, IJumpSetHandler
{
    [RequireInterface(typeof(IJumpSetHandler))]
    [SerializeField] private Object _jumpSetHandler;
    public IJumpSetHandler JumpSetHandler => _jumpSetHandler as IJumpSetHandler;
    public IJumpInputter JumpInputter => JumpSetHandler.JumpInputter;

    [SerializeField] private Object[] _jumpUpdateableConstrainerObjects;
    private IUpdateableJumpConstrainer[] JumpConstrainers => _jumpUpdateableConstrainerObjects.Select(x => x as IUpdateableJumpConstrainer).ToArray();

    protected virtual void Update()
    {
        foreach (var updateable in JumpConstrainers)
            updateable.CheckStateAndUpdate();

        JumpInputter.UpdateJumpControllerInfo();
    }

    public void InitialiseJumpController()
    {
        JumpSetHandler.InitialiseJumpController();
    }

    public void ApplyGravitationalForces()
    {
        JumpSetHandler.ApplyGravitationalForces();
    }
}