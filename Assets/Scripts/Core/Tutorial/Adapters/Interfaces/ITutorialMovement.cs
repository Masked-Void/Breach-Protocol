using UnityEngine;

// what the tutorial needs from the player controller and input
public interface ITutorialMovement
{
    // raw movement input, not velocity, so pushing into a wall still counts
    Vector2 MoveInput { get; }

    // stops movement without stopping the camera
    void SetMovementLocked(bool shouldLock);

    // moves the player instantly, for space restarts and falls
    void TeleportTo(Vector3 position, Quaternion rotation);
}
