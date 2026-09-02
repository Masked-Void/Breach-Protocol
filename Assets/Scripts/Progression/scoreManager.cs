using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

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
        ResetForNewRun();
    }

    public int RegisterKill()
    {
        float stress01 =
            HeartbeatManager.instance != null
                ? HeartbeatManager.instance.getStressPercent()
                : 0f;

        float multiplier =
            Mathf.Lerp(
                1f,
                maxStressMultiplier,
                stress01
            );

        int earned =
            Mathf.RoundToInt(
                baseKillScore * multiplier
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
                    WaveManager.instance.getCurrentWave()
                );
        }

        float growth =
            baseGrowth +
            growthPerRound *
            Mathf.Max(0, currentRound - 1);

        growth =
            Mathf.Min(
                growth,
                maxGrowth
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
                firstStreakCost
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

    private void OnValidate()
    {
        baseKillScore =
            Mathf.Max(1, baseKillScore);

        maxStressMultiplier =
            Mathf.Max(1f, maxStressMultiplier);

        firstStreakCost =
            Mathf.Max(1, firstStreakCost);

        baseGrowth =
            Mathf.Max(1f, baseGrowth);

        growthPerRound =
            Mathf.Max(0f, growthPerRound);

        maxGrowth =
            Mathf.Max(
                baseGrowth,
                maxGrowth
            );
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

