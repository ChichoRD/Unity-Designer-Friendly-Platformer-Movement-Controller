using System;
using UnityEngine;
using UnityEngine.Events;

public interface IJumpController
{
    Func<Vector3> GetVelocity { get; }
    Func<Vector3> GetJumpDirection { get; set; }
    Func<Vector3> GetGroundDirection { get; set; }
    Func<float> GetMass { get; }
    Action<Vector3> OnForceAdd { get; }

    UnityEvent OnJump { get; }
    UnityEvent OnDescentStart { get; }

    bool PerformJump();
    bool CancelJump();
    void AdjustJumpParameters();
    void Initialise(Func<Vector3> getJumpDirection, Func<Vector3> getGroundDirection, Func<Vector3> getVelocity, Func<float> getMass, Action<Vector3> onForceAdd);
    void UpdateInfo();
    Vector3 GetGravityDifference(Vector3 defaultAcceleration);

    void DebugJumpHeights(Color minJumpHeightColor, Color maxJumpHeightColor);
    void DebugJumpArcs(Color minJumpHeightColor, Color maxJumpHeightColor, Func<Vector3> getHorizontalVelocity);
}
