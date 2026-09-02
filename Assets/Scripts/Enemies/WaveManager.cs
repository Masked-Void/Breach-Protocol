/*
 * Script: WaveManager
 *
 * Description:
 * Central wave coordinator. Does NOT spawn enemies itself - each
 * spawner is fully individual (own prefabs, percentages, pacing,
 * and difficulty scaling). WaveManager just owns the wave number and
 * the countdown between waves, and keeps every spawner in sync:
 * the next wave will not begin until every registered spawner has
 * finished spawning AND every enemy from every spawner is dead.
 *
 * Responsibilities:
 * - Automatically start the first wave when the level begins
 * - Wait between waves (real-time, unaffected by Time.timeScale),
 *   then tell every spawner to begin the new wave
 * - Track total enemies alive across all spawn points
 * - Only complete a wave once ALL spawn points are done spawning and
 *   ALL of their enemies are dead
 * - Notify HeartbeatManager when enemies die or waves end
 * - Notify GameManager when all waves are completed
 *
 * Interacts With:
 * - spawner (one or more, individually configured)
 * - HeartbeatManager
 * - GameManager
 * - WaveLightController
 * - AudioManager
 */

using System.Collections;
using UnityEngine;



public class WaveManager : MonoBehaviour,IWaveHost
{
    public static WaveManager instance;

    [Header("Weapon Prefabs")]
    [SerializeField] GameObject[] basicWeaponPrefabs;
    [SerializeField] GameObject[] heavyWeaponPrefabs;
    [SerializeField] GameObject[] rangedWeaponPrefabs;

    private int enemiesAlive;
    private int enemiesKilled;
    private bool waveInProgress;
    [Header("Enemy Prefabs")]
    [SerializeField] GameObject basicEnemyPrefabs;
    [SerializeField] GameObject heavyEnemyPrefabs;
    [SerializeField] GameObject rangedEnemyPrefabs;

    [Header("Roam and Spawn Points")]
    [SerializeField] Transform[] roamPointTransforms;
    [SerializeField] Transform[] spawnPointTransforms;

    private RoamPoint[] roamPoints;
    private SpawnPoint[] spawnPoints;

    [Header("Roam Settings")]
    [Tooltip("Chance that a ranged enemy will roam before engaging.")]
    [SerializeField] float giveWillRoamChance;

    [Header("Spawn Settings")]
    [SerializeField] int enemiesToSpawnAtWave0;
    [SerializeField] float enemyIncreaseMultiplier;

    [HideInInspector] EnemyType typeSpawned;

    [Header("Enemy Percent To Spawn")]
    [SerializeField] int basicEnemyPercent;
    [SerializeField] int heavyEnemyPercent;
    [SerializeField] int rangedEnemyPercent;

    [Header("Timers")]
    [SerializeField] float timeBetweenSpawns;
    [SerializeField] int timeBetweenWaves;
    [SerializeField] float waveTimer;
    [SerializeField] float spawnPointCooldown = 5f;

    [Header("Misc")]
    [SerializeField] int maxWaves;
    [SerializeField] bool waitingForNextWave;

    [Header("Wave Tracking")]
    [SerializeField] private int currentWave = 0;

    private Coroutine spawnRoutine;

    private int spawnersStillSpawning;


    public int EnemiesAlive => enemiesAlive;
    public int EnemiesKilled => enemiesKilled;
    public int CurrentWave => currentWave;
    public bool IsWaveInProgress => waveInProgress;
    public bool IsWaitingForNextWave => waitingForNextWave;
    public int SecondsUntilNextWave => Mathf.Max(0, Mathf.CeilToInt(timeBetweenWaves - waveTimer));




    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        waveHost.active = this;

