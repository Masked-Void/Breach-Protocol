using UnityEngine;

public class SpawnPoint
{
    public Transform point;
    public float lastUsed;

    public bool IsFree(float cooldown)
    {
        return (Time.unscaledTime - lastUsed >= cooldown);
    }
}