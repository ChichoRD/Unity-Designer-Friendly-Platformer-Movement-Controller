using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

//Fix too many calls to the events
public class MultiJumpConstrainer : MonoBehaviour, IJumpConstrainer
{
    [SerializeField] private Object[] _jumpConstrainerObjects;
    private IJumpConstrainer[] _jumpConstrainers;
    
    private int _jumpsRemaining;
    public int JumpsRemaining 
    { 
        get => _jumpsRemaining; 
        set 
        {
            bool couldJump = CanJump();
            _jumpsRemaining = value;
            
            if (couldJump && value <= 0)
                OnJumpAbilityLost?.Invoke();

            if (!couldJump && value > 0)
                OnJumpAbilityRestored?.Invoke();
        }
    }
    
    [field: SerializeField] public UnityEvent OnJumpAbilityRestored { get; private set; }
    [field: SerializeField] public UnityEvent OnJumpAbilityLost { get; private set; }

    public Func<Vector3> GetGroundDirection => throw new NotImplementedException();
    public Func<Vector3> GetJumpDirection => throw new NotImplementedException();

    public bool CanJump()
    {
        return JumpsRemaining > 0;
    }

    public void Initialise(IJumpController jumpController)
    {
        _jumpConstrainers = new IJumpConstrainer[_jumpConstrainerObjects.Length];
        for (int i = 0; i < _jumpConstrainerObjects.Length; i++)
        {
            Object obj = _jumpConstrainerObjects[i];
            IJumpConstrainer jumpConstrainer = _jumpConstrainers[i] = obj as IJumpConstrainer;
            jumpConstrainer.Initialise(jumpController);

            jumpConstrainer.OnJumpAbilityRestored.AddListener(IncrementJumpsRemaining);
            jumpConstrainer.OnJumpAbilityLost.AddListener(DecrementJumpsRemaining);
        }
    }

    public void IncrementJumpsRemaining(int amount)
    {
        JumpsRemaining += amount;
    }

    public void IncrementJumpsRemaining()
    {
        JumpsRemaining++;
    }

    public void DecrementJumpsRemaining(int amount)
    {
        JumpsRemaining -= amount;
    }

    public void DecrementJumpsRemaining()
    {
        JumpsRemaining--;
    }
}
