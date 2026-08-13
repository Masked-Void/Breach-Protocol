/*
 * Script: WaveManager
 *
 * Description:
 * Central wave coordinator. Does NOT spawn enemies itself - each
 * spawnPoint is fully individual (own prefabs, percentages, pacing,
 * and difficulty scaling). WaveManager just owns the wave number and
 * the countdown between waves, and keeps every spawnPoint in sync:
 * the next wave will not begin until every registered spawnPoint has
 * finished spawning AND every enemy from every spawnPoint is dead.
 *
 * Responsibilities:
 * - Automatically start the first wave when the level begins
 * - Wait between waves (real-time, unaffected by Time.timeScale),
 *   then tell every spawnPoint to begin the new wave
 * - Track total enemies alive across all spawn points
 * - Only complete a wave once ALL spawn points are done spawning and
 *   ALL of their enemies are dead
 * - Notify HeartbeatManager when enemies die or waves end
 * - Notify GameManager when all waves are completed
 *
 * Interacts With:
 * - spawnPoint (one or more, individually configured)
 * - heartbeatManager
 * - gameManager
 * - waveLightController
 * - audioManager
 */

using System.Collections.Generic;
using UnityEngine;

public class waveManager : MonoBehaviour
{
    public static waveManager instance;

    [Header("Wave Settings")]
    [SerializeField] private int currentWave;
    [SerializeField] private int maxWaves;
    [SerializeField] private float timeBetweenWaves;

    [Header("Roam")]
    [SerializeField] private Transform[] roamPoints;
    [Tooltip("Roaming is only for ranged enemies")]
    [SerializeField] private float roamPercent;

    [Header("Runtime")]
    [SerializeField] private int enemiesAlive;
    [SerializeField] private bool waveInProgress;

    private bool waitingForNextWave;
    private float waveTimer;

    private List<spawner> spawners = new List<spawner>();
    private int spawnersStillSpawning;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        queueNextWave();
    }

    void Update()
    {
        if(gameManager.instance != null && gameManager.instance.isPaused) return; 
        if (waitingForNextWave)
        {
            waveTimer += Time.unscaledDeltaTime;

            if (waveTimer >= timeBetweenWaves)
            {
                waitingForNextWave = false;
                startWave();
            }
        }
    }

    // Called once by each spawnPoint (in its own Start) so waveManager
    // knows it exists and should be included in wave coordination.
    public void RegisterSpawner(spawner sp)
    {
        if (!spawners.Contains(sp))
        {
            spawners.Add(sp);
        }
    }

    public void UnregisterSpawner(spawner sp)
    {
        spawners.Remove(sp);
    }

    void queueNextWave()
    {
        if(gameManager.instance != null && gameManager.instance.isPaused) return; 
        currentWave++;

        if (currentWave > maxWaves)
        {
            playerWins();
            return;
        }

        if (waveLightController.instance != null)
        {
            waveLightController.instance.FlashWarningLights(timeBetweenWaves);
        }

        if (audioManager.instance != null)
        {
            audioManager.instance.playRoundTransitionMusic();
        }

        waveTimer = 0f;
        waitingForNextWave = true;
    }

    void startWave()
    {
        if (audioManager.instance != null)
        {
            audioManager.instance.stopMusic();
        }

        waveInProgress = true;
        enemiesAlive = 0;
        spawnersStillSpawning = spawners.Count;

        // Every spawn point runs its own count/pacing/prefab logic and just
        // reports back how many enemies it committed to spawning this wave.
        foreach (spawner sp in spawners)
        {
            enemiesAlive += sp.BeginWave(currentWave);
        }

        // Edge case: no spawn points registered, or every spawn point had
        // nothing to spawn this wave - don't get stuck forever waiting.
        if (spawners.Count == 0 || (spawnersStillSpawning <= 0 && enemiesAlive <= 0))
        {
            completeWave();
        }
    }

    // Called by a spawnPoint once it has finished spawning its quota for the
    // current wave (this means "done spawning", not "all its enemies died").
    public void SpawnerFinishedSpawning(spawner sp)
    {
        spawnersStillSpawning--;

        if (spawnersStillSpawning < 0)
        {
            spawnersStillSpawning = 0;
        }

        if (spawnersStillSpawning <= 0 && enemiesAlive <= 0)
        {
            completeWave();
        }
    }

    public void enemyKilled()
    {
        enemiesAlive--;

        if (enemiesAlive < 0)
        {
            enemiesAlive = 0;
        }

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.enemyKilled();
        }

        if (spawnersStillSpawning <= 0 && enemiesAlive <= 0)
        {
            completeWave();
        }
    }

    void completeWave()
    {
        if (!waveInProgress)
        {
            return;
        }

        waveInProgress = false;

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.waveCompleted();
        }

        queueNextWave();
    }

    void playerWins()
    {
        if (gameManager.instance != null)
        {
            // Add this once your gameManager has a win menu.
            // gameManager.instance.stateWin();
        }
    }

    public int getCurrentWave()
    {
        return currentWave;
    }

    public int getEnemiesAlive()
    {
        return enemiesAlive;
    }

    public bool isWaveInProgress()
    {
        return waveInProgress;
    }

    public bool isWaitingForNextWave()
    {
        return waitingForNextWave;
    }

    public int getSecondsUntilNextWave()
    {
        float remaining = timeBetweenWaves - waveTimer;
        return Mathf.Max(0, Mathf.CeilToInt(remaining));
    }

    public Vector3 newRoamPos()
    {
        if (roamPoints.Length == 0) return Vector3.zero;
        int index = Random.Range(0, roamPoints.Length);
        Vector3 roamPos = roamPoints[index].position;
        return roamPos;
    }

}