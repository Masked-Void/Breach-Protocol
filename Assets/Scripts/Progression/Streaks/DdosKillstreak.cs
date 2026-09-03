/// <summary>
/// DDoS: jams enemy AI for a short real-time duration.
/// Existing projectiles continue moving, so it is control rather than invulnerability.
/// </summary>
public class DdosKillstreak : KillstreakBase
{
    protected override void onActivate()
    {
        if (KillstreakManager.instance != null)
            KillstreakManager.instance.SetEnemiesJammed(true);
    }

    protected override void onDeactivate()
    {
        if (KillstreakManager.instance != null)
            KillstreakManager.instance.SetEnemiesJammed(false);
    }

    private void Reset()
    {
        killstreakName = "DDoS";
        duration = 6f;
    }
}
