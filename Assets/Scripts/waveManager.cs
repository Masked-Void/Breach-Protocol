using System.Collections;
using UnityEngine;

public enum enemyType
{
    basic,
    heavy,
    ranged
}

public class roamPoint
{
    public Transform point;
    public GameObject claimedBy;

    public bool isFree
    {
        get { return claimedBy == null; }
    }
}

public class spawnPoint
{
    public Transform point;
    public float lastUsed;

    public bool isFree(float cooldown)
    {
        return Time.time - lastUsed >= cooldown;
    }
}

public class waveManager : MonoBehaviour
{
    public static waveManager instance;

    [Header("Weapon Prefabs")]
    [SerializeField] GameObject[] basicWeaponPrefabs;
    [SerializeField] GameObject[] heavyWeaponPrefabs;
    [SerializeField] GameObject[] rangedWeaponPrefabs;

    [Header("Enemy Prefabs")]
    [SerializeField] GameObject basicEnemyPrefabs;
    [SerializeField] GameObject heavyEnemyPrefabs;
    [SerializeField] GameObject rangedEnemyPrefabs;

    [Header("Roam and Spawn Points")]
    [SerializeField] Transform[] roamPointTransforms;
    [SerializeField] Transform[] spawnPointTransforms;

    private roamPoint[] roamPoints;
    private spawnPoint[] spawnPoints;

    [Header("Roam Settings")]
    [Tooltip("Chance that a ranged enemy will roam before engaging.")]
    [SerializeField] float giveWillRoamChance;

    [Header("Spawn Settings")]
    [SerializeField] int enemiesToSpawnAtWave0;
    [SerializeField] float enemyIncreaseMultiplier;

    [HideInInspector] enemyType typeSpawned;

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
    [SerializeField] private int enemiesAlive;

    private bool waveInProgress;
    private Coroutine spawnRoutine;


    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        assignSpawnPoints(spawnPointTransforms);
        assignRoamPoints(roamPointTransforms);
    }


    void Start()
    {
        queueNextWave();
    }


    void Update()
    {
        if (gameManager.instance != null &&
            gameManager.instance.isPaused)
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

            spawnPoint point = getSpawnPoint();

            if (point == null)
                break;

            GameObject enemy = Instantiate(
                enemyPrefab,
                point.point.position,
                point.point.rotation
            );

            if (typeSpawned == enemyType.ranged)
            {
                if (enemy.TryGetComponent<enemyBase>(
                    out enemyBase enemyScript))
                {
                    enemyScript.willRoam =
                        Random.Range(0f, 1f) <= giveWillRoamChance;
                }
            }

            point.lastUsed = Time.time;

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
            typeSpawned = enemyType.ranged;
            return rangedEnemyPrefabs;
        }

        randomValue -= rangedEnemyPercent;

        if (randomValue < basicEnemyPercent)
        {
            typeSpawned = enemyType.basic;
            return basicEnemyPrefabs;
        }

        typeSpawned = enemyType.heavy;
        return heavyEnemyPrefabs;
    }


    private spawnPoint getSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPoint candidate =
                spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (candidate.isFree(spawnPointCooldown))
            {
                return candidate;
            }
        }

        return spawnPoints[startIndex];
    }


    private void startWave()
    {
        if (audioManager.instance != null)
        {
            audioManager.instance.stopMusic();
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

        if (waveLightController.instance != null)
        {
            waveLightController.instance
                .FlashWarningLights(timeBetweenWaves);
        }

        if (audioManager.instance != null)
        {
            audioManager.instance
                .playRoundTransitionMusic();
        }

        waveTimer = 0f;
        waitingForNextWave = true;
    }


    public Transform claimRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null)
            return null;

        if (roamPoints == null || roamPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0, roamPoints.Length);

        for (int i = 0; i < roamPoints.Length; i++)
        {
            roamPoint candidate =
                roamPoints[(startIndex + i) % roamPoints.Length];

            if (!candidate.isFree)
                continue;

            candidate.claimedBy = askingEnemy;

            return candidate.point;
        }

        return null;
    }


    public void releaseRoamPoint(GameObject askingEnemy)
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


    private GameObject getWeaponPrefab(enemyType type, int index)
    {
        GameObject[] weaponPrefabList = null;

        switch (type)
        {
            case enemyType.basic:
                weaponPrefabList = basicWeaponPrefabs;
                break;

            case enemyType.heavy:
                weaponPrefabList = heavyWeaponPrefabs;
                break;

            case enemyType.ranged:
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


    public int getCurrentWave()
    {
        return currentWave;
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

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.waveCompleted();
        }

        if (gameManager.instance != null)
        {
            gameManager.instance.AddFiles(5);

            Debug.Log(
                "Current Files: " +
                gameManager.instance.totalFiles
            );
        }

        queueNextWave();
    }


    void playerWins()
    {
        if (gameManager.instance != null)
        {
            // gameManager.instance.stateWin();
        }
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
        float remaining =
            timeBetweenWaves - waveTimer;

        return Mathf.Max(
            0,
            Mathf.CeilToInt(remaining)
        );
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

        spawnPoints = new spawnPoint[cleaned.Length];

        for (int i = 0; i < cleaned.Length; i++)
        {
            spawnPoint newPoint = new spawnPoint();

            newPoint.point = cleaned[i];
            newPoint.lastUsed = 0f;

            spawnPoints[i] = newPoint;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError(
                "waveManager: no spawn points assigned"
            );
        }
    }


    private void assignRoamPoints(Transform[] points)
    {
        Transform[] cleaned = cleanList(points);

        roamPoints = new roamPoint[cleaned.Length];

        for (int i = 0; i < cleaned.Length; i++)
        {
            roamPoint newPoint = new roamPoint();

            newPoint.point = cleaned[i];
            newPoint.claimedBy = null;

            roamPoints[i] = newPoint;
        }

        if (roamPoints.Length == 0)
        {
            Debug.LogError(
                "waveManager: no roam points assigned"
            );
        }
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
