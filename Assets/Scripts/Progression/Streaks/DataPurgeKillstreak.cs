using UnityEngine;

public class DataPurgeKillstreak : KillstreakBase
{
    protected override void onActivate()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayNuke();
        }

        
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>();

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null)
                continue;

            // Clear the wave without awarding normal kill rewards.
            enemy.ForceKill(false);
        }
    }

    protected override void onDeactivate()
    {
        // Instant scorestreak; nothing to undo.
    }
}

