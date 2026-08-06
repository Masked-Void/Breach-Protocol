/*
 * Script: WaveManager
 * Author: Devin Childs
 *
 * Description:
 * Controls enemy wave spawning and difficulty progression.
 * Each wave increases the total number of enemies by a percentage.
 * Enemy types are chosen by percentage so most enemies are ranged,
 * some are basic melee enemies, and the fewest are heavy enemies.
 *
 * Responsibilities:
 * - Start new waves
 * - Spawn enemies at spawn points
 * - Increase enemy count by percentage each wave
 * - Spawn weighted enemy types
 * - Track enemies alive
 * - Notify HeartbeatManager when enemies die or waves end
 * - Notify GameManager when all waves are completed
 *
 * Interacts With:
 * - EnemyBase
 * - BasicEnemy
 * - RangedEnemy
 * - heavyEnemy
 * - heartbeatManager
 * - gameManager
 *
 * Last Updated:
 * Prototype 1
 */

using System.Collections;
using UnityEngine;

public class waveManager : MonoBehaviour
{
    public static waveManager instance;

    [Header("Wave Settings")]
    [SerializeField] public int currentWave;
    [SerializeField] private int maxWaves;
    [SerializeField] private int startingEnemies;

    [Tooltip("Example: 25 means each wave has 25% more enemies than the last.")]
    [SerializeField] private float enemyIncreasePercent;

    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private float timeBetweenSpawns;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject rangedEnemyPrefab;
    [SerializeField] private GameObject heavyEnemyPrefab;

    [Header("Enemy Spawn Percentages")]
    [Tooltip("Largest percent. Example: 60")]
    [SerializeField] private float rangedEnemyPercent;

    [Tooltip("Middle percent. Example: 30")]
    [SerializeField] private float basicEnemyPercent;

    [Tooltip("Lowest percent. Example: 10")]
    [SerializeField] private float heavyEnemyPercent;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Roam")]
    [SerializeField] private Transform[] roamPoints;
    [Tooltip("Roaming is only for ranged enemies")]
    [SerializeField] private float roamPercent;

    [Header("Runtime")]
    [SerializeField] private int enemiesAlive;
    [SerializeField] private int enemiesSpawnedThisWave;
    [SerializeField] private bool waveInProgress;

    private bool isSpawning;

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
        StartCoroutine(beginNextWave());
    }

    IEnumerator beginNextWave()
    {
        // Flash the warning lights while waiting for the next wave.
        if (waveLightController.instance != null)
        {
            waveLightController.instance.FlashWarningLights(timeBetweenWaves);
        }

        if (audioManager.instance != null)
        {
            audioManager.instance.playRoundTransitionMusic();
        }

        // Wait for the warning sequence to finish.
        yield return new WaitForSecondsRealtime(timeBetweenWaves);

        if (audioManager.instance != null) audioManager.instance.stopMusic();

        currentWave++;

        if (currentWave > maxWaves)
        {
            playerWins();
            yield break;
        }

        waveInProgress = true;
        isSpawning = true;
        enemiesSpawnedThisWave = 0;

        int enemiesToSpawn = getEnemyCountForWave();

        enemiesAlive = enemiesToSpawn;

        //Debug.Log("Wave " + currentWave + " started. Enemies: " + enemiesToSpawn);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            spawnEnemy();
            enemiesSpawnedThisWave++;

            yield return new WaitForSecondsRealtime(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    void spawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            //Debug.LogWarning("WaveManager has no spawn points assigned.");
            return;
        }

        GameObject enemyToSpawn = chooseEnemyPrefab();


        if (enemyToSpawn == null)
        {
            //Debug.LogWarning("WaveManager is missing an enemy prefab.");
            return;
        }


        Transform chosenSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemyObj = Instantiate(enemyToSpawn, chosenSpawn.position, chosenSpawn.rotation);
        EnemyBase eb = enemyObj.GetComponent<EnemyBase>();

        if (enemyToSpawn == rangedEnemyPrefab)
        {
            float randomValue = Random.Range(0f, 1f);
            if (randomValue < roamPercent)
            {
                eb.willRoam = true;
            }

        }
    }

    public Vector3 newRoamPos()
    {
        if (roamPoints == null || roamPoints.Length == 0) return transform.position;

        return roamPoints[Random.Range(0, roamPoints.Length)].position;
    }

    GameObject chooseEnemyPrefab()
    {
        float totalPercent = rangedEnemyPercent + basicEnemyPercent + heavyEnemyPercent;

        if (totalPercent <= 0)
        {
            //Debug.LogWarning("Enemy spawn percentages are not set.");
            return null;
        }

        float randomValue = Random.Range(0, totalPercent);

        if (randomValue < rangedEnemyPercent)
        {
            return rangedEnemyPrefab;
        }

        randomValue -= rangedEnemyPercent;

        if (randomValue < basicEnemyPercent)
        {
            return basicEnemyPrefab;
        }

        return heavyEnemyPrefab;
    }

    int getEnemyCountForWave()
    {
        /*
         * Example with:
         * startingEnemies = 5
         * enemyIncreasePercent = 25
         *
         * Wave 1 = 5
         * Wave 2 = 7
         * Wave 3 = 8
         * Wave 4 = 10
         */

        float increaseMultiplier = 1 + (enemyIncreasePercent / 100f);

        float enemyCount = startingEnemies * Mathf.Pow(increaseMultiplier, currentWave - 1);

        return Mathf.CeilToInt(enemyCount);
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

        //Debug.Log("Enemy killed. Enemies alive: " + enemiesAlive);

        if (!isSpawning && enemiesAlive <= 0)
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

        //Debug.Log("Wave " + currentWave + " completed.");

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.waveCompleted();
        }

        StartCoroutine(beginNextWave());
    }

    void playerWins()
    {
        //Debug.Log("All waves completed. Player wins.");

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
}
