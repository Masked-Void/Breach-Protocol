using UnityEngine;

/// <summary>
/// DDoS: jams enemy AI for a short real-time duration.
/// Existing projectiles continue moving, so it is control rather than invulnerability.
/// </summary>
public class ddosKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (killstreakManager.instance != null)
            killstreakManager.instance.SetEnemiesJammed(true);
    }

    protected override void onDeactivate()
    {
        if (killstreakManager.instance != null)
            killstreakManager.instance.SetEnemiesJammed(false);
    }

    private void Reset()
    {
        killstreakName = "DDoS";
        duration = 6f;
    }
}
