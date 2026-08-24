using TMPro;
using UnityEngine;

/// <summary>
/// Owns cumulative run score and scorestreak thresholds.
/// There is no spendable streak currency: total score never decreases.
/// </summary>
public class scoreManager : MonoBehaviour
{
    public static scoreManager instance;

    [Header("Kill Score")]
    [SerializeField] private int baseKillScore = 100;

    [Tooltip("Full stress multiplies base kill score by this amount. 3 = 100 to 300.")]
    [SerializeField] private float maxStressMultiplier = 3f;

    [Header("Scorestreak Requirement")]
    [SerializeField] private int firstStreakCost = 1000;
    [SerializeField] private float baseGrowth = 1.33f;
    [SerializeField] private float growthPerRound = 0.01f;
    [SerializeField] private float maxGrowth = 1.60f;

    [Header("Runtime")]
    [SerializeField] private int totalScore;
    [SerializeField] private int currentStreakRequirement;
    [SerializeField] private int lastAwardScoreTarget;
    [SerializeField] private int nextStreakScoreTarget;
    [SerializeField] private int currentRound = 1;

    [Header("UI (Optional)")]
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
        ResetForNewRun();
    }

    /// <summary>
    /// Called once for a normal player kill, BEFORE heartbeat kill relief is applied.
    /// </summary>
    public int RegisterKill()
    {
        float stress01 = heartbeatManager.instance != null
            ? heartbeatManager.instance.getStressPercent()
            : 0f;

        float multiplier = Mathf.Lerp(1f, maxStressMultiplier, stress01);
        int earned = Mathf.RoundToInt(baseKillScore * multiplier);

        totalScore += earned;

        TryAwardPendingStreak();
        UpdateUI();

        return earned;
    }

    /// <summary>
    /// Awards any scorestreak whose cumulative score target has been crossed,
    /// but never exceeds the manager's three stored slots.
    /// Score earned while slots are full remains part of totalScore, so a newly
    /// opened slot can immediately receive a pending earned streak.
    /// </summary>
    public void TryAwardPendingStreak()
    {
        if (killstreakManager.instance == null)
            return;

        while (totalScore >= nextStreakScoreTarget &&
               killstreakManager.instance.HasOpenSlot())
        {
            if (!killstreakManager.instance.TryAwardRandomStreak())
                break;

            lastAwardScoreTarget = nextStreakScoreTarget;
            nextStreakScoreTarget =
                lastAwardScoreTarget + currentStreakRequirement;
        }

        UpdateUI();
    }

    /// <summary>
    /// Requirement growth happens only when a stored streak is actually USED.
    /// The current round determines the multiplier.
    /// Any not-yet-awarded target is recalculated from the last awarded target.
    /// </summary>
    public void NotifyStreakActivated()
    {
        float growth =
            baseGrowth + growthPerRound * Mathf.Max(0, currentRound - 1);

        growth = Mathf.Min(growth, maxGrowth);

        currentStreakRequirement = Mathf.CeilToInt(
            currentStreakRequirement * growth
        );

        nextStreakScoreTarget =
            lastAwardScoreTarget + currentStreakRequirement;

        UpdateUI();
    }

    public void SetCurrentRound(int roundNumber)
    {
        currentRound = Mathf.Max(1, roundNumber);
    }

    public void ResetForNewRun()
    {
        totalScore = 0;
        currentStreakRequirement = Mathf.Max(1, firstStreakCost);
        lastAwardScoreTarget = 0;
        nextStreakScoreTarget = currentStreakRequirement;
        currentRound = 1;

        UpdateUI();
    }

    public int GetTotalScore()
    {
        return totalScore;
    }

    public int GetScoreTowardNextStreak()
    {
        return Mathf.Max(0, totalScore - lastAwardScoreTarget);
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
            (float)GetScoreTowardNextStreak() / currentStreakRequirement
        );
    }

    private void UpdateUI()
    {
        if (totalScoreText != null)
            totalScoreText.text = totalScore.ToString();

        if (streakProgressText != null)
        {
            streakProgressText.text =
                GetScoreTowardNextStreak() +
                " / " +
                currentStreakRequirement;
        }
    }

    private void OnValidate()
    {
        baseKillScore = Mathf.Max(1, baseKillScore);
        maxStressMultiplier = Mathf.Max(1f, maxStressMultiplier);
        firstStreakCost = Mathf.Max(1, firstStreakCost);
        baseGrowth = Mathf.Max(1f, baseGrowth);
        growthPerRound = Mathf.Max(0f, growthPerRound);
        maxGrowth = Mathf.Max(baseGrowth, maxGrowth);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

