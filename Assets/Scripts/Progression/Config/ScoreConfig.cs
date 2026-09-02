using UnityEngine;

/*
 * Script: ScoreConfig
 *
 * Description:
 * Score and scorestreak numbers. One asset, referenced by ScoreManager.
 *
 */

[CreateAssetMenu(menuName = "Config/ScoreConfig")]
public class ScoreConfig : ScriptableObject
{

    [Header("Kill Score")]
    [Tooltip("Base score for killing an enemy: default is 100")]
    public int baseKillScore = 100;

    [Tooltip("Full stress multiplies base kill score by this amount: default is 3")]
    public float fullStressMultiplier = 3f;


    [Header("Streak Thresholds")]
    [Tooltip("Score required to trigger a kill streak: default is 1000")]
    public int killStreakThreshold = 1000;

    [Tooltip("How much the required score increases for each subsequent kill streak before the per round bonus: default is 1.33f")]
    public float baseGrowthMultiplier = 1.33f;

    [Tooltip("Extra growth added per round survived: default is 0.01f")]
    public float roundGrowthMultiplier = 0.01f;

    [Tooltip("Growth never exceeds this multiplier: default is 2.0f")]
    public float maxGrowthMultiplier = 2.0f;

    // Stops a growth below 1 which would make streaks get cheaper
    private void OnValidate()
    {
        baseKillScore = Mathf.Max(0, baseKillScore);
        fullStressMultiplier = Mathf.Max(1f, fullStressMultiplier);
        killStreakThreshold = Mathf.Max(1, killStreakThreshold);
        baseGrowthMultiplier = Mathf.Max(1f, baseGrowthMultiplier);
        maxGrowthMultiplier = Mathf.Max(baseGrowthMultiplier, maxGrowthMultiplier);
    }

}
