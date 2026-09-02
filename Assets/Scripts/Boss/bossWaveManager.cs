using System.Collections;
using UnityEngine;

// Sub class holding one block of spawn numbers, one for each phase and each transition
[System.Serializable]
public struct WaveSetup {
    [Tooltip("Weights, not real percents. They are rolled against their own total so they do not have to add to 100")]
    public float basicEnemyPercent;
    public float heavyEnemyPercent;
    public float rangedEnemyPercent;

    [Tooltip("Ceiling on how many enemies can be alive at once")]
    public int maxEnemiesOnMap;
    [Tooltip("How many spawn per burst, capped by the room left under maxEnemiesOnMap")]
    public int maxSpawnCount;
    [Tooltip("Real seconds between bursts")]
    public float timeBetweenBursts;
}


// runs enemy spawning for the CEO fight. the boss manager swaps setups as phases start and end,
// and a single loop keeps topping the arena back up to whatever the current setup allows.
// everything in here is real seconds because nothing in the boss arena obeys the player's time scale.
public class bossWaveManager : MonoBehaviour , IWaveHost {
    public static bossWaveManager instance;

    [Header("Prefabs")]
    [SerializeField] GameObject[] basicEnemyPrefabs;
    [SerializeField] GameObject[] heavyEnemyPrefabs;
    [SerializeField] GameObject[] rangedEnemyPrefabs;



    [Header("Roam and Spawn points")]
    [SerializeField] Transform[] roamPointTransforms;
    [SerializeField] Transform[] spawnPointTransforms;
    private roamPoint[] roamPoints;
    private spawnPoint[] spawnPoints;


    [Header("Roam Settings")]
    [SerializeField] float giveWillRoamChance;

    [Header("Cues")]
    [Tooltip("Music that plays while a madage phase is running")]
    [SerializeField] AudioClip phaseMusic;
    [Tooltip("Music that plays during immune phase")]
    [SerializeField] AudioClip immuneMusic;
    [Tooltip("How long warning lights flash when a segment changes")]
    [SerializeField] float lightFlashTime = 3f;
    

    [Header("Spawn Settings")]
    [SerializeField] float timeBetweenSpawns = .25f;
    [SerializeField] float spawnPointCooldown = 5f;


    [Header("Phase 1")]
    [SerializeField] waveSetup p1;

    [Header("Phase 1 to Phase 2")]
    [SerializeField] waveSetup p1_p2;

    [Header("Phase 2")]
    [SerializeField] waveSetup p2;

    [Header("Phase 2 to Phase 3")]
    [SerializeField] waveSetup p2_p3;

    [Header("Phase 3")]
    [SerializeField] waveSetup p3;

    [Header("Phase 3 to Phase 4")]
    [SerializeField] waveSetup p3_p4;

    [Header("Phase 4")]
    [SerializeField] waveSetup p4;

    private waveSetup current;
    private enemyType typeSpawned;
    private int enemiesAlive;
    private Coroutine spawnRoutine;


    void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;

        waveHost.active = this;

        assignSpawnPoints(spawnPointTransforms);
        assignRoamPoints(roamPointTransforms);

