using UnityEngine;

/// <summary>
/// GHOST PROTOCOL: corrupts ranged-enemy aim while active.
/// </summary>
public class GhostProtocolKillstreak : KillstreakBase
{
    [Header("Ghost Protocol")]
    [Tooltip("how far off target enemy shots go while active, in degrees")]
    [SerializeField] private float enemyAimErrorDegrees = 12f;

    protected override void onActivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetGhostProtocol(
                true,
                enemyAimErrorDegrees
            );
        }
    }

    protected override void onDeactivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetGhostProtocol(
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

