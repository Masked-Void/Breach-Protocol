using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour, IDamage
{
    [Header("Visuals")]
    [SerializeField] public Renderer model;
    Color colorOrig;

    [Header("Agent")]
    [SerializeField] public NavMeshAgent agent;

    [Header("Stats")]
    int currentHP;
    [Range(1, 50)][SerializeField] int maxHP;
    [Range(1, 30)][SerializeField] float faceTargetSpeed = 8f;
    [Range(15, 180)][SerializeField] float FOV = 120f;
    [Range(.1f, 5)][SerializeField] public float attackRate = 1.5f;
    [Range(1, 20)][SerializeField] public float attackRange = 2f;
    [Range(1, 20)][SerializeField] public int attackDamage = 1;
    [Range(1, 20)][SerializeField] public float rangedEnemeyAttackRange = 15f;

    [Header("Roaming")]
    [SerializeField] float roamWaitTime = 1.1f;
    float roamTimer;
    public Transform roamTarget;
    [SerializeField] float roamArriveDistance = 0.1f;
    [SerializeField] float roamChance = .1f;

    [Header("Currency")]
    int byteValue = 5;


    protected bool playerInTrigger;
    protected float angleToPlayer;
    protected float stoppingDistOrig;
    protected float attackTimer;

    protected Vector3 playerDir;

    [Header("Spawn and Roam")]
    public bool hasLeftSpawnRoom = false;
    public bool willRoam = false;
    [SerializeField] GameObject roamPoint;
    protected bool isEngaged = false;
    [Header("Challenge")]
    protected WeaponStats lastDamageWeapon;
    protected bool lastDamageFromGround;

    bool hasGameManager;
    bool hasAudioManager;
    bool hasWeaponManager;
    bool hasChallengeManager;
    bool hasUpgradeManager;

    [Header("Footsteps")]
    [SerializeField] float stepInterval = 0.5f;
    [SerializeField] float movementThreshold = 0.1f;
    float stepTimer;

    [Header("Ranged AI Settings")]
    [Range(5f, 30f)][SerializeField] float maxRoamDistanceFromPlayer = 15f; // distance ranged ai should stay within player

    // small compatibility state used by the scorestreak system
    private bool isDead;
    private bool suppressKillRewards;

    public bool IsDead => isDead;
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
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
        if (GameManager.instance != null && GameManager.instance.isPaused) return;
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
                agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 0.5f);
                agent.SetDestination(GameManager.instance.player.transform.position);
                playerDir = GameManager.instance.player.transform.position - transform.position;
                faceTarget();
                attack();
            }
        }
        else if (isEngaged)
        {
            // Ranged: now chasing the player
            if (GameManager.instance?.player != null)
            {
                //check to disengage
                if (!canStillSeePlayer())
                {
                    isEngaged = false;
                    agent.stoppingDistance = 0;

                    //clear old roam point so pick a new one
                    if (roamTarget != null)
                    {
                        waveHost.active?.releaseRoamPoint(gameObject);
                        roamTarget = null;
                    }
                    return;
                }
                agent.SetDestination(GameManager.instance.player.transform.position);
                playerDir = GameManager.instance.player.transform.position - transform.position;
                faceTarget();


                float distance = playerDir.magnitude;
              
                
                if (distance <= rangedEnemeyAttackRange)
                {
                 attack();
                }
            }
        }
        else
        {
            // Ranged: roaming � only look around while stopped at a roam point
            roam();

            
                if (tryAttackFromCurrentPosition())
                {
                    isEngaged = true;
                    agent.stoppingDistance = stoppingDistOrig;
                }
            
        }
    }

    void pickRoamPoint()
    {
        if (waveHost.active == null) return;

        waveHost.active.releaseRoamPoint(gameObject);

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
    Transform findNearestRoamPointToPlayer()
    {
        if(waveHost.active == null) return null;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Transform closestPoint = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < 10; i++)
        {
            Transform candidatePoint = waveHost.active.claimRoamPoint(gameObject);
            if (candidatePoint == null) break;

            float distToPlayer = Vector3.Distance(candidatePoint.position , playerPos);

            if (distToPlayer <= maxRoamDistanceFromPlayer && distToPlayer < closestDistance)
            {
                if (closestPoint != null)
                {
                    //need to check this
                }
                closestPoint= candidatePoint;
                closestDistance= distToPlayer;
            }
            else
            {
                waveHost.active.releaseRoamPoint(gameObject);
            }
        }
        return closestPoint;
    }



    void HandleFootSteps()
    {
        if (agent.velocity.magnitude > movementThreshold)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;

                if (AudioManager.instance != null && AudioManager.instance.enemySteps != null && AudioManager.instance.enemySteps.Length > 0)
                {
                    AudioManager.instance.playSpatialSFX(AudioManager.instance.pickRandomAudio(AudioManager.instance.enemySteps), transform.position, AudioManager.instance.enemyStepsVol, 3f, 20f);
                }
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // need this so ai goes back to roaming after losing sight. 
    private bool canStillSeePlayer()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return false;

        Vector3 dirToPlayer = GameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, rangedEnemeyAttackRange))
        {
            return hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player") && angleToPlayer <= FOV;
        }
        return false;
    }

    // Attempt to see and attack the player without changing the agent's destination.
    // Returns true if the player was visible and an attack/face action was triggered.
    protected bool tryAttackFromCurrentPosition()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return false;

        Vector3 dirToPlayer = GameManager.instance.player.transform.position - transform.position;
        float distance = dirToPlayer.magnitude;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (willRoam && distance > rangedEnemeyAttackRange)
        {
            return false;
        }

        if (Physics.Raycast(transform.position,dirToPlayer.normalized, out RaycastHit hit, rangedEnemeyAttackRange))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= FOV)
            {
                playerDir = dirToPlayer;
                faceTarget();
                attack();
                return true;
            }
        }

        return false;
    }

    // ensures player is within a range or FOV so they can be seen
    public virtual bool canSeePlayer()
    {
        playerDir = GameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, playerDir, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= FOV)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                faceTarget();

                attack();
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }

  
    public virtual void roam()
    {

        //Check distance from player
        if (GameManager.instance?.player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position,GameManager.instance.player.transform.position);

            // if too far hunt them down
            if (distToPlayer > rangedEnemeyAttackRange)
            {
                agent.SetDestination(GameManager.instance.player.transform.position);
                agent.stoppingDistance = 0f;

                //clear roam target to hunt
                if (roamTarget != null)
                {
                    waveHost.active?.releaseRoamPoint(gameObject);
                    roamTarget = null;
                }
                return;
            }
        }

        // within range is normal roaming

        if (roamTarget != null && AtRoamTarget())
        {
            waveHost.active?.releaseRoamPoint(gameObject);
            roamTarget = null;
            roamTimer = 0f;
            return;
        }

        if (roamTarget == null)
        {
            roamTimer += Time.deltaTime;
            if (roamTimer < roamWaitTime) return;
            roamTimer = 0f;
            if (Random.Range(0f, 1f) > roamChance) return;
            pickRoamPoint();
        }
    }
    bool AtRoamTarget()
    {
        if (roamTarget == null) return false;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + roamArriveDistance;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            agent.stoppingDistance = 0;
            playerInTrigger = false;
        }
    }
    public void RegisterDamageSource(WeaponStats weapon, bool fromGround)
    {
        lastDamageWeapon = weapon;
        lastDamageFromGround = fromGround;
    }
    public void takeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

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
            // die() owns the byte award so it only ever fires once
            die();
        }
        else if (model != null)
        {
            StartCoroutine(FlashBlack());
        }
    }

    public virtual void die()
    {
        if (isDead)
            return;

        isDead = true;

        // ForceKill(false) is used by scorestreaks such as Data Purge.
        // Those kills still need to reduce the wave enemy count, but they
        // should not count as normal player kills/rewards.
        bool awardKillRewards = !suppressKillRewards;
        suppressKillRewards = false;

        // REPORT TO CHALLENGE SYSTEM
        if (awardKillRewards && lastDamageWeapon != null)
        {
            // ChallengeManager.instance?.ReportKill(lastDamageWeapon, lastDamageFromGround);
            //ChallengeManager.instance?.ReportKill(WeaponManager.instance.activeWeapon);
            ChallengeManager.instance?.ReportKill(lastDamageWeapon);
        }

        // enemies talk to the wave through waveHost, not the singleton
        if (waveHost.active != null)
        {
            waveHost.active.enemyKilled();
        }

        if (awardKillRewards)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.addKill();
                GameManager.instance.AddBytes(byteValue);
            }

            if (KillChainManager.instance != null)
            {
                KillChainManager.instance.RegisterKill();
            }
        }

        Destroy(gameObject);
    }

    IEnumerator FlashBlack()
    {
        model.material.color = Color.black;
        yield return new WaitForSecondsRealtime(.1f);
        model.material.color = colorOrig;
    }

    public void faceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, faceTargetSpeed * Time.deltaTime);
    }

    protected abstract void attack();

    protected bool tryMeleeHit()
    {
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange);
        float dist = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);
        if (dist > attackRange || attackTimer <= attackRate) return false;

        attackTimer = 0;
        GameManager.instance.player.GetComponent<IDamage>()?.takeDamage(attackDamage);
        return true;
    }

    // Chain Reaction uses this so secondary damage has a clear entry point.
    // Right now it intentionally behaves like normal enemy damage.
    public void TakeSecondaryDamage(int amount)
    {
        takeDamage(amount);
    }

    // Existing code can still call ForceKill() with no arguments.
    // Scorestreak/environment kills can pass false to suppress player rewards.
    public void ForceKill(bool countAsPlayerKill = true)
    {
        if (isDead)
            return;

        suppressKillRewards = !countAsPlayerKill;
        currentHP = 0;
        die();
    }

    public void throwWeapon(GameObject spawnedWeaponModel, Transform pivot)
    {
        if (spawnedWeaponModel == null) return;
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<WeaponWallAvoidance>(out WeaponWallAvoidance clip)) clip.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<PickWeapon>(out var picker)) picker.enabled = true;

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

        if (spawnedWeaponModel.TryGetComponent<Collider>(out Collider weaponCollider)) weaponCollider.enabled = true;

        spawnedWeaponModel = null;
    }
}