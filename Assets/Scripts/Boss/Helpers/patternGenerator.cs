using UnityEngine;




[System.Serializable]
public class patternGenerator
{

    [Header("Pillars in Play")]
    [Tooltip("How many pillars are live at difficulty 0")]
    [Range(1, 4)] public int pillarsEasy = 1;
    [Tooltip("How many pillars are live at difficulty 1")]
    [Range(1, 4)] public int pillarsHard = 4;


    [Header("Density")]
    [Tooltip("Steps per pass at difficulty 0, x is the min and y is the max")]
    public Vector2 stepsEasy = new Vector2(3, 5);
    [Tooltip("Steps per pass at difficulty 1")]
    public Vector2 stepsHard = new Vector2(6, 10);


    [Header("Pacing")]
    [Tooltip("Real seconds between steps at difficulty 0")]
    public Vector2 delayEasy = new Vector2(0.8f, 1.6f);
    [Tooltip("Real seconds between steps at difficulty 1")]
    public Vector2 delayHard = new Vector2(.25f, .7f);


    [Header("Shape")]
    [Tooltip("How often a step retracts at difficulty 0. High keeps the arena open")]
    [Range(0f, 1f)] public float retractEasy = 0.55f;
    [Tooltip("How often a step retracts at difficulty 1. Low lets the arena fill up")]
    [Range(0f, 1f)] public float retractHard = .25f;
    [Tooltip("How often a step takes a whole pillar difficulty 0")]
    [Range(0f, 1f)] public float wholePillarEasy = .05f;
    [Tooltip("How often a step takes a whole pillar difficulty 1")]
    [Range(0f, 1f)] public float wholePillarHard = .25f;


    [Header("Variation")]
    [Tooltip("Slot curves to pick from. one gets rolled per pass, leave empty for a flat spread")]
    public AnimationCurve[] slotBiasOptions;
    [Tooltip("Random swing applied to the lerp number, so two passes at the same difficulty differ")]
    [Range(0f, 0.5f)] public float jitter = 0.15f;
}
