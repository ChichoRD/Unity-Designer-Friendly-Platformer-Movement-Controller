using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HorizontalMovementController : MonoBehaviour
{
    [SerializeField][Min(0)] private float _accelerationTime = 0.2f;
    [SerializeField] private AnimationCurve _accelerationCurve;
    private Coroutine _accelerationRoutine;

    [Space]
    [SerializeField][Min(0)] private float _deccelerationTime = 0.3f;
    [SerializeField] private AnimationCurve _deccelerationCurve;
    private Coroutine _deccelerationRoutine;

    private float _elapsedMovementTime;
    private float _elapsedStopedTime;
    public bool Accelerating => _accelerationRoutine != null;
    public bool Deccelerating => _deccelerationRoutine != null;

    [SerializeField][Min(0)] private float _maxMovementSpeed = 12.08f;
    private float _currentMovementSpeed;
    private float _currentRelativeMovementSpeed;
    [SerializeField][Range(0f, 1f)] protected float _counterMovementFactor = 0.05f;

    public Func<float> GetRigidbodySpeed { get; private set; }
    public Func<float> GetRigidbodyMass { get; private set; }
    public float MaxMovementSpeed { get => _maxMovementSpeed; }

    [field: SerializeField] public UnityEvent OnAccelerationStarted { get; private set; }
    [field: SerializeField] public UnityEvent OnReachedTopSpeed { get; private set; }
    [field: SerializeField] public UnityEvent OnDeccelerationStarted { get; private set; }
    [field: SerializeField] public UnityEvent OnSlowedDownToStop { get; private set; }

    private IEnumerator AccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedMovementTime = 0f;
        float initialMinRelativeSpeed = _currentRelativeMovementSpeed;
        OnAccelerationStarted?.Invoke();
        
        while (_elapsedMovementTime < _accelerationTime)
        {
            _elapsedMovementTime += Time.fixedDeltaTime;
            float n = _accelerationCurve.Evaluate(_elapsedMovementTime / _accelerationTime);
            
            //Sqrt correction for acceleration curve because we later multiply by the relative speed
            _currentRelativeMovementSpeed = Mathf.Sqrt(n) * (1 - initialMinRelativeSpeed) + initialMinRelativeSpeed;
            yield return wait;
        }

        _currentRelativeMovementSpeed = 1f;
        _accelerationRoutine = null;
        OnReachedTopSpeed?.Invoke();
    }

    private IEnumerator DeccelerationRoutine()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        _elapsedStopedTime = 0f;
        float initialMaxRelativeSpeed = _currentRelativeMovementSpeed;
        OnDeccelerationStarted?.Invoke();

        while (_elapsedStopedTime < _deccelerationTime)
        {
            _elapsedStopedTime += Time.fixedDeltaTime;
            float n = _deccelerationCurve.Evaluate(_elapsedStopedTime / _deccelerationTime);
            
            //Sqrt correction for acceleration curve because we later multiply by the relative speed
            _currentRelativeMovementSpeed = Mathf.Sqrt(n) * initialMaxRelativeSpeed;
            yield return wait;
        }

        _currentRelativeMovementSpeed = 0f;
        _deccelerationRoutine = null;
        OnSlowedDownToStop?.Invoke();
    }

    public void BeginAcceleration()
    {
        StopAllAccelerations();

        _accelerationRoutine = StartCoroutine(AccelerationRoutine());
    }

    public void BeginDecceleration()
    {
        StopAllAccelerations();

        _deccelerationRoutine = StartCoroutine(DeccelerationRoutine());
    }

    private void StopAllAccelerations()
    {
        if (_accelerationRoutine != null)
            StopCoroutine(_accelerationRoutine);
        if (_deccelerationRoutine != null)
            StopCoroutine(_deccelerationRoutine);
    }

    private float GetMovementSpeedDueToRelative()
    {
        return Mathf.Max(_maxMovementSpeed, GetRigidbodySpeed()) * _currentRelativeMovementSpeed;
    }

    public float GetSpeedIncrementNeeded()
    {
        float optimal = GetMovementSpeedDueToRelative();
        float current = Math.Min(GetRigidbodySpeed(), _maxMovementSpeed);

        float increment = optimal - current;
        return increment;
    }

    public float GetForceDueToIncrementNeeded()
    {
        float speedIncrement = GetSpeedIncrementNeeded();
        float maxIncrement = _maxMovementSpeed * _counterMovementFactor;
        float mass = GetRigidbodyMass();

        //Stopping force:
        //f_r = m * (-v(t) * r) / dt 

        //v(t) = x
        //v(t + 1) = x - v(t) * r
        //v(t + 1) = x - x * r
        //v(t + 1) = v(t) * (1 - r) 

        //v(t + 1) - v(t) = v(t) * (1 - r) - v(t)
        //dv = v(t) * (1 - r - 1)
        //dv = v(t) * (-r) 

        //(1 - r) <= 0 

        //Compensation:
        //v(t + 1) -= dv
        //v(t + 1) -= -r * v(t)
        //dv = f * dt / m
        //m * dv = f * dt
        //f = m * dv / dt 

        //f_c = m * (-(-r) * v(t)) / dt
        //f_c = m * r * v(t) / dt 

        //Own movement force:
        //f_t = f_c + f_f 

        //f_f = m * (dv_f) / dt 

        //f_t = m * (r * v(t) + dv_t) / dt
        
        const float SPEED_FACTOR = 1f;
        float dv = _counterMovementFactor * GetRigidbodySpeed() + speedIncrement;
        dv = Mathf.Clamp(dv, -maxIncrement, maxIncrement);

        float force = _currentRelativeMovementSpeed
                      * SPEED_FACTOR
                      * mass
                      * dv
                      / Time.fixedDeltaTime;
        
        //_currentMovementSpeed += force / mass * Time.fixedDeltaTime;
        return force;
    }

    public float GetCounterMovementForce()
    {
        float speed = GetRigidbodySpeed();
        float mass = GetRigidbodyMass();

        //-v * m * r / dt = -a * m * r = -f * r
        float force = mass * (-speed * _counterMovementFactor) / Time.fixedDeltaTime;

        //_currentMovementSpeed += force / mass * Time.fixedDeltaTime;
        return force;
    }

    public void Initialise(Func<float> getSpeed, Func<float> getMass)
    {
        GetRigidbodySpeed = getSpeed;
        GetRigidbodyMass = getMass;
    }
}
