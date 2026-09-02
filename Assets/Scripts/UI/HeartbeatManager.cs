using TMPro;
using UnityEngine;

/// <summary>
/// Stress-driven BPM/health system.
/// Stress and BPM use unscaled real time so freezing the world does not freeze danger.
/// </summary>
public class HeartbeatManager : MonoBehaviour
{
    public static HeartbeatManager instance;

    [Header("BPM Settings")]
    [SerializeField] private int restingBpm = 20;
    [SerializeField] private int maxBPM = 200;

    [Header("Runtime")]
    [SerializeField] private int currentBpm;

    [Header("Stress Settings")]
    [SerializeField] private float currentStress;
    [SerializeField] private float maxStress = 100f;

    [Tooltip("Stress removed per real-world second while gameplay is active.")]
    [SerializeField] private float stressDecayRate = 2f;

    [Header("Stress Change Values")]
    [SerializeField] private float shootStress = 6f;
    [SerializeField] private float damageStress = 40f;
    [SerializeField] private float nearMissStress = 25f;
    [SerializeField] private float killStressReduction = 10f;
    [SerializeField] private float waveStressReduction = 30f;
    

    [Header("UI (Optional)")]
    [SerializeField] private TMP_Text heartRateText;
    [SerializeField] private string bpmSuffix = " BPM";

    private float stressPercent;
    private bool hasLost;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
        RefreshHeartbeat(true);
    }

    private void Update()
    {
        if (hasLost)
            return;

        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;

        DecayStress();
    }

    private void DecayStress()
    {
        if (currentStress <= 0f || stressDecayRate <= 0f)
            return;

        float newStress = Mathf.MoveTowards(
            currentStress,
            0f,
            stressDecayRate * Time.unscaledDeltaTime
        );

        SetStress(newStress);
    }

    private void SetStress(float newStress)
    {
        float clampedStress = Mathf.Clamp(newStress, 0f, maxStress);

        if (Mathf.Approximately(clampedStress, currentStress))
            return;

        currentStress = clampedStress;

        RefreshHeartbeat(true);
    }

    private void RefreshHeartbeat(bool forceUIUpdate)
    {
        stressPercent = maxStress > 0f
            ? Mathf.Clamp01(currentStress / maxStress)
            : 0f;

        int newBPM = Mathf.RoundToInt(
            Mathf.Lerp(restingBpm, maxBPM, stressPercent)
        );

        if (forceUIUpdate || newBPM != currentBpm)
        {
            currentBpm = newBPM;
            UpdateHeartRateUI();
        }

        if (!hasLost && currentBpm >= maxBPM)
            TriggerHeartFailure();
    }

    private void UpdateHeartRateUI()
    {
        if (heartRateText != null)
            heartRateText.text = currentBpm + bpmSuffix;
    }

    private void TriggerHeartFailure()
    {
        hasLost = true;

        // Timed/manual streaks must not survive death.
        if (KillstreakManager.instance != null)
            KillstreakManager.instance.CancelActiveStreak();

        if (GameManager.instance != null)
            GameManager.instance.StateLose();
    }

    public void AddStress(float amount)
    {
        if (amount <= 0f || hasLost)
            return;

        // God Mode means the player cannot gain stress while it is active.
        if (KillstreakManager.instance != null &&
            KillstreakManager.instance.IsInvulnerable)
        {
            return;
        }

        SetStress(currentStress + amount);
    }

    public void ReduceStress(float amount)
    {
        if (amount <= 0f)
            return;

        SetStress(currentStress - amount);
    }

    public void PlayerShot()
    {
        AddStress(shootStress);
    }

    public void PlayerDamaged()
    {
        // God Mode blocks hit stress completely.
        if (KillstreakManager.instance != null &&
            KillstreakManager.instance.IsInvulnerable)
        {
            return;
        }

        AddStress(damageStress);
    }

    public void NearMiss()
    {
        AddStress(nearMissStress);
    }

    public void EnemyKilled()
    {
        ReduceStress(killStressReduction);
    }

    public void WaveCompleted()
    {
        ReduceStress(waveStressReduction);
    }

    /// <summary>
    /// Cold Boot uses this. It returns stress/BPM to the resting value
    /// without resetting run/death state.
    /// </summary>
    public void ResetToRestingBpm()
    {
        if (hasLost)
            return;

        currentStress = 0f;
        RefreshHeartbeat(true);
    }

    /// <summary>
    /// Full new-run/reset function.
    /// </summary>
    public void ResetHeartbeat()
    {
        hasLost = false;
        currentStress = 0f;
        RefreshHeartbeat(true);
    }

    public int CurrentBpm => currentBpm;

    public int RestingBpm => restingBpm;

    public float StressPercent => stressPercent;

    public float CurrentStress => currentStress;

    private void OnValidate()
    {
        restingBpm = Mathf.Max(1, restingBpm);
        maxBPM = Mathf.Max(restingBpm + 1, maxBPM);
        maxStress = Mathf.Max(1f, maxStress);
        stressDecayRate = Mathf.Max(0f, stressDecayRate);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
