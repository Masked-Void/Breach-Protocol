using UnityEngine;

/// <summary>
/// FORK BOMB: damage to one enemy propagates a percentage of that damage
/// to nearby enemies. Secondary hits do not recursively fork again.
/// </summary>
public class forkBombKillstreak : killstreakBase
{
    [Header("Fork Bomb")]
    [SerializeField] private float spreadRadius = 4f;
    [SerializeField, Range(0.05f, 1f)] private float damagePercent = 0.5f;

    protected override void onActivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetChainReaction(
                true,
                spreadRadius,
                damagePercent
            );
        }
    }

    protected override void onDeactivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetChainReaction(
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

