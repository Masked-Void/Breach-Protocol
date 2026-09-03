using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PACKET LOSS: slows every enemy's attack rate for the duration, then puts
/// each one back to what it was.
/// </summary>
public class PacketLossKillstreak : KillstreakBase
{
    [Header("Packet Loss")]
    [Tooltip("attack rate is multiplied by this, higher means longer between enemy attacks")]
    [SerializeField] private float attackRateMultiplier = 2.5f;

    // what each enemy's rate was before we slowed it, so it can be restored
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

    private void Reset()
    {
        killstreakName = "Packet Loss";
        duration = 8f;
    }
}
