using UnityEngine;

// what a wave system has to provide so enemies can report in and claim roam points.
// WaveManager runs normal levels, BossWaveManager runs the boss arena.
public interface IWaveHost
{
    void EnemyKilled();
    Transform ClaimRoamPoint(GameObject askingEnemy);
    void ReleaseRoamPoint(GameObject askingEnemy);
}

// enemies read this instead of a singleton, so the boss arena can swap in its own
// wave system without every enemy knowing which one it is
public static class waveHost
{
    public static IWaveHost active;
}