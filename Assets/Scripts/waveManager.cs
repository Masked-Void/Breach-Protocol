/*
Script: WaveManager*
Description:
Central wave coordinator. Does NOT spawn enemies itself - each
spawnPoint is fully individual (own prefabs, percentages, pacing,
and difficulty scaling). WaveManager just owns the wave number and
the countdown between waves, and keeps every spawnPoint in sync:
the next wave will not begin until every registered spawnPoint has
finished spawning AND every enemy from every spawnPoint is dead.*
Responsibilities:
Automatically start the first wave when the level begins
Wait between waves (real-time, unaffected by Time.timeScale),
then tell every spawnPoint to begin the new wave
Track total enemies alive across all spawn points
Only complete a wave once ALL spawn points are done spawning and
ALL of their enemies are dead
Notify HeartbeatManager when enemies die or waves end
Notify GameManager when all waves are completed
*
Interacts With:
spawnPoint (one or more, individually configured)
heartbeatManager
gameManager
waveLightController
audioManager
*/

using JetBrains.Annotations;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public enum enemyType { basic, heavy, ranged }

// Sub class for each roamPoint to determine if it has a currently selected enemy
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
         return (Time.time - lastUsed >= cooldown);
    }
}

public class waveManager : MonoBehaviour
{
    public static waveManager instance;

    [Header("Prefabs")]
    [SerializeField] GameObject []basicWeaponPrefabs;
    [SerializeField] GameObject[] heavyWeaponPrefabs;
    [SerializeField] GameObject[] rangedWeaponPrefabs;

    [SerializeField] GameObject[] basicEnemyPrefabs;
    [SerializeField] GameObject[] heavyEnemyPrefabs;
    [SerializeField] GameObject[] rangedEnemyPrefabs;

    [Header("Roam and Spawn points")]
    [SerializeField] Transform[] roamPointTransforms;
    [SerializeField] Transform[] spawnPointTransforms;
    private roamPoint[] roamPoints;
    private spawnPoint[] spawnPoints;

    [Header("Roam Settings")]
    [Tooltip("Roam is for ranged enemies")]
    [SerializeField] float giveWillRoamChance;


    [Header("Spawn Settings")]
    [SerializeField] int enemiesToSpawnAtWave0;
    [SerializeField] float enemyIncreaseMultiplier;
    [HideInInspector] enemyType typeSpawned;

    [Header("EnemyPercentToSpawn")]
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
    private int currentIndex;
    [SerializeField] bool waitingForNextWave;
    bool waveInProgress;

    [Header("Wave Tracking")]
    [SerializeField] private int currentWave = 0;

