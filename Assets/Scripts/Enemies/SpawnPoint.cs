using UnityEngine;

public class SpawnPoint
{
    public Transform point;
    public float lastUsed;

    public bool isFree(float cooldown)
    {
        return (Time.unscaledTime - lastUsed >= cooldown);
    }
}