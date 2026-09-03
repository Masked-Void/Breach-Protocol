using UnityEngine;
using System;

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