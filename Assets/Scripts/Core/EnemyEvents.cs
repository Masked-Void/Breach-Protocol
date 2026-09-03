using UnityEngine;
using System;

public static class EnemyEvents
{
    public static event Action<EnemyBase> Killed;

    public static event Action<EnemyBase> ShotAtPlayer;

    public static void RaiseKilled(EnemyBase enemy)
    {
        Killed?.Invoke(enemy);
    }

    public static void RaiseShotAtPlayer(EnemyBase enemy)
    {
        ShotAtPlayer?.Invoke(enemy);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void clearOldEvents()
    {
        Killed = null;
        ShotAtPlayer = null;
    }
}