using UnityEngine;

public interface IWaveHost {
    void EnemyKilled();
    Transform ClaimRoamPoint(GameObject askingEnemy);
    void ReleaseRoamPoint(GameObject askingEnemy);
}

public static class waveHost {
    public static IWaveHost active;
}
