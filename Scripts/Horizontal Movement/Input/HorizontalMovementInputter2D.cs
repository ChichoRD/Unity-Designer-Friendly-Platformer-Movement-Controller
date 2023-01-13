using UnityEngine;

public class HorizontalMovementInputter2D : HorizontalMovementInputter
{
    protected override bool HasInputAppeared(Vector2 oldInput, Vector2 newInput)
    {
        return Mathf.Abs(oldInput.x) < InputThreshold && Mathf.Abs(newInput.x) >= InputThreshold;
    }

    protected override bool HasInputVanished(Vector2 oldInput, Vector2 newInput)
    {
        return Mathf.Abs(oldInput.x) >= InputThreshold && Mathf.Abs(newInput.x) < InputThreshold;
    }
}
