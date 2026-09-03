using UnityEngine;

// Sub class holding one block of spawn numbers, one for each phase and each transition
[System.Serializable]
public struct waveSetup
{
    [Tooltip("Weights, not real percents. They are rolled against their own total so they do not have to add to 100")]
    public float basicEnemyPercent;

    [Tooltip("Weight for heavy enemies, rolled against the total of all three")]
    public float heavyEnemyPercent;

    [Tooltip("Weight for ranged enemies, rolled against the total of all three")]
    public float rangedEnemyPercent;
    [Tooltip("Ceiling on how many enemies can be alive at once")]
    public int maxEnemiesOnMap;
    [Tooltip("How many spawn per burst, capped by the room left under maxEnemiesOnMap")]
    public int maxSpawnCount;
    [Tooltip("Real seconds between bursts")]
    public float timeBetweenBursts;
}
