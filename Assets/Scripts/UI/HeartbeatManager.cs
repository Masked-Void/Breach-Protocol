using TMPro;
using UnityEngine;

/*
 * Script: HeartbeatManager
 *
 * Description:
 * The health system. Per the GDD there is no HP pool for the player, only a
 * BPM value driven by stress. Stress rises from firing, taking damage and near
 * misses, and decays over time. Hitting max BPM is death.
 *
 * Stress and BPM use unscaled time so freezing the world does not freeze danger.
 *
 * Responsibilities:
 * - Hold current stress and convert it to a BPM between resting and max
 * - Add stress when the player fires, takes damage or is nearly hit
 * - Reduce stress on kills and at the end of a wave
 * - Decay stress every second while nothing is happening
 * - Trigger the lose state when BPM reaches max
 *
 * Interacts With:
 * - StressConfig (all tuning, one shared asset)
 * - WeaponManager (calls PlayerShot when the player fires)
 * - PlayerController (calls PlayerDamaged)
 * - WaveManager, BossWaveManager (call EnemyKilled and WaveCompleted)
 * - HeartbeatUI (reads CurrentBpm for the display)
 * - GameManager (lose state)
 *
 * Notes:
 * - shootStress is the player firing, not being fired at. Acting raises your
 *   heart rate, which is the SUPERHOT-style pressure the design wants.
 * - NearMiss has no callers yet. Near miss detection is not implemented.
 * - This lives in the UI folder but it is gameplay state, not display. It sits
 *   inside GameManager-Base along with everything else.
 */


/// <summary>
/// Stress-driven BPM/health system.
/// Stress and BPM use unscaled real time so freezing the world does not freeze danger.
/// </summary>
public class HeartbeatManager : MonoBehaviour
{
    public static HeartbeatManager instance;

    [Header("Config")]
    [Tooltip("All the numbers that drive the stress/BPM system.")]
    [SerializeField] private StressConfig config;

    [Header("Runtime State")]
    [SerializeField] private int currentBpm;
    [SerializeField] private float currentStress;

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

        // no config means every stress number is missing, fail loudly here
        if (config == null)
            Debug.LogError("HeartbeatManager: No StressConfig assigned",this);
    }

    private void Start()
    {
        currentStress = Mathf.Clamp(currentStress, 0f, config.maxStress);
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
        if (currentStress <= 0f || config.stressDecayRate <= 0f)
            return;

        float newStress = Mathf.MoveTowards(
            currentStress,
            0f,
            config.stressDecayRate * Time.unscaledDeltaTime
        );

        SetStress(newStress);
    }

    private void SetStress(float newStress)
    {
        float clampedStress = Mathf.Clamp(newStress, 0f, config.maxStress);

        if (Mathf.Approximately(clampedStress, currentStress))
            return;

        currentStress = clampedStress;

        RefreshHeartbeat(true);
    }

    private void RefreshHeartbeat(bool forceUIUpdate)
    {
        stressPercent = config.maxStress > 0f
            ? Mathf.Clamp01(currentStress / config.maxStress)
            : 0f;

        int newBPM = Mathf.RoundToInt(
            Mathf.Lerp(config.restingBpm, config.maxBpm, stressPercent)
        );

        if (forceUIUpdate || newBPM != currentBpm)
        {
            currentBpm = newBPM;
            UpdateHeartRateUI();
        }

        if (!hasLost && currentBpm >= config.maxBpm)
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
        AddStress(config.shootingStress);
    }

    public void PlayerDamaged()
    {
        // God Mode blocks hit stress completely.
        if (KillstreakManager.instance != null &&
            KillstreakManager.instance.IsInvulnerable)
        {
            return;
        }

        AddStress(config.damagedStress);
    }

    public void NearMiss()
    {
        AddStress(config.nearMissStress);
    }

    public void EnemyKilled()
    {
        ReduceStress(config.killStressRelief);
    }

    public void WaveCompleted()
    {
        ReduceStress(config.waveEndStressRelief);
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

    public int RestingBpm => config.restingBpm;

    public float StressPercent => stressPercent;

    public float CurrentStress => currentStress;


    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
