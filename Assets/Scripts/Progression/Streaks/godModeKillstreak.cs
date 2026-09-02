using UnityEngine;

/// <summary>
/// GOD MODE: incoming hit stress is ignored for the duration.
/// </summary>
public class godModeKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (killstreakManager.instance != null)
            killstreakManager.instance.SetInvulnerable(true);
    }

    protected override void onDeactivate()
    {
        if (killstreakManager.instance != null)
            killstreakManager.instance.SetInvulnerable(false);
    }

    private void Reset()
    {
        killstreakName = "God Mode";
        duration = 5f;
    }
}
