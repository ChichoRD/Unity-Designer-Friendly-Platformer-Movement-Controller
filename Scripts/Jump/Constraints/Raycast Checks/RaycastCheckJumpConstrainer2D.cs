using UnityEngine;

public class RaycastCheckJumpConstrainer2D : RaycastCheckJumpConstrainer
{
    public override bool RaycastCheck(Vector3 origin, Vector3 direction, float distance, LayerMask layerMask, out Vector3 normal)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layerMask);
        normal = hit.normal;
        return hit;
    }
}
