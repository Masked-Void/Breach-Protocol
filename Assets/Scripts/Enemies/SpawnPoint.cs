using UnityEngine;

// one spawn location in a level. tracks when it last spawned something so the
// wave manager can spread spawns out instead of dumping everything in one door.
public class SpawnPoint
{
    // where the enemy appears
    public Transform point;

    // unscaled time of the last spawn, so the cooldown is real seconds
    public float lastUsed;

    // true once enough real time has passed since the last spawn here
    public bool IsFree(float cooldown)
    {
        return (Time.unscaledTime - lastUsed >= cooldown);
    }
}