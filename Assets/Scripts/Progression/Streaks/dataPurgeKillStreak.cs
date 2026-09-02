using UnityEngine;

public class dataPurgeKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (audioManager.instance != null)
        {
            audioManager.instance.playNuke();
        }

        
        enemyBase[] enemies = FindObjectsByType<enemyBase>();

        foreach (enemyBase enemy in enemies)
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