    int enemiesAlive;
    int enemiesToSpawn;

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
        if (waitingForNextWave)
        {
            // Unscaled so the countdown is always real seconds, regardless
            // of Time.timeScale (slow-mo, hit-stop, etc.).
            waveTimer += Time.unscaledDeltaTime;

            if (waveTimer >= timeBetweenWaves)
            {
                waitingForNextWave = false;
                startWave();
            }
        }
    }

    private IEnumerator spawn()
    {
        // How many enemies this wave?  wave0 = base, then multiply each wave
        int enemiesToSpawn = Mathf.RoundToInt(
            enemiesToSpawnAtWave0 * Mathf.Pow(enemyIncreaseMultiplier, currentWave)
        );

        for (int i = 0; i < enemiesToSpawn; i++)
        {

            // Grab a random prefab for that type (-1 = random index)
            GameObject enemyPrefab = chooseEnemyPrefab();
            if (enemyPrefab == null) continue;

            //  Get the next spawn point in round-robin order
            spawnPoint point = getSpawnPoint(enemyPrefab);
            if (point == null) break;               // no spawn points configured

            // Instantiate the enemy
            GameObject enemy = Instantiate(enemyPrefab, point.point.transform.position, point.point.transform.rotation);
            
            if (typeSpawned == enemyType.ranged)
            {
                if (enemy.TryGetComponent<enemyBase>(out enemyBase enemyScript))
                {
                    bool giveRoam = Random.Range(0f, 1f) <= giveWillRoamChance;
                    enemyScript.willRoam = giveRoam;
                }
            }
          

            point.lastUsed = Time.time;

            enemiesAlive++;

            // Wait before spawning the next one
            yield return new WaitForSeconds(timeBetweenSpawns);
           
        }

        enemiesToSpawn = 0;
        spawnRoutine = null;

        if (enemiesAlive <= 0)
        {
            completeWave();
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
            typeSpawned = enemyType.ranged;
            return rangedEnemyPrefabs[Random.Range(0, rangedEnemyPrefabs.Length)];
        }

        randomValue -= rangedEnemyPercent;

        if (randomValue < basicEnemyPercent)
        {
            typeSpawned = enemyType.basic;
            return basicEnemyPrefabs[Random.Range(0, basicEnemyPrefabs.Length)];
        }
        typeSpawned = enemyType.heavy;
         return heavyEnemyPrefabs[Random.Range(0, heavyEnemyPrefabs.Length)];
    }
    
    private spawnPoint getSpawnPoint(GameObject enemy)
    {
        if (spawnPoints.Length == 0) return null;

        int startIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPoint candidate = spawnPoints[(startIndex + i) % spawnPoints.Length];

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

        enemiesToSpawn = 1;
        
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
           //layerWins();
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


    public Transform claimRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null) return null;
        if (roamPoints == null || roamPoints.Length == 0) return null;

        int startIndex = Random.Range(0, roamPoints.Length);

        for (int i = 0; i < roamPoints.Length; i++)
        {
            roamPoint candidate = roamPoints[(startIndex + i) % roamPoints.Length];

            if (!candidate.isFree) continue;

            candidate.claimedBy = askingEnemy;
            return candidate.point;
        }

        return null;
    }

    public void releaseRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null) return;
        if (roamPoints == null) return;

        for (int i = 0; i < roamPoints.Length; i++)
        {
            if (roamPoints[i].claimedBy == askingEnemy)
            {
                roamPoints[i].claimedBy = null;
            }
        }
    }

    // Gets the prefab for an weapon for given enemy type. If the index is less than 0 or greater than the amount of prefabs in that list it gets a random one;
    private GameObject getWeaponPrefab(enemyType type, int index)
    {
        GameObject[] weaponPrefabList = new GameObject[0];

        switch ((int)type)
        {
            case 0: weaponPrefabList = basicWeaponPrefabs; break;
            case 1: weaponPrefabList = heavyWeaponPrefabs; break;
            case 2: weaponPrefabList = rangedWeaponPrefabs; break;
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

        if (enemiesAlive <= 0)
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
        gameManager.instance.AddFiles(5);
        upgradeManager.instance.files += gameManager.instance.totalFiles;
        upgradeManager.instance.SaveUpgrades();
        //Debug.Log("Current Files: " + gameManager.instance.totalFiles);

    }

    void playerWins()
    {
        if (gameManager.instance != null)
        {
            // Add this once your gameManager has a win menu.
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
        float remaining = timeBetweenWaves - waveTimer;
        return Mathf.Max(0, Mathf.CeilToInt(remaining));
    }

    public Transform[] cleanList(Transform[] source)
    {
        if (source == null) return new Transform[0];

        int counted = 0;
    
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null) counted++;
        }

        Transform[] cleaned = new Transform[counted];

        int write = 0; 
        for (int i = 0;  i < source.Length; i++)
        {
            if (source[i] == null) continue;

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

        currentIndex = 0;

        if (spawnPointTransforms.Length == 0)
        {
            //Debug.LogError("waveManager: no spawn points assigned");
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

        if (roamPointTransforms.Length == 0)
        {
            //Debug.LogError("waveManager: no roam points assigned");
        }
    }
}
