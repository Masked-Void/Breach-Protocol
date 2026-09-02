using UnityEngine;

// one step in a firing sequence
[System.Serializable]
public struct laserStep
{
    [Tooltip("which pillar this step hits")]
    public pillarColor color;

    // -1 is the sentinel for the whole pillar, otherwise its an index into that pillars array
    [Tooltip("which laser on the pillar. use -1 to fire the entire pillar at once")]
    public int slot;

    [Tooltip("On pulls laser back in")]
    public bool retract;

    [Tooltip("seconds to wait after this step. set it to 0 to fire together with the next one")]
    public float delay;
}

