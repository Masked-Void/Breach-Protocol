using System.Collections;
using UnityEngine;
using UnityEngine.AI;


/*
 * Script: EnemyBase
 *
 * Description:
 * Base class for every enemy. Owns the shared plumbing: sight checks, roaming,
 * attacking, damage and the single death path. Subclasses override the hooks;
 * tuning lives in an EnemyConfig asset, one per enemy type.
 *
 * Responsibilities:
 * - Read tuning from EnemyConfig, copy the mutable values into fields on Start
 * - Line of sight and FOV checks against the player
 * - Roaming between claimed roam points when not engaged
 * - Melee attack timing and damage
 * - Single death path through Die(), which raises EnemyEvents.Killed
 * - Tell the wave host directly, since wave progression is a hard dependency
 *
 * Interacts With:
 * - EnemyConfig (tuning, one asset per type)
 * - EnemyEvents (raises Killed)
 * - IWaveHost (WaveManager or BossWaveManager)
 * - GameManager (player position)
 * - PacketLossKillstreak (mutates attackRate at runtime)
 *
 * Notes:
 * - attackRate is a field, not a config read, because streaks and guns change it
 * - maxHP exists but every enemy is tuned to 1, per the GDD one-hit rule
 */


[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamage
{
    [Header("Visuals")]
    [Tooltip("the model renderer, flashed black briefly when hit")]
    [SerializeField] public Renderer model;

    // original colour, restored after the hit flash
    Color colorOrig;

    [Header("Agent")]
    [Tooltip("navmesh agent on this object, does the actual pathing")]
    [SerializeField] public NavMeshAgent agent;

    [Header("Config")]
    [Tooltip("tuning for this enemy type, one asset per type")]
    [SerializeField] private EnemyConfig config;

    [Header("Runtime Stats")]
    // copied out of the config in Start so streaks and guns can change them per enemy
    int currentHP;
    public float attackRate;

    // read straight off the config, nothing changes these at runtime
    public float AttackRange => config.attackRange;
    public int AttackDamage => config.attackDamage;

    [Header("Spawn and Roam")]
    [Tooltip("set true once the enemy walks out of its spawn room, gates roaming")]
    public bool hasLeftSpawnRoom = false;

    [Tooltip("tick to let this enemy wander between roam points instead of holding position")]
    public bool willRoam = false;

    [Tooltip("optional specific point this enemy prefers, leave empty to claim any free one")]
    [SerializeField] GameObject roamPoint;

    // roaming state
    float roamTimer;
    public Transform roamTarget;
    protected bool isEngaged = false;

    // footstep timing
    float stepTimer;

    // per frame sight and combat state
    protected bool playerInTrigger;
    protected float angleToPlayer;
    protected float stoppingDistOrig;
    protected float attackTimer;
    protected Vector3 playerDir;

    [Header("Challenge")]
    // what killed this enemy, so ChallengeManager can credit the right weapon
    protected WeaponStats lastDamageWeapon;
    protected bool lastDamageFromGround;

    // cached on Start so Update isn't null checking five singletons every frame
    bool hasGameManager;
    bool hasAudioManager;
    bool hasWeaponManager;
    bool hasChallengeManager;
    bool hasUpgradeManager;

    public WeaponStats LastDamageWeapon => lastDamageWeapon;

    // death state. suppressKillRewards lets Data Purge kill without awarding.
    private bool isDead;
    private bool suppressKillRewards;

    public bool IsDead => isDead;
    public int ByteValue => config.byteValue;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackRate = config.shotInterval;
        currentHP = config.maxHP;
        stoppingDistOrig = agent.stoppingDistance;
        if (willRoam)
        {
            pickRoamPoint();
        }

        if (model != null)
            colorOrig = model.material.color;

        hasAudioManager = AudioManager.instance != null;
        hasGameManager = GameManager.instance != null;
        hasWeaponManager = WeaponManager.instance != null;
        hasChallengeManager = ChallengeManager.instance != null;
        hasUpgradeManager = UpgradeManager.instance != null;
    }

    void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;
        attackTimer += Time.unscaledDeltaTime;

        HandleFootSteps();

        if (!willRoam)
        {
            // Heavy / Basic: finish first roam point, then b-line player forever
            /* if (roamTarget != null)
             {
                 if (AtRoamTarget())
                 {
                     waveHost.active?.releaseRoamPoint(gameObject);
                     roamTarget = null;
                     agent.stoppingDistance = stoppingDistOrig;

                     -Blocked out this line of code. With this commented out melee AI go straight for player
                 }
             }
            */
            if (GameManager.instance?.player != null)
            {
                agent.stoppingDistance = Mathf.Max(0.5f, config.attackRange - 0.5f);
                agent.SetDestination(GameManager.instance.player.transform.position);
                playerDir = GameManager.instance.player.transform.position - transform.position;
                FaceTarget();
                attack();
            }
        }
        else if (isEngaged)
        {
            // Ranged: now chasing the player
            if (GameManager.instance?.player != null)
            {
                //check to disengage
                if (!CanStillSeePlayer())
                {
                    isEngaged = false;
                    agent.stoppingDistance = 0;

                    //clear old roam point so pick a new one
                    if (roamTarget != null)
                    {
                        waveHost.active?.ReleaseRoamPoint(gameObject);
                        roamTarget = null;
                    }
                    return;
                }
                agent.SetDestination(GameManager.instance.player.transform.position);
                playerDir = GameManager.instance.player.transform.position - transform.position;
                FaceTarget();


                float distance = playerDir.magnitude;


                if (distance <= config.rangedAttackRange)
                {
                    attack();
                }
            }
        }
        else
        {
            // Ranged: roaming � only look around while stopped at a roam point
            Roam();


            if (tryAttackFromCurrentPosition())
            {
                isEngaged = true;
                agent.stoppingDistance = stoppingDistOrig;
            }

        }
    }

    // picks a roam point, preferring the assigned one, otherwise claiming any free one
    void pickRoamPoint()
    {
        if (waveHost.active == null)
            return;

        waveHost.active.ReleaseRoamPoint(gameObject);

        if (willRoam && GameManager.instance?.player != null)
        {
            Transform nextRoamPoint = findNearestRoamPointToPlayer();

            if (nextRoamPoint != null)
            {
                roamTarget = nextRoamPoint;
                agent.stoppingDistance = 0f;
                agent.SetDestination(roamTarget.position);
            }
            return;
        }
    }

    // used by ranged enemies so they close on the player rather than wandering off
    Transform findNearestRoamPointToPlayer()
    {
        if (waveHost.active == null)
            return null;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Transform closestPoint = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < 10; i++)
        {
            Transform candidatePoint = waveHost.active.ClaimRoamPoint(gameObject);
            if (candidatePoint == null)
                break;

            float distToPlayer = Vector3.Distance(candidatePoint.position, playerPos);

            if (distToPlayer <= config.roamRange && distToPlayer < closestDistance)
            {
                if (closestPoint != null)
                {
                    //need to check this
                }
                closestPoint = candidatePoint;
                closestDistance = distToPlayer;
            }
            else
            {
                waveHost.active.ReleaseRoamPoint(gameObject);
            }
        }
        return closestPoint;
    }


    // fires a step sound on an interval, only while actually moving
    void HandleFootSteps()
    {
        if (agent.velocity.magnitude > config.stepSpeedThreshold)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= config.stepInterval)
            {
                stepTimer = 0f;

                if (AudioManager.instance != null && AudioManager.instance.enemySteps != null && AudioManager.instance.enemySteps.Length > 0)
                {
                    AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.enemySteps), transform.position, AudioManager.instance.enemyStepsVol, 3f, 20f);
                }
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // cheaper follow-up check once already engaged, skips the FOV cone
    private bool CanStillSeePlayer()
    {
        if (GameManager.instance == null || GameManager.instance.player == null)
            return false;

        Vector3 dirToPlayer = GameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, config.rangedAttackRange))
        {
            return hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player") && angleToPlayer <= config.fov;
        }
        return false;
    }

    // true if we're in range and off cooldown, so the subclass can swing or shoot
    protected bool tryAttackFromCurrentPosition()
    {
        if (GameManager.instance == null || GameManager.instance.player == null)
            return false;

        Vector3 dirToPlayer = GameManager.instance.player.transform.position - transform.position;
        float distance = dirToPlayer.magnitude;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (willRoam && distance > config.rangedAttackRange)
        {
            return false;
        }

        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, config.rangedAttackRange))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= config.fov)
            {
                playerDir = dirToPlayer;
                FaceTarget();
                attack();
                return true;
            }
        }

        return false;
    }

    // full check: in trigger, inside the FOV cone, and nothing blocking the ray
    public virtual bool CanSeePlayer()
    {
        playerDir = GameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, playerDir, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= config.fov)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                FaceTarget();

                attack();
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }


    public virtual void Roam()
    {

        //Check distance from player
        if (GameManager.instance?.player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);

            // if too far hunt them down
            if (distToPlayer > config.rangedAttackRange)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                agent.stoppingDistance = 0f;

                //clear roam target to hunt
                if (roamTarget != null)
                {
                    waveHost.active?.ReleaseRoamPoint(gameObject);
                    roamTarget = null;
                }
                return;
            }
        }

        // within range is normal roaming

        if (roamTarget != null && AtRoamTarget())
        {
            waveHost.active?.ReleaseRoamPoint(gameObject);
            roamTarget = null;
            roamTimer = 0f;
            return;
        }

        if (roamTarget == null)
        {
            roamTimer += Time.deltaTime;
            if (roamTimer < config.roamWaitTime)
                return;
            roamTimer = 0f;
            if (Random.Range(0f, 1f) > config.roamChance)
                return;
            pickRoamPoint();
        }
    }

    // true once the agent has arrived at its roam target
    bool AtRoamTarget()
    {
        if (roamTarget == null)
            return false;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + config.roamArrivalDistance;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            agent.stoppingDistance = 0;
            playerInTrigger = false;
        }
    }

    // called by weapons before damage lands, so the kill can be credited correctly
    public void RegisterDamageSource(WeaponStats weapon, bool fromGround)
    {
        lastDamageWeapon = weapon;
        lastDamageFromGround = fromGround;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHP -= amount;

        if (GameManager.instance?.player != null)
        {
            if (!willRoam)
                agent.SetDestination(GameManager.instance.player.transform.position);
            else
            {
                isEngaged = true;
                agent.stoppingDistance = stoppingDistOrig;
            }
        }

        if (currentHP <= 0)
        {
            // Die() owns the byte award so it only ever fires once
            Die();
        }
        else if (model != null)
        {
            StartCoroutine(FlashBlack());
        }
    }

    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // ForceKill(false) is used by scorestreaks such as Data Purge.
        // Those kills still need to reduce the wave enemy count, but they
        // should not count as normal player kills/rewards.
        bool awardKillRewards = !suppressKillRewards;
        suppressKillRewards = false;

        if (awardKillRewards)
        {
            EnemyEvents.RaiseKilled(this);
        }

        // enemies talk to the wave through waveHost, not the singleton
        if (waveHost.active != null)
        {
            waveHost.active.EnemyKilled();
        }

        Destroy(gameObject);
    }

    // brief black flash on hit, gives feedback without an animation
    IEnumerator FlashBlack()
    {
        model.material.color = Color.black;
        yield return new WaitForSecondsRealtime(.1f);
        model.material.color = colorOrig;
    }

    // rotates to face the player, used before attacking so hits look intentional
    public void FaceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, config.turnSpeed * Time.deltaTime);
    }

    protected abstract void attack();

    // raycast at swing time, so a melee attack can still miss
    protected bool tryMeleeHit()
    {
        agent.stoppingDistance = Mathf.Max(0.5f, config.attackRange);
        float dist = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);
        if (dist > config.attackRange || attackTimer <= attackRate)
            return false;

        attackTimer = 0;
        GameManager.instance.player.GetComponent<IDamage>()?.TakeDamage(config.attackDamage);
        return true;
    }

    // Fork Bomb spreads damage through this, so it doesn't re-trigger the fork
    public void TakeSecondaryDamage(int amount)
    {
        TakeDamage(amount);
    }

    // kills without going through damage. Data Purge passes false so the kill
    // still reduces the wave count but awards nothing.
    public void ForceKill(bool countAsPlayerKill = true)
    {
        if (isDead)
            return;

        suppressKillRewards = !countAsPlayerKill;
        currentHP = 0;
        Die();
    }

    // drops the held weapon on death, re-enabling its pickup and physics so the
    // player can grab it. weapons only come from enemy drops per the gdd.
    public void ThrowWeapon(GameObject spawnedWeaponModel, Transform pivot)
    {
        if (spawnedWeaponModel == null)
            return;
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<WeaponWallAvoidance>(out WeaponWallAvoidance clip))
            clip.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<PickWeapon>(out var picker))
            picker.enabled = true;

        if (!spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody projectileRb))
        {
            projectileRb = spawnedWeaponModel.AddComponent<Rigidbody>();
        }

        projectileRb.isKinematic = false;
        projectileRb.useGravity = true;

        // Calculate directional trajectory
        Vector3 forceDirection = pivot.forward.normalized;

        // Apply forward and upward force
        Vector3 forceToAdd = forceDirection * 2f + transform.up * 0;
        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);
        // Add subtle spin for realistic throwing physics
        projectileRb.AddTorque(transform.right * 0.3f, ForceMode.Impulse);

        if (spawnedWeaponModel.TryGetComponent<Collider>(out Collider weaponCollider))
            weaponCollider.enabled = true;

        spawnedWeaponModel = null;
    }
}