using System.Collections.Generic;
using UnityEngine;

public class packetLossKillstreak : killstreakBase
{
    [SerializeField] private float attackRateMultiplier = 2.5f;

    private readonly Dictionary<enemyBase, float> originalAttackRates =
        new Dictionary<enemyBase, float>();

    protected override void onActivate()
    {
        originalAttackRates.Clear();

        enemyBase[] enemies =
            FindObjectsByType<enemyBase>();

        foreach (enemyBase enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            originalAttackRates.Add(
                enemy,
                enemy.attackRate
            );

            // Higher attackRate means more time between attacks.
            enemy.attackRate *= attackRateMultiplier;
        }
    }

    protected override void onDeactivate()
    {
        foreach (KeyValuePair<enemyBase, float> entry
                 in originalAttackRates)
        {
            if (entry.Key == null)
                continue;

            entry.Key.attackRate = entry.Value;
        }

        originalAttackRates.Clear();
    }
}
