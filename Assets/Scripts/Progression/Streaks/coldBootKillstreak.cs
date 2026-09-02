using UnityEngine;

/// <summary>
/// COLD BOOT: immediately drops stress to zero and returns BPM to 20/resting.
/// </summary>
public class coldBootKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (heartbeatManager.instance != null)
            heartbeatManager.instance.resetToRestingBPM();
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