        assignSpawnPoints(spawnPointTransforms);
        assignRoamPoints(roamPointTransforms);

    }


    void Start()
    {
        queueNextWave();
    }


    void Update()
    {
        if (GameManager.instance != null &&
            GameManager.instance.isPaused)
        {
            return;
        }

        if (!waitingForNextWave)
            return;

        waveTimer += Time.unscaledDeltaTime;

        if (waveTimer >= timeBetweenWaves)
        {
            waitingForNextWave = false;
            startWave();
        }
    }

    private void OnDestroy() {
        if (instance == this)
            instance = null;

        if (ReferenceEquals(waveHost.active , this)) {
            waveHost.active = null;
        }
    }
    private IEnumerator spawn()
    {
        int amountToSpawn = Mathf.RoundToInt(
            enemiesToSpawnAtWave0 *
            Mathf.Pow(enemyIncreaseMultiplier, currentWave)
        );

        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject enemyPrefab = chooseEnemyPrefab();

            if (enemyPrefab == null)
                continue;

            SpawnPoint point = getSpawnPoint();

            if (point == null)
                break;

            GameObject enemy = Instantiate(
                enemyPrefab,
                point.point.position,
                point.point.rotation
            );

            if (typeSpawned == EnemyType.ranged)
            {
                if (enemy.TryGetComponent<EnemyBase>(
                    out EnemyBase enemyScript))
                {
                    enemyScript.willRoam =
                        Random.Range(0f, 1f) <= giveWillRoamChance;
                }
            }

            point.lastUsed = Time.unscaledTime;

            enemiesAlive++;

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        spawnRoutine = null;

        if (enemiesAlive <= 0)
        {
            completeWave();
        }
    }


    GameObject chooseEnemyPrefab()
    {
        float totalPercent =
            rangedEnemyPercent +
            basicEnemyPercent +
            heavyEnemyPercent;

        if (totalPercent <= 0)
            return null;

        float randomValue = Random.Range(0f, totalPercent);

        if (randomValue < rangedEnemyPercent)
        {
            typeSpawned = EnemyType.ranged;
            return rangedEnemyPrefabs;
        }

        randomValue -= rangedEnemyPercent;

        if (randomValue < basicEnemyPercent)
        {
            typeSpawned = EnemyType.basic;
            return basicEnemyPrefabs;
        }

        typeSpawned = EnemyType.heavy;
        return heavyEnemyPrefabs;
    }


    private SpawnPoint getSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint candidate =
                spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (candidate.IsFree(spawnPointCooldown))
            {
                return candidate;
            }
        }

        return spawnPoints[startIndex];
    }


    private void startWave()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopMusic();
        }

        waveInProgress = true;
        enemiesAlive = 0;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(spawn());
    }


    private void queueNextWave()
    {
        currentWave++;
        enemiesAlive = 0;

        if (currentWave > maxWaves)
        {
            playerWins();
            return;
        }

        if (WaveLightController.instance != null)
        {
            WaveLightController.instance
                .FlashWarningLights(timeBetweenWaves);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance
                .PlayRoundTransitionMusic();
        }

        waveTimer = 0f;
        waitingForNextWave = true;
    }


    public Transform ClaimRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null)
            return null;

        if (roamPoints == null || roamPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0, roamPoints.Length);

        for (int i = 0; i < roamPoints.Length; i++)
        {
            RoamPoint candidate =
                roamPoints[(startIndex + i) % roamPoints.Length];

            if (!candidate.isFree)
                continue;

            candidate.claimedBy = askingEnemy;

            return candidate.point;
        }

        return null;
    }


    public void ReleaseRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null || roamPoints == null)
            return;

        for (int i = 0; i < roamPoints.Length; i++)
        {
            if (roamPoints[i].claimedBy == askingEnemy)
            {
                roamPoints[i].claimedBy = null;
            }
        }
    }


    private GameObject getWeaponPrefab(EnemyType type, int index)
    {
        GameObject[] weaponPrefabList = null;

        switch (type)
        {
            case EnemyType.basic:
                weaponPrefabList = basicWeaponPrefabs;
                break;

            case EnemyType.heavy:
                weaponPrefabList = heavyWeaponPrefabs;
                break;

            case EnemyType.ranged:
                weaponPrefabList = rangedWeaponPrefabs;
                break;
        }

        if (weaponPrefabList == null ||
            weaponPrefabList.Length == 0)
        {
            return null;
        }

        if (index < 0 || index >= weaponPrefabList.Length)
        {
            index = Random.Range(0, weaponPrefabList.Length);
        }

        return weaponPrefabList[index];
    }





    public void EnemyKilled()
    {
        enemiesAlive--;
        enemiesKilled++;

        if (enemiesAlive < 0)
        {
            enemiesAlive = 0;
        }

        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.EnemyKilled();
        }

        // Don't finish while more enemies are still scheduled to spawn.
        if (enemiesAlive <= 0 && spawnRoutine == null)
        {
            completeWave();
        }
    }


    void completeWave()
    {
        if (!waveInProgress)
            return;

        waveInProgress = false;

        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.WaveCompleted();
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.AddFiles(5);

            // keep the upgrade currency in sync after every wave
            if (UpgradeManager.instance != null)
            {
                UpgradeManager.instance.files += GameManager.instance.totalFiles;
                UpgradeManager.instance.SaveUpgrades();
            }

            //Debug.Log("Current Files: " + GameManager.instance.totalFiles);
        }

        queueNextWave();
    }


    void playerWins()
    {
        if (GameManager.instance != null)
        {
            // GameManager.instance.stateWin();
        }
    }









    public Transform[] cleanList(Transform[] source)
    {
        if (source == null)
            return new Transform[0];

        int counted = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                counted++;
        }

        Transform[] cleaned = new Transform[counted];

        int write = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
                continue;

            cleaned[write] = source[i];
            write++;
        }

        return cleaned;
    }


    private void assignSpawnPoints(Transform[] points)
    {
        Transform[] cleaned = cleanList(points);

        spawnPoints = new SpawnPoint[cleaned.Length];

        for (int i = 0; i < cleaned.Length; i++)
        {
            SpawnPoint newPoint = new SpawnPoint();

            newPoint.point = cleaned[i];
            newPoint.lastUsed = 0f;

            spawnPoints[i] = newPoint;
        }

        if (spawnPoints.Length == 0)
        {
            //Debug.LogError("WaveManager: no spawn points assigned");
        }
    }


    private void assignRoamPoints(Transform[] points)
    {
        Transform[] cleaned = cleanList(points);

        roamPoints = new RoamPoint[cleaned.Length];

        for (int i = 0; i < cleaned.Length; i++)
        {
            RoamPoint newPoint = new RoamPoint();

            newPoint.point = cleaned[i];
            newPoint.claimedBy = null;

            roamPoints[i] = newPoint;
        }

        if (roamPoints.Length == 0)
        {
            //Debug.LogError("WaveManager: no roam points assigned");
        }
    }


}
