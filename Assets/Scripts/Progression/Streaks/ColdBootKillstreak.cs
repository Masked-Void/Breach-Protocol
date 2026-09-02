using UnityEngine;

/// <summary>
/// COLD BOOT: immediately drops stress to zero and returns BPM to 20/resting.
/// </summary>
public class ColdBootKillstreak : KillstreakBase
{
    protected override void onActivate()
    {
        if (HeartbeatManager.instance != null)
            HeartbeatManager.instance.resetToRestingBPM();
    }

    protected override void onDeactivate()
    {
    }

    private void Reset()
    {
        killstreakName = "Cold Boot";
        duration = 0f;
    }
}
