using UnityEngine;

/// <summary>
/// GOD MODE: incoming hit stress is ignored for the duration.
/// </summary>
public class GodModeKillstreak : KillstreakBase
{
    protected override void onActivate()
    {
        if (KillstreakManager.instance != null)
            KillstreakManager.instance.SetInvulnerable(true);
    }

    protected override void onDeactivate()
    {
        if (KillstreakManager.instance != null)
            KillstreakManager.instance.SetInvulnerable(false);
    }

    private void Reset()
    {
        killstreakName = "God Mode";
        duration = 5f;
    }
}
