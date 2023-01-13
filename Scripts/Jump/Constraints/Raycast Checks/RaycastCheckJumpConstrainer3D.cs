using UnityEngine;

public class RaycastCheckJumpConstrainer3D : RaycastCheckJumpConstrainer
{
    public override bool RaycastCheck(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal)
    {
        bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, distance, layerMask);
        normal = hitInfo.normal;
        return hit;
    }
}