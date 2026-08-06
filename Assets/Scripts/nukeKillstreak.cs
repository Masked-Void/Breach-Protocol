using UnityEngine;

public class nukeKillstreak : killstreakBase
{
    protected override void onActivate()
    {
        if (audioManager.instance != null)
        {
            audioManager.instance.playNuke();
        }

        // Prevent all enemies killed by the nuke from adding
        // to the current kill chain.
        if (killChainManager.instance != null)
        {
            killChainManager.instance.SetIgnoreRegisteredKills(true);
        }

        // Finds all currently active EnemyBase components.
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>();

        int enemiesKilled = 0;

        foreach (EnemyBase enemy in enemies)
        {
            if (enemy == null)
                continue;

            enemy.ForceKill();
        }

        // Return the kill-chain system to normal after the nuke finishes.
        if (killChainManager.instance != null)
        {
            killChainManager.instance.SetIgnoreRegisteredKills(false);
        }

        // Debug.Log(
        //     "Nuke activated. Enemies killed: " + enemiesKilled
        // );
    }

    protected override void onDeactivate()
    {
        // The Nuke is an immediate effect, so there is nothing to undo.
    }
}
