using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EarlyJumpConstrainer))]
public abstract class RaycastCheckJumpConstrainer : MonoBehaviour, IUpdateableJumpConstrainer, IRaycastCheckJumpConstrainer
{
    [SerializeField] private EarlyJumpConstrainer _earlyJumpConstrainer;

    [Space]
    [SerializeField] private Vector2 _groundCheckAreaSize = Vector2.one;
    [SerializeField][Range(10e-2f, 10e+2f)] private float _raycastDensity = 3f;
    [SerializeField][Min(0)] private float _raycastsLength = 0.3f;
    [SerializeField][Range(0f, 90f)] private float _maxRaycastTilt;
    [SerializeField] private LayerMask _jumpableLayers;
    [SerializeField] private Transform _groundCheckCentre;

    private Vector3 lastNormal;
    public Vector3 LastNormal
    {
        get => lastNormal;
        private set
        {
            lastNormal = value;
            LastNormalAngle = Vector3.Angle(GetJumpDirection(), value);
        }
    }

    private float _lastNormalAngle;
    public float LastNormalAngle { get => _lastNormalAngle; private set => _lastNormalAngle = value; }
    public Func<Vector3> GetGroundDirection => _earlyJumpConstrainer.GetGroundDirection;
    public Func<Vector3> GetJumpDirection => _earlyJumpConstrainer.GetJumpDirection;

    private bool _isOnValidGround;

    public bool IsOnValidGround
    {
        get => _isOnValidGround;
        private set
        {
            bool wasOnGround = _isOnValidGround;
            _isOnValidGround = value;

            if (wasOnGround && !_isOnValidGround)
                OnLeftGround?.Invoke();

            if (!wasOnGround && _isOnValidGround)
                OnLanded?.Invoke();
        }
    }

    [field: Space]
    [field: SerializeField] public UnityEvent OnLanded { get; private set; }
    [field: SerializeField] public UnityEvent OnLeftGround { get; set; }

    [field: SerializeField] public UnityEvent OnJumpAbilityRestored { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpAbilityLost { get; private set; }
    public float RaycastsLength { get => _raycastsLength; }
    public LayerMask JumpableLayers { get => _jumpableLayers; }

    private bool CheckGroundState()
    {
        bool onGround = false;
        GenerateRayField((ray) =>
        {
            bool hit = RaycastCheck(ray.origin, ray.direction, _raycastsLength, _jumpableLayers, out Vector3 normal);
            LastNormal = normal;
            if (!hit) return false;

            onGround = true;
            return true;
        });

        return onGround;
    }

    public void CheckStateAndUpdate()
    {
        IsOnValidGround = CheckGroundState();
    }

    public virtual bool CanJump()
    {
        return _earlyJumpConstrainer.CanJump() && IsOnValidGround;
    }

    public virtual void Initialise(IJumpController jumpController)
    {
        _earlyJumpConstrainer.Initialise(jumpController);

        OnLanded.AddListener(OnJumpAbilityRestored.Invoke);
        OnLeftGround.AddListener(OnJumpAbilityLost.Invoke);
    }

    public void DebugGroundChecks(Color groundChecksColor)
    {
        GenerateRayField((ray) =>
        {
            Vector3 end = ray.origin + ray.direction * _raycastsLength;
            Debug.DrawLine(ray.origin, end, groundChecksColor);

            return false;
        });
    }

    public void GenerateRayField(Func<Ray, bool> onRayShortcircuit)
    {
        if (!_earlyJumpConstrainer.HasJumpController || _groundCheckCentre == null || GetGroundDirection == null) return;

        Vector3 centre = _groundCheckCentre.position;
        Vector2 halfSize = _groundCheckAreaSize / 2f;
        float step = 1 / _raycastDensity;
        float maxDistance = Vector3.Distance(centre, centre + new Vector3(halfSize.x, 0f, halfSize.y));

        for (float x = -halfSize.x; x <= halfSize.x; x += step)
        {
            for (float z = -halfSize.y; z <= halfSize.y; z += step)
            {
                Vector3 origin = centre + new Vector3(x, 0, z);
                Vector3 direction = GetGroundDirection();

                float distance = Vector3.Distance(origin, centre);
                float normalisedDistance = distance / maxDistance;
                Vector3 axis = Vector3.Cross(GetGroundDirection(), origin + direction - centre).normalized;
                Quaternion rotation = Quaternion.AngleAxis(_maxRaycastTilt * normalisedDistance, axis);
                direction = Matrix4x4.Rotate(rotation).MultiplyVector(direction);

                Ray ray = new Ray(origin, direction);
                if (onRayShortcircuit(ray)) return;
            }
        }
    }

    public abstract bool RaycastCheck(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal);
    protected virtual void OnDrawGizmosSelected()
    {
        DebugGroundChecks(Color.red);
    }
}
