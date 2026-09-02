using UnityEngine;
using System;

public static class enemyEvents
{
    public static event Action<enemyBase> killed;

    public static event Action<enemyBase> shotAtPlayer;

    public static void RaiseKilled(enemyBase enemy)
    {
        killed?.Invoke(enemy);
    }

    public static void RaiseShotAtPlayer(enemyBase enemy)
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