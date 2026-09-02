using UnityEngine;

public interface IWaveHost {
    void enemyKilled();
    Transform claimRoamPoint(GameObject askingEnemy);
    void releaseRoamPoint(GameObject askingEnemy);
}

public static class waveHost {
    public static IWaveHost active;
}
