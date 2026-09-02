using System.Collections.Generic;
using UnityEngine;

public class PacketLossKillstreak : KillstreakBase
{
    [SerializeField] private float attackRateMultiplier = 2.5f;

    private readonly Dictionary<EnemyBase, float> originalAttackRates =
        new Dictionary<EnemyBase, float>();

    protected override void onActivate()
    {
        originalAttackRates.Clear();

        EnemyBase[] enemies =
            FindObjectsByType<EnemyBase>();

        foreach (EnemyBase enemy in enemies)
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
        foreach (KeyValuePair<EnemyBase, float> entry
                 in originalAttackRates)
        {
            if (entry.Key == null)
                continue;

            entry.Key.attackRate = entry.Value;
        }

        originalAttackRates.Clear();
    }
}
