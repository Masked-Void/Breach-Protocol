using UnityEngine;

// a full sequence of steps
[System.Serializable]
public class laserPattern
{
    [Tooltip("just a label for the inspector, never read at runtime")]
    public string patternName;

    [Tooltip("the sequence, runs top to bottom")]
    public laserStep[] steps;

    [Tooltip("repeat forever. looping patterns only end when stopPattern gets called")]
    public bool loops;
}
