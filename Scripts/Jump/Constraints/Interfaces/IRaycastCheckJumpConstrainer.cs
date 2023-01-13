using System;
using UnityEngine;

internal interface IRaycastCheckJumpConstrainer : IJumpConstrainer
{
    Vector3 LastNormal { get; }
    float LastNormalAngle { get; }
    LayerMask JumpableLayers { get; }
    float RaycastsLength { get; }
    bool IsOnValidGround { get; }
    public void GenerateRayField(Func<Ray, bool> onRayShortcircuit);
}
