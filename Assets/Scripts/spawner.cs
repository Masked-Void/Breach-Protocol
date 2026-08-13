/*
 * Script: spawnPoint
 *
 * Description:
 * A fully individual enemy spawn point. Each spawnPoint has its own
 * prefabs, spawn percentages, spawn area, pacing, and difficulty
 * scaling - completely independent of any other spawnPoint in the
 * scene. It does not decide when waves start or end; it just spawns
 * its own quota whenever waveManager tells it to, and reports back
 * once it's done.
 *
 * Interacts With:
 * - waveManager (registers itself, gets told when to begin a wave,
 *   reports back when spawning is finished)
 * - EnemyBase (spawned enemies notify waveManager directly on death)
 */

using UnityEngine;
using UnityEngine.AI;

public class spawner : MonoBehaviour
{
    [Header("Difficulty Scaling")]
    [SerializeField] private int startingEnemies;

    [Tooltip("Example: 25 means each wave THIS spawn point has 25% more enemies than the last.")]
    [SerializeField] private float enemyIncreasePercent;

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

    [Header("Spawn Area")]
    [Tooltip("Enemies spawn on the NavMesh somewhere inside this radius of this spawn point.")]
    [SerializeField] private float spawnDist;
    [SerializeField] private ParticleSystem spawnEffect;

    [Header("Runtime")]
    [SerializeField] private int enemiesToSpawnThisWave;
    [SerializeField] private int enemiesSpawnedThisWave;
    [SerializeField] private bool isSpawning;

    private float spawnTimer;

    void Start()
    {
        if (waveManager.instance != null)
        {
            waveManager.instance.RegisterSpawner(this);
        }
    }

    void OnDestroy()
    {
        if (waveManager.instance != null)
        {
            waveManager.instance.UnregisterSpawner(this);
        }
    }

    void Update()
    {
        if (!isSpawning)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (enemiesSpawnedThisWave < enemiesToSpawnThisWave && spawnTimer >= timeBetweenSpawns)
        {
            spawn();
        }

        if (enemiesSpawnedThisWave >= enemiesToSpawnThisWave)
        {
            isSpawning = false;

            if (waveManager.instance != null)
            {
                waveManager.instance.SpawnerFinishedSpawning(this);
            }
        }
    }

    public int BeginWave(int waveNumber)
    {
        enemiesSpawnedThisWave = 0;
        enemiesToSpawnThisWave = getEnemyCountForWave(waveNumber);
        spawnTimer = timeBetweenSpawns; // spawn the first enemy right away
        isSpawning = enemiesToSpawnThisWave > 0;

        if (!isSpawning && waveManager.instance != null)
        {
            waveManager.instance.SpawnerFinishedSpawning(this);
        }

        return enemiesToSpawnThisWave;
    }

    void spawn()
    {
        spawnTimer = 0f;
        enemiesSpawnedThisWave++;

        GameObject enemyToSpawn = chooseEnemyPrefab();

        if (enemyToSpawn == null)
        {
            return;
        }
        
        Vector3 ranPos = Random.insideUnitSphere * spawnDist;
        ranPos += transform.position;

        NavMeshHit hit;
        NavMesh.SamplePosition(ranPos, out hit, spawnDist, 1);

        Instantiate(enemyToSpawn, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0));

        if (spawnEffect != null)
        {
            Instantiate(spawnEffect, hit.position, Quaternion.identity);
        }
    }

    GameObject chooseEnemyPrefab()
    {
        float totalPercent = rangedEnemyPercent + basicEnemyPercent + heavyEnemyPercent;

        if (totalPercent <= 0)
        {
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

    int getEnemyCountForWave(int waveNumber)
    {
        float increaseMultiplier = 1 + (enemyIncreasePercent / 100f);
        float enemyCount = startingEnemies * Mathf.Pow(increaseMultiplier, waveNumber - 1);
        return Mathf.CeilToInt(enemyCount);
    }
}