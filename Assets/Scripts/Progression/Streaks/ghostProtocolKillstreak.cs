using UnityEngine;

/// <summary>
/// GHOST PROTOCOL: corrupts ranged-enemy aim while active.
/// </summary>
public class ghostProtocolKillstreak : killstreakBase
{
    [Header("Ghost Protocol")]
    [SerializeField] private float enemyAimErrorDegrees = 12f;

    protected override void onActivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetGhostProtocol(
                true,
                enemyAimErrorDegrees
            );
        }
    }

    protected override void onDeactivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetGhostProtocol(
                false,
                enemyAimErrorDegrees
            );
        }
    }

    private void Reset()
    {
        killstreakName = "Ghost Protocol";
        duration = 10f;
    }
}

