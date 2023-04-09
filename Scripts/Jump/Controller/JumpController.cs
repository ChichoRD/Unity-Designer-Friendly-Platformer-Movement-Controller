using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class JumpController : MonoBehaviour, IJumpController
{
    [SerializeField] private float _jumpSpeed;
    [SerializeField][Min(0)] private float _minJumpTime;
    [SerializeField] private float _defaultGravity;
    [SerializeField][Min(0)] private float _fallGravityMultiplier = 2.5f;
    [SerializeField] private float _fallGravity;
    private float _currentGravity;
    [SerializeField][Min(0)] private float _jumpKillDelay = 0.2f;

    [Space]
    [SerializeField][Min(0)] private float _maxJumpHeight = 8f;
    [SerializeField][Min(0)] private float _maxJumpTime = 1f;
    [SerializeField][Min(0)] private float _minJumpHeight = 2f;
    [SerializeField][Min(0)] private float _descentTimeFromPeak = 1f;

    [Space]
    [SerializeField] private Transform _jumpHeightsDebugger;

    private float _jumpTimeElapsed;
    private Coroutine _jumpTimeCoroutine;
#pragma warning disable IDE0052 //Remove unused private members
    private Coroutine _jumpKillCoroutine;
#pragma warning restore IDE0052

    private float _lastYVelocity;
    private float LastYVelocity
    {
        get => _lastYVelocity;
        set
        {
            if (value < 0 && _lastYVelocity >= 0)
                OnDescentStart?.Invoke();

            _lastYVelocity = value;
        }
    }

    public Func<Vector3> GetVelocity { get; private set; }
    public Func<Vector3> GetJumpDirection { get; set; }
    public Func<Vector3> GetGroundDirection { get; set; }
    public Func<float> GetMass { get; private set; }
    public Action<Vector3> OnForceAdd { get; private set; }

    public float MaxJumpHeight => _maxJumpHeight;
    public float MaxJumpTime => _maxJumpTime;
    public float MinJumpHeight => _minJumpHeight;
    protected float DescentTimeFromPeak => _descentTimeFromPeak;
    protected bool Descending => LastYVelocity < 0;
    protected bool Ascending => LastYVelocity > 0;

    [field: Space]
    [field: SerializeField] public UnityEvent OnDescentStart { get; private set; }
    [field: SerializeField] public UnityEvent OnJump { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpCancel { get; private set; }
    public float JumpSpeed { get => _jumpSpeed; }
    public float CurrentGravity { get => _currentGravity; }

    protected virtual void Awake()
    {
        SetDefaultAddedGravity();

        OnJump.AddListener(SetDefaultAddedGravity);
        OnJump.AddListener(JumpTimeElapsedUpdate);

        OnDescentStart.AddListener(SetDescentAddedGravity);
        OnDescentStart.AddListener(JumpTimeElapsedStop);
    }

    public float GetJumpSpeed(float peakHeight, float timeToPeak)
    {
        //h''(t) = g
        //h'(t) = v0 + g*t
        //h(t) = v0*t + 0.5*g*t^2

        //h'(t_h) = 0 <-- We want to reach the max height at t_h, where the velocity is 0
        //v0 + g*t_h = 0
        //v0 = -g*t_h

        //h(t_h) = v0*t_h + 0.5*g*t_h^2
        //h(t_h) = h_max
        //v0*t_h + 0.5*g*t_h^2 = h_max
        //v0*t_h - 0.5*(-g*t_h)*t_h = h_max
        //v0*t_h - 0.5*v0*t_h = h_max
        //0.5*v0*t_h = h_max

        //v0 = 2 * h_max/ t_h <-- This is the max velocity we need to reach the max height in t_h seconds
        return 2f * peakHeight / timeToPeak;
    }

    public float GetJumpSpeedDueToGravity(float gravity, float timeToPeak)
    {
        //h'(t_h) = 0 <-- We want to reach the max height at t_h, where the velocity is 0
        //v0 + g*t_h = 0
        //v0 = -g*t_h

        return -gravity * timeToPeak;
    }

    public float GetJumpAcceleration(float peakHeight, float timeToPeak)
    {
        //v0 = -g*t_h
        //2 * h_max/ t_h = -g*t_h
        //g = -2 * h_max / t_h^2 <-- This is the gravity we need to reach the max height in t_h seconds

        return -2f * peakHeight / (timeToPeak * timeToPeak);
    }

    public float GetJumpPeakReachTimeDueToVelocity(float peakHeight, float velocity)
    {
        //v0 = 2 * h_max/ t_h <-- This is the max velocity we need to reach the max height in t_h seconds
        //t_h = 2 * h_max / v0
        return 2f * peakHeight / velocity;
    }

    public float GetJumpPeakReachTimeDueToAcceleration(float peakHeight, float acceleration)
    {
        //g = -2 * h_max / t_h^2 <-- This is the gravity we need to reach the max height in t_h seconds
        //t_h = sqrt(-2 * h_max / g)
        return MathF.Sqrt(-2f * peakHeight / acceleration);
    }

    public void AdjustJumpParameters()
    {
        _jumpSpeed = GetJumpSpeed(_maxJumpHeight, _maxJumpTime);
        _defaultGravity = GetJumpAcceleration(_maxJumpHeight, _maxJumpTime);

        _minJumpTime = Mathf.Max(GetJumpPeakReachTimeDueToVelocity(_minJumpHeight, _jumpSpeed) - _jumpKillDelay, 0f);
        _fallGravity = GetJumpAcceleration(_maxJumpHeight, _descentTimeFromPeak);
        _fallGravityMultiplier = _fallGravity / _defaultGravity;
    }

    public virtual bool PerformJump()
    {
        //f = m * dv / dv
        if (Descending)
            CancelFalling();

        OnForceAdd?.Invoke(_jumpSpeed * GetMass() * GetJumpDirection() / Time.fixedDeltaTime);
        OnJump?.Invoke();
        return true;
    }

    public virtual bool CancelJump()
    {
        //if (Descending) return false;
        JumpKill();
        OnJumpCancel?.Invoke();
        return true;
    }

    protected virtual bool CancelFalling()
    {
        if (!Descending) return false;
        Vector3 fallingVelocity = Vector3.Project(GetVelocity(), GetGroundDirection());
        Vector3 cancelingForce = GetMass() * (-fallingVelocity) / Time.fixedDeltaTime;
        OnForceAdd(cancelingForce);
        return true;
    }

    public void UpdateInfo() => LastYVelocity = GetVelocity().y;

    protected void SetDefaultAddedGravity() => _currentGravity = _defaultGravity;
    protected void SetDescentAddedGravity() => _currentGravity = _fallGravity;

    public Vector3 GetGravityDifference(Vector3 defaultAcceleration) => GetJumpDirection() * _currentGravity - defaultAcceleration;

    protected void JumpKill()
    {
        _jumpKillCoroutine ??= StartCoroutine(JumpKillCoroutine());
    }

    private IEnumerator JumpKillCoroutine()
    {
        yield return new WaitForSeconds(Mathf.Max(_minJumpTime - _jumpTimeElapsed, 0f));

        const float KILL_FACTOR = 0.85f;
        for (float t = 0; t < _jumpKillDelay; t += Time.fixedDeltaTime)
        {
            Vector3 killingForce = GetMass() * Math.Abs(GetVelocity().y) * KILL_FACTOR * GetGroundDirection() / _jumpKillDelay;
            OnForceAdd(killingForce);
            yield return null;
        }

        _jumpKillCoroutine = null;
    }

    protected void JumpTimeElapsedUpdate()
    {
        _jumpTimeCoroutine ??= StartCoroutine(JumpTimeElapsedUpdateCoroutine());
    }

    protected void JumpTimeElapsedStop()
    {
        if (_jumpTimeCoroutine == null) return;
        StopCoroutine(_jumpTimeCoroutine);
        _jumpTimeCoroutine = null;
    }

    private IEnumerator JumpTimeElapsedUpdateCoroutine()
    {
        _jumpTimeElapsed = 0f;
        while (_jumpTimeElapsed < _maxJumpTime)
        {
            _jumpTimeElapsed += Time.deltaTime;
            yield return null;
        }

        _jumpTimeCoroutine = null;
    }

    protected virtual void OnValidate()
    {
        AdjustJumpParameters();
    }

    public void DebugJumpHeights(Color minJumpHeightColor, Color maxJumpHeightColor)
    {
        if (_jumpHeightsDebugger == null || GetJumpDirection == null) return;

        Vector3 origin = _jumpHeightsDebugger.position;
        Vector3 maxEnd = origin + GetJumpDirection() * MaxJumpHeight;
        Vector3 minEnd = origin + GetJumpDirection() * MinJumpHeight;

        Debug.DrawLine(origin, maxEnd, maxJumpHeightColor);
        Debug.DrawLine(origin, minEnd, minJumpHeightColor);
    }

    public void DebugJumpArcs(Color minJumpHeightColor, Color maxJumpHeightColor, Func<Vector3> getHorizontalVelocity)
    {
        DebugJumpArc(maxJumpHeightColor, _maxJumpHeight, _maxJumpTime, getHorizontalVelocity);
        DebugJumpArc(minJumpHeightColor, _minJumpHeight, _minJumpTime + _jumpKillDelay, getHorizontalVelocity);
    }

    public Vector3 GetLandingPosition(float peakHeight, float jumpTime, Func<Vector3> getHorizontalVelocity)
    {
        if (_jumpHeightsDebugger == null || GetJumpDirection == null) return Vector3.zero;

        Vector3 initialVelocity = GetJumpSpeed(peakHeight, jumpTime) * GetJumpDirection() + getHorizontalVelocity();
        Vector3 descentAcceleration = GetJumpAcceleration(peakHeight, jumpTime) * GetJumpDirection() * _fallGravityMultiplier;

        float timeToPeak = GetJumpPeakReachTimeDueToVelocity(peakHeight, Vector3.Dot(initialVelocity, GetJumpDirection()));
        float timeToDescendFromPeak = GetJumpPeakReachTimeDueToAcceleration(peakHeight, Vector3.Dot(descentAcceleration, GetJumpDirection()));

        return _jumpHeightsDebugger.position + getHorizontalVelocity() * (timeToPeak + timeToDescendFromPeak);
    }

    public List<Vector3> SamplePointsAlongJumpArc(Vector3 initialPosition, float peakHeight, float jumpTime, Func<Vector3> getHorizontalVelocity, float dtFactor = 1)
    {
        if (GetJumpDirection == null) return Enumerable.Empty<Vector3>().ToList();

        Vector3 currentPosition = initialPosition;
        List<Vector3> points = new List<Vector3>() { currentPosition };

        Vector3 initialVelocity = GetJumpSpeed(peakHeight, jumpTime) * GetJumpDirection() + getHorizontalVelocity();
        Vector3 currentVelocity = initialVelocity;

        Vector3 initialAcceleration = GetJumpAcceleration(peakHeight, jumpTime) * GetJumpDirection();
        Vector3 currentAcceleration = initialAcceleration;

        float timeToPeak = GetJumpPeakReachTimeDueToVelocity(peakHeight, Vector3.Dot(initialVelocity, GetJumpDirection()));
        float dt = Time.fixedDeltaTime * dtFactor;
        const int MAX_ITERATIONS = 1000;
        int i = 0;

        for (float t = 0; t < timeToPeak; t += dt)
        {
            if (i++ > MAX_ITERATIONS)
            {
                Debug.LogWarning("Max iterations reached");
                break;
            }

            //Verlet
            currentPosition += currentVelocity * dt + 0.5f * dt * dt * currentAcceleration;
            currentVelocity += currentAcceleration * dt;

            points.Add(currentPosition);
        }

        currentAcceleration *= _fallGravityMultiplier;
        float timeToDescendFromPeak = GetJumpPeakReachTimeDueToAcceleration(peakHeight, Vector3.Dot(currentAcceleration, GetJumpDirection()));
        i = 0;
        for (float t = 0; t < timeToDescendFromPeak; t += dt)
        {
            if (i++ > MAX_ITERATIONS)
            {
                Debug.LogWarning("Max iterations reached");
                break;
            }

            //Verlet
            currentPosition += currentVelocity * dt + 0.5f * dt * dt * currentAcceleration;
            currentVelocity += currentAcceleration * dt;

            points.Add(currentPosition);
        }

        return points;
    }

    private void DebugJumpArc(Color arcColor, float peakHeight, float jumpTime, Func<Vector3> getHorizontalVelocity)
    {
        if (_jumpHeightsDebugger == null) return;
        var positions = SamplePointsAlongJumpArc(_jumpHeightsDebugger.position, peakHeight, jumpTime, getHorizontalVelocity);

        for (int i = 0; i < positions.Count - 1; i++)
        {
            Vector3 currentPosition = positions[i];
            Vector3 nextedPosition = positions[i + 1];

            Debug.DrawLine(currentPosition, nextedPosition, arcColor);
        }
    }

    public virtual void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd)
    {
        GetJumpDirection = getJumpDirection;
        GetGroundDirection = getGroundDirection;
        GetVelocity = getVelocity;
        GetMass = getMass;
        OnForceAdd = onForceAdd;
    }
}
