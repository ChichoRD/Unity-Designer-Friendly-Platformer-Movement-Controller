using UnityEngine;

public class HorizontalMovementInputter3D : HorizontalMovementInputter
{
    protected override bool HasInputAppeared(Vector2 oldInput, Vector2 newInput)
    {
        return oldInput.magnitude < InputThreshold && newInput.magnitude >= InputThreshold;
    }

    protected override bool HasInputVanished(Vector2 oldInput, Vector2 newInput)
    {
        return oldInput.magnitude >= InputThreshold && newInput.magnitude < InputThreshold;
    }
}
