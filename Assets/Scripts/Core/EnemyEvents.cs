using UnityEngine;
using System;

/*
 * Script: EnemyEvents
 *
 * Description:
 * Central place enemies announce things. Systems subscribe here instead of
 * enemies reaching into every manager, so adding a new consumer never means
 * editing EnemyBase. Static on purpose, there is nothing to put in a scene.
 *
 * Responsibilities:
 * - Raise Killed when an enemy dies in a way that should reward the player
 * - Raise NearMissedPlayer when an enemy shot passes close (not yet implemented)
 * - Clear subscribers on play so a skipped domain reload doesn't leave dead ones
 *
 * Interacts With:
 * - EnemyBase (raises)
 * - GameManager, ScoreManager, KillChainManager, ChallengeManager (subscribe)
 */


public static class EnemyEvents
{
    public static event Action<EnemyBase> Killed;

    public static event Action<EnemyBase> NearMissedPlayer;

    public static void RaiseKilled(EnemyBase enemy)
    {
        Killed?.Invoke(enemy);
    }

    public static void RaiseNearMissedPlayer(EnemyBase enemy)
    {
        NearMissedPlayer?.Invoke(enemy);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void clearOldEvents()
    {
        Killed = null;
        NearMissedPlayer = null;
    }
}