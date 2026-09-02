using UnityEngine;

[System.Serializable]
public struct trapSetup
{
    [Header("Lasers")]

    [Tooltip("False retracts every laser and stops the lasers from spinning")]
    public bool laserActive;
    [Tooltip("Index into LaserArrayManager's pattern list. -1 runs no sequence")]
    public int laserPattern;
    public bool generatePattern;
    [Range(0f, 1f)] public float laserDifficulty;
    public int laserSpinDirection;

    [Header("Lava")]
    [Tooltip("Off drains the lava back down for this block")]
    public bool lavaActive;
    [Tooltip("How high the lava goes. 0 is drained, 1 is the high marker")]
    [Range(0f, 1f)] public float lavaLevel;

    [Header("Platforms")]
    [Tooltip("On sends every stage up, Off drops them back down")]
    public bool platformsActive;

    [Header("Cycling")]
    [Tooltip("On makes whichever of the lava and platforms are active rise and fall on a loop instead of holding one position. P4 is where this will most likely be used")]
    public bool cycleTraps;
    [Tooltip("Real seconds the cycle holds until it stops")]
    public float cycleInterval;
}