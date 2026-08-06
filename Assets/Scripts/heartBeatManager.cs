/*
 * Script: HeartbeatManager
 * Author: Devin Childs
 *
 * Description:
 * Controls the player's heartbeat and stress system.
 * Instead of taking traditional health damage, the player builds stress
 * through combat actions such as taking damage, firing weapons, and
 * experiencing dangerous situations. As stress increases, the player's
 * BPM (beats per minute) rises. If the BPM reaches the maximum value,
 * the player loses the game due to cardiac overload.
 *
 * Responsibilities:
 * - Track current stress level.
 * - Calculate the player's current BPM.
 * - Gradually reduce stress over time.
 * - Increase or decrease stress based on gameplay events.
 * - Notify the GameManager of BPM changes.
 * - Check the lose condition when BPM reaches its maximum.
 *
 * Interacts With:
 * - playerController (player takes damage / fires weapon)
 * - waveManager (wave completion)
 * - Enemy scripts (enemy defeated)
 * - gameManager (updates BPM UI and triggers lose state)
 *
 * Last Updated:
 * Prototype 1
 */
using UnityEngine;

public class heartbeatManager : MonoBehaviour
{
    public static heartbeatManager instance;

    [Header("BPM Settings")]
    [SerializeField] private int restingBPM;
    [SerializeField] private int maxBPM;
    [SerializeField] private int currentBPM;

    [Header("Stress Settings")]
    [SerializeField] private float currentStress;
    [SerializeField] private float maxStress;
    [SerializeField] private float stressDecayRate;

    [Header("Stress Change Values")]
    [SerializeField] private float shootStress;
    [SerializeField] private float damageStress;
    [SerializeField] private float nearMissStress;
    [SerializeField] private float killStressReduction;
    [SerializeField] private float waveStressReduction;

    private bool hasLost;

    void Awake()
    {
        // Lets other scripts call heartbeatManager.instance
        instance = this;
    }

    void Start()
    {
        // Current BPM is runtime data.
        // It starts at whatever restingBPM the designer set in Unity.
        currentBPM = restingBPM;
    }

    void Update()
    {
        if (hasLost)
            return;

        decayStress();
        updateBPM();
        checkLoseCondition();
    }

    void decayStress()
    {
        // Stress lowers over time based on the Inspector value.
        currentStress -= stressDecayRate * Time.unscaledDeltaTime;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
    }

    void updateBPM()
    {
        // Avoid divide-by-zero if maxStress was not set in Unity.
        if (maxStress <= 0)
            return;

        float stressPercent = currentStress / maxStress;

        // Converts stress into a BPM value.
        currentBPM = Mathf.RoundToInt(Mathf.Lerp(restingBPM, maxBPM, stressPercent));

    }

    void checkLoseCondition()
    {
        // Player loses when BPM reaches the designer-set max BPM.
        if (currentBPM >= maxBPM)
        {
            hasLost = true;

            if (gameManager.instance != null)
            {
                gameManager.instance.stateLose();
            }
        }
    }

    public void addStress(float amount)
    {
        currentStress += amount;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
    }

    public void reduceStress(float amount)
    {
        currentStress -= amount;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
    }

    public void playerShot()
    {
        // Call when player fires.
        addStress(shootStress);
    }

    public void playerDamaged()
    {
        // Call when player gets hit.
        // Damage raises stress instead of lowering HP.
        addStress(damageStress);
    }

    public void nearMiss()
    {
        // Optional later feature for bullets that barely miss player.
        addStress(nearMissStress);
    }

    public void enemyKilled()
    {
        // Call when enemy dies.
        reduceStress(killStressReduction);
    }

    public void waveCompleted()
    {
        // Call when player survives a full wave.
        reduceStress(waveStressReduction);
    }

    public void resetHeartbeat()
    {
        // Use when restarting the level.
        hasLost = false;
        currentStress = 0f;
        currentBPM = restingBPM;
    }

    public int getCurrentBPM()
    {
        return currentBPM;
    }

    public float getStressPercent()
    {
        if (maxStress <= 0)
            return 0f;

        return currentStress / maxStress;
    }
}
