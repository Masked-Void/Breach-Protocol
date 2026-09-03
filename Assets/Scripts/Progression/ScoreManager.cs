using TMPro;
using UnityEngine;

/*
 * Script: ScoreManager
 *
 * Description:
 * Tracks run score and decides when the player has earned a scorestreak roll.
 * Kill score scales with current stress, so playing dangerously is worth more.
 *
 * Responsibilities:
 * - Award score per kill, scaled between base and full-stress multiplier
 * - Track the running total and the score needed for the next streak
 * - Raise the streak requirement each time one is awarded, growing per round
 * - Drive the score and streak progress HUD text
 *
 * Interacts With:
 * - ScoreConfig (all tuning, one shared asset)
 * - EnemyEvents (subscribes to Killed)
 * - HeartbeatManager (reads StressPercent for the multiplier)
 * - KillstreakManager (streak rolls)
 *
 * Notes:
 * - Not attached to anything yet. The component exists but no prefab uses it,
 *   which is why RegisterKill had no callers before the events refactor.
 * - RegisterKill returns the points earned, presumably for a floating score
 *   popup that was never built.
 */

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    [Header("Config")]
    [Tooltip("all the score and streak numbers live here, one asset shared by every level")]
    [SerializeField] private ScoreConfig config;

    [Header("Runtime")]
    [SerializeField] private int totalScore;
    [SerializeField] private int currentStreakRequirement;
    [SerializeField] private int lastAwardScoreTarget;
    [SerializeField] private int nextStreakScoreTarget;
    [SerializeField] private int currentRound = 1;

    [Header("UI")]
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text streakProgressText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (config == null)
            Debug.LogError("ScoreManager: No ScoreConfig assigned", this);

        ResetForNewRun();
    }

    private void OnEnable() => EnemyEvents.Killed += handleKill;
    private void OnDisable() => EnemyEvents.Killed -= handleKill;

    private void handleKill(EnemyBase enemy) => RegisterKill();

    public int RegisterKill()
    {
        float stress01 =
            HeartbeatManager.instance != null
                ? HeartbeatManager.instance.StressPercent
                : 0f;

        float multiplier =
            Mathf.Lerp(
                1f,
                config.fullStressMultiplier,
                stress01
            );

        int earned =
            Mathf.RoundToInt(
                config.baseKillScore * multiplier
            );

        totalScore += earned;

        TryAwardPendingStreak();
        UpdateUI();

        return earned;
    }

    public void TryAwardPendingStreak()
    {
        if (KillstreakManager.instance == null)
            return;

        if (!KillstreakManager.instance.HasOpenSlot())
        {
            UpdateUI();
            return;
        }

        if (totalScore < nextStreakScoreTarget)
        {
            UpdateUI();
            return;
        }

        if (KillstreakManager.instance.TryAwardRandomStreak())
        {
            lastAwardScoreTarget =
                nextStreakScoreTarget;

            nextStreakScoreTarget =
                lastAwardScoreTarget +
                currentStreakRequirement;
        }

        UpdateUI();
    }

    public void NotifyStreakActivated()
    {
        if (WaveManager.instance != null)
        {
            currentRound =
                Mathf.Max(
                    1,
                    WaveManager.instance.CurrentWave
                );
        }

        float growth =
            config.baseGrowthMultiplier +
            config.roundGrowthMultiplier *
            Mathf.Max(0, currentRound - 1);

        growth =
            Mathf.Min(
                growth,
                config.maxGrowthMultiplier
            );

        currentStreakRequirement =
            Mathf.CeilToInt(
                currentStreakRequirement *
                growth
            );

        nextStreakScoreTarget =
            lastAwardScoreTarget +
            currentStreakRequirement;

        UpdateUI();
    }

    public void SetCurrentRound(int roundNumber)
    {
        currentRound =
            Mathf.Max(1, roundNumber);
    }

    public void ResetForNewRun()
    {
        totalScore = 0;

        currentStreakRequirement =
            Mathf.Max(
                1,
                config.killStreakThreshold
            );

        lastAwardScoreTarget = 0;

        nextStreakScoreTarget =
            currentStreakRequirement;

        currentRound = 1;

        UpdateUI();
    }

    public int GetTotalScore()
    {
        return totalScore;
    }

    public int GetScoreTowardNextStreak()
    {
        return Mathf.Max(
            0,
            totalScore -
            lastAwardScoreTarget
        );
    }

    public int GetCurrentStreakRequirement()
    {
        return currentStreakRequirement;
    }

    public int GetNextStreakScoreTarget()
    {
        return nextStreakScoreTarget;
    }

    public float GetStreakProgress01()
    {
        if (currentStreakRequirement <= 0)
            return 0f;

        return Mathf.Clamp01(
            (float)GetScoreTowardNextStreak() /
            currentStreakRequirement
        );
    }

    private void UpdateUI()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text =
                totalScore.ToString();
        }

        if (streakProgressText != null)
        {
            streakProgressText.text =
                GetScoreTowardNextStreak() +
                " / " +
                currentStreakRequirement;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

