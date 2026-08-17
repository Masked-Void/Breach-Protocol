using UnityEngine;

public class heartbeatManager : MonoBehaviour
{
    public static heartbeatManager instance;

    [Header("BPM Settings")]
    [SerializeField] private int restingBPM = 60;
    [SerializeField] private int maxBPM = 200;

    [Header("Runtime")]
    [SerializeField] private int currentBPM;

    [Header("Stress Settings")]
    [SerializeField] private float currentStress;
    [SerializeField] private float maxStress = 100f;

    [Tooltip("Stress removed per real-world second.")]
    [SerializeField] private float stressDecayRate = 3f;

    [Header("Stress Change Values")]
    [SerializeField] private float shootStress = 4f;
    [SerializeField] private float damageStress = 20f;
    [SerializeField] private float nearMissStress = 5f;

    [SerializeField] private float killStressReduction = 5f;
    [SerializeField] private float waveStressReduction = 20f;

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
        currentStress = Mathf.Clamp(
            currentStress,
            0f,
            maxStress
        );

        RefreshHeartbeat(true);
    }

    private void Update()
    {
        if (hasLost)
            return;

        if (gameManager.instance != null &&
            gameManager.instance.isPaused)
        {
            return;
        }

        DecayStress();
    }

    private void DecayStress()
    {
        if (currentStress <= 0f ||
            stressDecayRate <= 0f)
        {
            return;
        }

        // Heartbeat is player physiology, so it continues
        // according to real-world time rather than game slow motion.
        float newStress = Mathf.MoveTowards(
            currentStress,
            0f,
            stressDecayRate * Time.unscaledDeltaTime
        );

        SetStress(newStress);
    }

    private void SetStress(float newStress)
    {
        float clampedStress = Mathf.Clamp(
            newStress,
            0f,
            maxStress
        );

        if (Mathf.Approximately(
            clampedStress,
            currentStress))
        {
            return;
        }

        currentStress = clampedStress;

        RefreshHeartbeat(true);
    }

    private void RefreshHeartbeat(bool forceUIUpdate)
    {
        if (maxStress <= 0f)
        {
            stressPercent = 0f;
        }
        else
        {
            stressPercent = Mathf.Clamp01(
                currentStress / maxStress
            );
        }

        int newBPM = Mathf.RoundToInt(
            Mathf.Lerp(
                restingBPM,
                maxBPM,
                stressPercent
            )
        );

        // Don't send UI updates every frame.
        if (forceUIUpdate || newBPM != currentBPM)
        {
            currentBPM = newBPM;

            if (gameManager.instance != null)
            {
                //gameManager.instance.updateHeartRate(currentBPM);
            }
        }

        if (!hasLost &&
            currentBPM >= maxBPM)
        {
            TriggerHeartFailure();
        }
    }

    private void TriggerHeartFailure()
    {
        hasLost = true;

        if (gameManager.instance != null)
        {
            gameManager.instance.stateLose();
        }
    }

    // STRESS API

    public void addStress(float amount)
    {
        if (amount <= 0f || hasLost)
            return;

        SetStress(
            currentStress + amount
        );
    }

    public void reduceStress(float amount)
    {
        if (amount <= 0f)
            return;

        SetStress(
            currentStress - amount
        );
    }

    // GAMEPLAY EVENTS

    public void playerShot()
    {
        addStress(shootStress);
    }

    public void playerDamaged()
    {
        addStress(damageStress);
    }

    public void nearMiss()
    {
        addStress(nearMissStress);
    }

    public void enemyKilled()
    {
        reduceStress(killStressReduction);
    }

    public void waveCompleted()
    {
        reduceStress(waveStressReduction);
    }

    public void resetHeartbeat()
    {
        hasLost = false;

        currentStress = 0f;

        RefreshHeartbeat(true);
    }

    // GETTERS

    public int getCurrentBPM()
    {
        return currentBPM;
    }

    public float getStressPercent()
    {
        // Cached instead of recalculating every timeManager frame.
        return stressPercent;
    }

    public float getCurrentStress()
    {
        return currentStress;
    }

    private void OnValidate()
    {
        restingBPM = Mathf.Max(1, restingBPM);

        maxBPM = Mathf.Max(
            restingBPM + 1,
            maxBPM
        );

        maxStress = Mathf.Max(
            1f,
            maxStress
        );

        stressDecayRate = Mathf.Max(
            0f,
            stressDecayRate
        );
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}