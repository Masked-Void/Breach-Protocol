using UnityEngine;

/// <summary>
/// DATA PURGE: instantly deletes all currently spawned regular enemies.
/// These forced kills clear the wave but do not award score or kill credit.
/// </summary>
public class dataPurgeKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (audioManager.instance != null)
            audioManager.instance.playNuke();

        EnemyBase[] enemies =
            FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].ForceKill(false);
        }
    }

    protected override void onDeactivate()
    {
    }

    private void Reset()
    {
        killstreakName = "Data Purge";
        duration = 0f;
    }
}
