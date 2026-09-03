using System.Collections;
using UnityEngine;


// runs enemy spawning for the CEO fight. the boss manager swaps setups as phases start and end,
// and a single loop keeps topping the arena back up to whatever the current setup allows.
// everything in here is real seconds because nothing in the boss arena obeys the player's time scale.
public class BossWaveManager : MonoBehaviour, IWaveHost
{
    public static BossWaveManager instance;

    [Header("Prefabs")]
    [Tooltip("melee enemies, one is picked at random when a basic spawn rolls")]
    [SerializeField] GameObject[] basicEnemyPrefabs;

    [Tooltip("shoving enemies")]
    [SerializeField] GameObject[] heavyEnemyPrefabs;

    [Tooltip("shooting enemies")]
    [SerializeField] GameObject[] rangedEnemyPrefabs;

    [Header("Roam and Spawn points")]
    [Tooltip("empty objects enemies wander between in the arena")]
    [SerializeField] Transform[] roamPointTransforms;

    [Tooltip("empty objects enemies appear at, spread around the arena edge")]
    [SerializeField] Transform[] spawnPointTransforms;



    [Header("Roam Settings")]
    [Tooltip("chance a spawning ranged enemy roams before engaging, 0 to 1")]
    [SerializeField] float giveWillRoamChance;

    [Header("Cues")]
    [Tooltip("Music that plays while a damage phase is running")]
    [SerializeField] AudioClip phaseMusic;

    [Tooltip("Music that plays during immune phase")]
    [SerializeField] AudioClip immuneMusic;

    [Tooltip("How long warning lights flash when a segment changes")]
    [SerializeField] float lightFlashTime = 3f;


    [Header("Spawn Settings")]
    [Tooltip("real seconds between each enemy appearing")]
    [SerializeField] float timeBetweenSpawns = .25f;

    [Tooltip("real seconds a spawn point rests before reusing, spreads spawns around the arena")]
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

    // whichever setup the current phase is running, swapped by the boss manager
    private waveSetup current;

    // which type the last roll picked, held between the roll and the spawn
    private EnemyType typeSpawned;
    private int enemiesAlive;
    private Coroutine spawnRoutine;
    private RoamPoint[] roamPoints;
    private SpawnPoint[] spawnPoints;



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

