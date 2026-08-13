using Unity.VisualScripting;
using UnityEngine;

public class bossWaveManager : MonoBehaviour
{

    bossWaveManager instance;

    [SerializeField] public GameObject[] spawners;

    [Header("Phase 1 Enemy Percentages")]
    [SerializeField] float p1RangedPerc;
    [SerializeField] float p1MeleePerc;
    [SerializeField] float p1HeavyPerc;
    [SerializeField] int p1MaxEnemiesOnMap;
    [SerializeField] int p1MaxSpawnCount;

    [Header("Phase 1 to Phase 2 Transition")]
    [SerializeField] float p1_p2RangedPerc;
    [SerializeField] float p1_p2MeleePerc;
    [SerializeField] float p1_p2HeavyPerc;
    [SerializeField] int p1_p2MaxEnemiesOnMap;
    [SerializeField] int p1_p2MaxSpawnCount;

    [Header("Phase 2 Enemy Percentages")]
    [SerializeField] float p2RangedPerc;
    [SerializeField] float p2MeleePerc;
    [SerializeField] float p2HeavyPerc;
    [SerializeField] int p2MaxEnemiesOnMap;
    [SerializeField] int p2MaxSpawnCount;

    [Header("Phase 2 to Phase 3 Transition")]
    [SerializeField] float p2_p3RangedPerc;
    [SerializeField] float p2_p3MeleePerc;
    [SerializeField] float p2_p3HeavyPerc;
    [SerializeField] int p2_p3MaxEnemiesOnMap;
    [SerializeField] int p2_p3MaxSpawnCount;

    [Header("Phase 3 Enemy Percentages")]
    [SerializeField] float p3RangedPerc;
    [SerializeField] float p3MeleePerc;
    [SerializeField] float p3HeavyPerc;
    [SerializeField] int p3MaxEnemiesOnMap;
    [SerializeField] int p3MaxSpawnCount;

    [Header("Phase 3 to Phase 4 Transition")]
    [SerializeField] float p3_p4RangedPerc;
    [SerializeField] float p3_p4MeleePerc;
    [SerializeField] float p3_p4HeavyPerc;
    [SerializeField] int p3_p4MaxEnemiesOnMap;
    [SerializeField] int p3_p4MaxSpawnCount;

    [Header("Phase 4 Enemy Percentages")]
    [SerializeField] float p4RangedPerc;
    [SerializeField] float p4MeleePerc;
    [SerializeField] float p4HeavyPerc;
    [SerializeField] int p4MaxEnemiesOnMap;
    [SerializeField] int p4MaxSpawnCount;

    float rangedPerc;
    float meleePerc;
    float heavyPerc;
    int maxEnemiesOnMap;
    int maxSpawnCount;

    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void startP1()
    {
        rangedPerc = p1RangedPerc;
        meleePerc = p1MeleePerc;
        heavyPerc = p1HeavyPerc;
        maxEnemiesOnMap = p1MaxEnemiesOnMap;
        maxSpawnCount = p1MaxSpawnCount;
    }
    public void startP2()
    {
        rangedPerc = p2RangedPerc;
        meleePerc = p2MeleePerc;
        heavyPerc = p2HeavyPerc;
        maxEnemiesOnMap = p2MaxEnemiesOnMap;
        maxSpawnCount = p2MaxSpawnCount;
    }

    public void startP3()
    {
        rangedPerc = p3RangedPerc;
        meleePerc = p3MeleePerc;
        heavyPerc = p3HeavyPerc;
        maxEnemiesOnMap = p3MaxEnemiesOnMap;
        maxSpawnCount = p3MaxSpawnCount;
    }

    public void startP4()
    {
        rangedPerc = p3RangedPerc;
        meleePerc = p3MeleePerc;
        heavyPerc = p3HeavyPerc;
        maxEnemiesOnMap = p3MaxEnemiesOnMap;
        maxSpawnCount = p3MaxSpawnCount;
    }

    public void endP1()
    {
        rangedPerc = p1_p2RangedPerc;
        meleePerc = p1_p2MeleePerc;
        heavyPerc = p1_p2HeavyPerc;
        maxEnemiesOnMap = p1_p2MaxEnemiesOnMap;
        maxSpawnCount = p1_p2MaxSpawnCount;
    }

    public void endP2()
    {
        rangedPerc = p2_p3RangedPerc;
        meleePerc = p2_p3MeleePerc;
        heavyPerc = p2_p3HeavyPerc;
        maxEnemiesOnMap = p2_p3MaxEnemiesOnMap;
        maxSpawnCount = p2_p3MaxSpawnCount;
    }

    public void endP3()
    {
        rangedPerc = p3_p4RangedPerc;
        meleePerc = p3_p4MeleePerc;
        heavyPerc= p3_p4HeavyPerc;
        maxEnemiesOnMap = p3_p4MaxEnemiesOnMap;
        maxSpawnCount= p3_p4MaxSpawnCount;
    }
}
