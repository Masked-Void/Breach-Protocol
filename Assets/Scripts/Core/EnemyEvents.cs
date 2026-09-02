using UnityEngine;
using System;

public static class EnemyEvents
{
    public static event Action<EnemyBase> killed;

    public static event Action<EnemyBase> shotAtPlayer;

    public static void RaiseKilled(EnemyBase enemy)
    {
        killed?.Invoke(enemy);
    }

    public static void RaiseShotAtPlayer(EnemyBase enemy)
    {
        shotAtPlayer?.Invoke(enemy);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void clearOldEvents()
    {
        killed = null;
        shotAtPlayer = null;
    }
}