        setWaveSetupBase();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (ReferenceEquals(waveHost.active, this))
        {
            waveHost.active = null;
        }
    }

    // the boss manager calls these as each phase begins. StartP* run during a
    // damage phase, EndP* run during the immune window between phases.

    public void StartP1() { applySetup(p1); segmentCue(false); }
    public void StartP2() { applySetup(p2); segmentCue(false); }
    public void StartP3() { applySetup(p3); segmentCue(false); }
    public void StartP4() { applySetup(p4); segmentCue(false); }

    // Called as each phase ends. Spawning keeps going through immune window, just using different spawn numbers
    public void EndP1() { applySetup(p1_p2); segmentCue(true); }
    public void EndP2() { applySetup(p2_p3); segmentCue(true); }
    public void EndP3() { applySetup(p3_p4); segmentCue(true); }

    // Boss is dead, stop spawning.
    public void EndP4() { StopSpawning(); }

    // swaps music and flashes the warning lights when the fight changes state
    private void segmentCue(bool immuneWindow)
    {
        if (WaveLightController.instance != null)
        {
            WaveLightController.instance.FlashWarningLights(lightFlashTime);
        }

        if (AudioManager.instance == null)
            return;

        if (immuneWindow)
        {
            AudioManager.instance.PlayMusic(immuneMusic);
        }
        else
        {
            AudioManager.instance.PlayMusic(phaseMusic);
        }
    }

    // switches to a new spawn setup. the loop is already running, it just starts
    // reading different numbers, so phases blend rather than restart.
    private void applySetup(waveSetup setup)
    {

        current = setup;

        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(spawnLoop());
        }
    }


    public void StopSpawning()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    // one loop for the whole fight, topping the arena back up to whatever the
    // current setup allows rather than spawning in discrete waves
    private IEnumerator spawnLoop()
    {
        // only fill the room that is left, so it never goes above maxEnemiesOnMap but it isnt target.
        while (true)
        {
            int roomLeft = current.maxEnemiesOnMap - enemiesAlive;
            int toSpawn = Mathf.Min(roomLeft, current.maxSpawnCount);

            for (int i = 0; i < toSpawn; i++)
            {
                spawnOne();

                // Wait before spawning the next one
                yield return new WaitForSecondsRealtime(timeBetweenSpawns);
            }

            // Real seconds like the rest of the boss arena, Floor so a setup left at 0
            // cannot spin the loop every frame
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, current.timeBetweenBursts));
        }
    }

    // spawns a single enemy of a rolled type at a free spawn point
    private void spawnOne()
    {

        // Grab a random prefab for whichever type the roll landed on
        GameObject enemyPrefab = chooseEnemyPrefab();
        if (enemyPrefab == null)
            return;

        // Get a spawn point that is off cooldown
        SpawnPoint point = getSpawnPoint();
        if (point == null)
            return;

        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, point.point.position, point.point.rotation);

        // Only ranged enemioes roll for roaming
        if (typeSpawned == EnemyType.ranged)
        {
            if (enemy.TryGetComponent<EnemyBase>(out EnemyBase enemyScript))
            {
                enemyScript.willRoam = Random.Range(0f, 1f) <= giveWillRoamChance;
            }
        }

        point.lastUsed = Time.unscaledTime;

        enemiesAlive++;
    }

    // strips nulls out of an inspector array so a forgotten empty slot doesn't throw
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

        int writen = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
                continue;

            cleaned[writen] = source[i];
            writen++;
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

        if (cleaned.Length == 0)
        {
            Debug.LogError("bossWaveManager: no spawn points assigned", this);
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

        if (cleaned.Length == 0)
        {
            Debug.LogError("bossWaveManager: no roam points assigned", this);
        }
    }

    public int EnemiesAlive => enemiesAlive;

    public void EnemyKilled()
    {
        enemiesAlive--;

        if (enemiesAlive < 0)
        {
            enemiesAlive = 0;
        }

        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.EnemyKilled();
        }
    }


    public void ReleaseRoamPoint(GameObject askingEnemy)
    {
        if (askingEnemy == null)
            return;
        if (roamPoints == null)
            return;

        for (int i = 0; i < roamPoints.Length; i++)
        {
            if (roamPoints[i].claimedBy == askingEnemy)
            {
                roamPoints[i].claimedBy = null;
            }
        }
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
            RoamPoint candidate = roamPoints[(startIndex + i) % roamPoints.Length];

            if (!candidate.isFree)
                continue;

            candidate.claimedBy = askingEnemy;
            return candidate.point;
        }

        return null;
    }


    private SpawnPoint getSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        // Gets a random position to start
        int startIndex = Random.Range(0, spawnPoints.Length);

        //  Goes through the list looking for a point that is off cooldown while starting at the original point
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint candidate = spawnPoints[(startIndex + i) % spawnPoints.Length];

            if (candidate.IsFree(spawnPointCooldown))
            {
                return candidate;
            }
        }

        // Everything is still cooling down, so just use the one picked originally
        return spawnPoints[startIndex];
    }

    // picks one at random from a prefab list, null safe
    private GameObject pickFrom(GameObject[] list)
    {
        if (list == null || list.Length == 0)
            return null;

        return list[Random.Range(0, list.Length)];
    }


    // rolls a type against the current setup's weights and returns a prefab
    private GameObject chooseEnemyPrefab()
    {
        float totalPercent = current.rangedEnemyPercent + current.heavyEnemyPercent + current.basicEnemyPercent;

        if (totalPercent <= 0f)
            return null;

        float randomValue = Random.Range(0f, totalPercent);

        if (randomValue < current.rangedEnemyPercent)
        {
            typeSpawned = EnemyType.ranged;
            return pickFrom(rangedEnemyPrefabs);
        }

        randomValue -= current.rangedEnemyPercent;

        if (randomValue < current.basicEnemyPercent)
        {
            typeSpawned = EnemyType.basic;
            return pickFrom(basicEnemyPrefabs);
        }

        typeSpawned = EnemyType.heavy;
        return pickFrom(heavyEnemyPrefabs);

    }


    // sets the starting setup so the loop has something to read before phase 1
    private void setWaveSetupBase()
    {
        waveSetup[] waves = new waveSetup[] { p1, p1_p2, p2, p2_p3, p3, p3_p4, p4 };

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].basicEnemyPercent == 0f)
            {
                waves[i].basicEnemyPercent = 0.25f;
            }

            if (waves[i].heavyEnemyPercent == 0f)
            {
                waves[i].heavyEnemyPercent = 0.25f;
            }

            if (waves[i].rangedEnemyPercent == 0f)
            {
                waves[i].rangedEnemyPercent = 0.5f;
            }

            if (waves[i].maxEnemiesOnMap == 0)
            {
                waves[i].maxEnemiesOnMap = 10;
            }

            if (waves[i].maxSpawnCount == 0)
            {
                waves[i].maxSpawnCount = 5;
            }

            if (waves[i].timeBetweenBursts == 0)
            {
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