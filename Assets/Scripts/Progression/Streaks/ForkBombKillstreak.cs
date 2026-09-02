using UnityEngine;

/// <summary>
/// FORK BOMB: damage to one enemy propagates a percentage of that damage
/// to nearby enemies. Secondary hits do not recursively fork again.
/// </summary>
public class ForkBombKillstreak : KillstreakBase
{
    [Header("Fork Bomb")]
    [SerializeField] private float spreadRadius = 4f;
    [SerializeField, Range(0.05f, 1f)] private float damagePercent = 0.5f;

    protected override void onActivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetChainReaction(
                true,
                spreadRadius,
                damagePercent
            );
        }
    }

    protected override void onDeactivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetChainReaction(
                false,
                spreadRadius,
                damagePercent
            );
        }
    }

    private void Reset()
    {
        killstreakName = "Fork Bomb";
        duration = 10f;
    }
}

