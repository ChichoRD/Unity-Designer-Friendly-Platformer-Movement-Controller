using System;
using UnityEngine;
using UnityEngine.Events;

//[RequireComponent(typeof(RaycastCheckJumpConstrainer))]
public class AngledRaycastCheckJumpConstrainer : MonoBehaviour, IUpdateableJumpConstrainer, IRaycastCheckJumpConstrainer
{
    [SerializeField] private RaycastCheckJumpConstrainer _jumpConstrainer;
    [SerializeField][Range(0f, 90f)] private float _maxJumpAngle = 45f;
    [SerializeField][Range(0f, 90f)] private float _minJumpAngle = 0f;
    [SerializeField][Min(0f)] private float _tolerance = 0.1f;
    public float MaxJumpAngle => _maxJumpAngle;
    private bool _isOnValidGround;
    public bool IsOnValidGround
    {
        get => _isOnValidGround;
        private set
        {
            bool wasOnValidGround = _isOnValidGround;
            _isOnValidGround = value;

            if (wasOnValidGround && !_isOnValidGround)
                OnLeftValidGround?.Invoke();

            if (!wasOnValidGround && _isOnValidGround)
                OnLandedOnValidGround?.Invoke();
        }
    }

    [field: SerializeField] public UnityEvent OnJumpAbilityRestored { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpAbilityLost { get; private set; }

    public Func<Vector3> GetGroundDirection => _jumpConstrainer.GetGroundDirection;
    public Func<Vector3> GetJumpDirection => _jumpConstrainer.GetJumpDirection;

    [field: SerializeField] public UnityEvent OnLeftValidGround { get; private set; }
    [field: SerializeField] public UnityEvent OnLandedOnValidGround { get; private set; }

    public Vector3 LastNormal => _jumpConstrainer.LastNormal;
    public LayerMask JumpableLayers => _jumpConstrainer.JumpableLayers;
    public float RaycastsLength => _jumpConstrainer.RaycastsLength;

    public float LastNormalAngle => ((IRaycastCheckJumpConstrainer)_jumpConstrainer).LastNormalAngle;

    public bool CanJump()
    {
        return _jumpConstrainer.CanJump() && IsOnValidGround;
    }

    public void Initialise(IJumpController jumpController)
    {
        _jumpConstrainer.Initialise(jumpController);

        OnLandedOnValidGround.AddListener(OnJumpAbilityRestored.Invoke);
        OnLeftValidGround.AddListener(OnJumpAbilityLost.Invoke);
    }

    private bool CheckValidGroundState()
    {
        _jumpConstrainer.CheckStateAndUpdate();

        bool hit = false;
        bool validAngle = false;
        GenerateRayField((ray) =>
        {
            hit = _jumpConstrainer.RaycastCheck(ray.origin, ray.direction, RaycastsLength, JumpableLayers, out Vector3 normal);
            if (!hit || normal == Vector3.zero) return false;

            float angle = Vector3.Angle(GetJumpDirection(), normal);
            validAngle = ValidAngle(angle);
            return validAngle;
        });

        return validAngle && hit;

        bool ValidAngle(float angle) => angle - _tolerance < _maxJumpAngle && angle + _tolerance >= _minJumpAngle;
    }

    public void CheckStateAndUpdate()
    {
        IsOnValidGround = CheckValidGroundState();
    }

    public void GenerateRayField(Func<Ray, bool> onRayShortcircuit)
    {
        _jumpConstrainer.GenerateRayField(onRayShortcircuit);
    }
}