        setWaveSetupBase();
    }

    void OnDestroy() {
        if (instance == this)
            instance = null;

        if (ReferenceEquals(waveHost.active, this)) {
            waveHost.active = null;
        }
    }

    // Called by bossFightManager as each phase begins
    public void startP1() { applySetup(p1); segmentCue(false); }
    public void startP2() { applySetup(p2); segmentCue(false); }
    public void startP3() { applySetup(p3); segmentCue(false); }
    public void startP4() { applySetup(p4); segmentCue(false); }

    // Called as each phase ends. Spawning keeps going through immune window, just using different spawn numbers
    public void endP1() { applySetup(p1_p2); segmentCue(true); }
    public void endP2() { applySetup(p2_p3); segmentCue(true); }
    public void endP3() { applySetup(p3_p4); segmentCue(true); }

    // Boss is dead, stop spawning.
    public void endP4() { stopSpawning(); }


    private void segmentCue(bool immuneWindow) {
        if (waveLightController.instance != null) {
            waveLightController.instance.FlashWarningLights(lightFlashTime);
        }

        if (audioManager.instance == null)
            return;

        if (immuneWindow) {
            audioManager.instance.playMusic(immuneMusic);
        } else {
            audioManager.instance.playMusic(phaseMusic);
        }
    }

    // Swap the spawn numbers to the current phase numbers
    private void applySetup(waveSetup setup) {
        current = setup;

        if (spawnRoutine == null) {
            spawnRoutine = StartCoroutine(spawnLoop());
        }
    }


    public void stopSpawning() {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private IEnumerator spawnLoop() {
        // only fill the room that is left, so it never goes above maxEnemiesOnMap but it isnt target.
        while (true) {
            int roomLeft = current.maxEnemiesOnMap - enemiesAlive;
            int toSpawn = Mathf.Min(roomLeft , current.maxSpawnCount);

            for (int i = 0 ; i < toSpawn ; i++) {
                spawnOne();

                // Wait before spawning the next one
                yield return new WaitForSecondsRealtime(timeBetweenSpawns);
            }

            // Real seconds like the rest of the boss arena, Floor so a setup left at 0
            // cannot spin the loop every frame
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f , current.timeBetweenBursts));
        }
    }

    private void spawnOne() {
        // Grab a random prefab for whichever type the roll landed on
        GameObject enemyPrefab = chooseEnemyPrefab();
        if (enemyPrefab == null)
            return;

        // Get a spawn point that is off cooldown
        spawnPoint point = getSpawnPoint();
        if (point == null)
            return;

        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab , point.point.position , point.point.rotation);

        // Only ranged enemioes roll for roaming
        if (typeSpawned == enemyType.ranged) {
            if (enemy.TryGetComponent<enemyBase>(out enemyBase enemyScript)) {
                enemyScript.willRoam = Random.Range(0f , 1f) <= giveWillRoamChance;
            }
        }

        point.lastUsed = Time.unscaledTime;

        enemiesAlive++;
    }

    public Transform[] cleanList(Transform[] source) {
        if (source == null)
            return new Transform[0];

        int counted = 0;

        for (int i = 0 ; i < source.Length ; i++) {
            if (source[i] != null)
                counted++;
        }

        Transform[] cleaned = new Transform[counted];

        int writen = 0;

        for (int i = 0 ; i < source.Length ; i++) {
            if (source[i] == null)
                continue;

            cleaned[writen] = source[i];
            writen++;
        }

        return cleaned;
    }

    private void assignSpawnPoints(Transform[] points) {
        Transform[] cleaned = cleanList(points);
        spawnPoints = new spawnPoint[cleaned.Length];

        for (int i = 0 ; i < cleaned.Length ; i++) {
            spawnPoint newPoint = new spawnPoint();
            newPoint.point = cleaned[i];
            newPoint.lastUsed = 0f;

            spawnPoints[i] = newPoint;
        }

        if (cleaned.Length == 0) {
            Debug.LogError("bossWaveManager: no spawn points assigned" , this);
        }
    }


    private void assignRoamPoints(Transform[] points) {
        Transform[] cleaned = cleanList(points);
        roamPoints = new roamPoint[cleaned.Length];

        for (int i = 0 ; i < cleaned.Length ; i++) {
            roamPoint newPoint = new roamPoint();
            newPoint.point = cleaned[i];
            newPoint.claimedBy = null;

            roamPoints[i] = newPoint;
        }

        if (cleaned.Length == 0) {
            Debug.LogError("bossWaveManager: no roam points assigned" , this);
        }
    }

    public int getEnemiesAlive() {
        return enemiesAlive;
    }

    public void enemyKilled() {
        enemiesAlive--;

        if (enemiesAlive < 0) {
            enemiesAlive = 0;
        }

        if (heartbeatManager.instance != null) {
            heartbeatManager.instance.enemyKilled();
        }
    }


    public void releaseRoamPoint(GameObject askingEnemy) {
        if (askingEnemy == null)
            return;
        if (roamPoints == null)
            return;

        for (int i = 0 ; i < roamPoints.Length ; i++) {
            if (roamPoints[i].claimedBy == askingEnemy) {
                roamPoints[i].claimedBy = null;
            }
        }
    }


    public Transform claimRoamPoint(GameObject askingEnemy) {
        if (askingEnemy == null)
            return null;
        if (roamPoints == null || roamPoints.Length == 0)
            return null;

        int startIndex = Random.Range(0 , roamPoints.Length);

        for (int i = 0 ; i < roamPoints.Length ; i++) {
            roamPoint candidate = roamPoints[(startIndex + i) % roamPoints.Length];

            if (!candidate.isFree)
                continue;

            candidate.claimedBy = askingEnemy;
            return candidate.point;
        }

        return null;
    }


    private spawnPoint getSpawnPoint() {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        // Gets a random position to start
        int startIndex = Random.Range(0 , spawnPoints.Length);

        //  Goes through the list looking for a point that is off cooldown while starting at the original point
        for (int i = 0 ; i < spawnPoints.Length ; i++) {
            spawnPoint candidate = spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (candidate.isFree(spawnPointCooldown)) {
                return candidate;
            }
        }

        // Everything is still cooling down, so just use the one picked originally
        return spawnPoints[startIndex];
    }

    // Standardized method to pick randomly from a list
    private GameObject pickFrom(GameObject[] list) {
        if (list == null || list.Length == 0)
            return null;

        return list[Random.Range(0 , list.Length)];
    }


    // Rolls against the weight total incase it isnt set to 100 or 1
    private GameObject chooseEnemyPrefab() {
        float totalPercent = current.rangedEnemyPercent + current.heavyEnemyPercent + current.basicEnemyPercent;

        if (totalPercent <= 0f)
            return null;

        float randomValue = Random.Range(0f , totalPercent);

        if (randomValue < current.rangedEnemyPercent) {
            typeSpawned = enemyType.ranged;
            return pickFrom(rangedEnemyPrefabs);
        }

        randomValue -= current.rangedEnemyPercent;

        if (randomValue < current.basicEnemyPercent) {
            typeSpawned = enemyType.basic;
            return pickFrom(basicEnemyPrefabs);
        }

        typeSpawned = enemyType.heavy;
        return pickFrom(heavyEnemyPrefabs);

    }


    private void setWaveSetupBase() {
        waveSetup[] waves = new waveSetup[] { p1 , p1_p2 ,p2, p2_p3 , p3 , p3_p4 , p4 };

        for (int i = 0 ; i < waves.Length ; i++) {
            if (waves[i].basicEnemyPercent == 0f) {
                waves[i].basicEnemyPercent = 0.25f;
            }

            if (waves[i].heavyEnemyPercent == 0f) {
                waves[i].heavyEnemyPercent = 0.25f;
            }

            if (waves[i].rangedEnemyPercent == 0f) {
                waves[i].rangedEnemyPercent = 0.5f;
            }

            if (waves[i].maxEnemiesOnMap == 0) {
                waves[i].maxEnemiesOnMap = 10;
            }

            if (waves[i].maxSpawnCount == 0) {
                waves[i].maxSpawnCount = 5;
            }

            if (waves[i].timeBetweenBursts == 0) {
                waves[i].timeBetweenBursts = 5;
            }
        }

        p1 = waves[0];
        p1_p2 = waves[1];
        p2 = waves[2];
        p2_p3 = waves[3];
        p3 = waves[4];
        p3_p4 = waves[5];
        p4 = waves[6];
    }
}