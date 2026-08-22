using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class enemyBase : MonoBehaviour, IDamage
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
    [Range(15, 180)][SerializeField] float FOV = 90f;
    [Range(.1f, 5)][SerializeField] public float attackRate = 1.5f;
    [Range(1, 20)][SerializeField] public float attackRange = 2f;
    [Range(1, 20)][SerializeField] public int attackDamage = 1;

    [Header("Roaming")]
    [SerializeField] float roamWaitTime = 1.1f;
    float roamTimer;
    public Transform roamTarget;
    [SerializeField] float roamArriveDistance = 0.1f;
    [SerializeField] float roamChance = .1f;

    [Header("Currency")]
    [SerializeField] int byteValue = 5;


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
    protected weaponStats lastDamageWeapon;
    protected bool lastDamageFromGround;
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
        stoppingDistOrig = agent.stoppingDistance;

        pickRoamPoint();

        if (model != null)
            colorOrig = model.material.color;
    }

    void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;
        attackTimer += Time.deltaTime;

        if (!willRoam)
        {
            // Heavy / Basic: finish first roam point, then b-line player forever
            if (roamTarget != null)
            {
                if (AtRoamTarget())
                {
                    waveManager.instance?.releaseRoamPoint(gameObject);
                    roamTarget = null;
                    agent.stoppingDistance = stoppingDistOrig;
                }
            }
            else if (gameManager.instance?.player != null)
            {
                agent.SetDestination(gameManager.instance.player.transform.position);
                playerDir = gameManager.instance.player.transform.position - transform.position;
                faceTarget();
                attack();
            }
        }
        else if (isEngaged)
        {
            // Ranged: now chasing the player
            if (gameManager.instance?.player != null)
            {
                agent.SetDestination(gameManager.instance.player.transform.position);
                playerDir = gameManager.instance.player.transform.position - transform.position;
                faceTarget();
                attack();
            }
        }
        else
        {
            // Ranged: roaming   only look around while stopped at a roam point
            roam();

            if (roamTarget == null && playerInTrigger)
            {
                if (tryAttackFromCurrentPosition())
                {
                    isEngaged = true;
                    agent.stoppingDistance = stoppingDistOrig;
                }
            }
        }
    }

    // Attempt to see and attack the player without changing the agent's destination.
    // Returns true if the player was visible and an attack/face action was triggered.
    protected bool tryAttackFromCurrentPosition()
    {
        if (gameManager.instance == null || gameManager.instance.player == null) return false;

        Vector3 dir = gameManager.instance.player.transform.position - transform.position;
        float angle = Vector3.Angle(dir, transform.forward);

        if (angle > FOV) return false;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player"))
            {
                playerDir = dir;
                faceTarget();
                attack();
                return true;
            }
        }

        return false;
    }

    bool canSeePlayer()
    {
        playerDir = gameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, playerDir, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= FOV)
            {
                agent.SetDestination(gameManager.instance.player.transform.position);
                faceTarget();

                attack();
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }

    void pickRoamPoint()
    {
        if (waveManager.instance == null) return;

        waveManager.instance.releaseRoamPoint(gameObject);

        Transform nextRoamPoint = waveManager.instance.claimRoamPoint(gameObject);

        if (nextRoamPoint == null) return;

        roamTarget = nextRoamPoint;
        agent.stoppingDistance = 0f;
        agent.SetDestination(roamTarget.position);
    }

    public virtual void roam()
    {
        if (roamTarget != null && AtRoamTarget())
        {
            waveManager.instance?.releaseRoamPoint(gameObject);
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
    public void RegisterDamageSource(weaponStats weapon, bool fromGround)
    {
        lastDamageWeapon = weapon;
        lastDamageFromGround = fromGround;
    }
    public void takeDamage(int amount)
    {
        currentHP -= amount;

        if (gameManager.instance?.player != null)
        {
            if (!willRoam)
                agent.SetDestination(gameManager.instance.player.transform.position);
            else
            {
                isEngaged = true;
                agent.stoppingDistance = stoppingDistOrig;
            }
        }

        if (currentHP <= 0)
        {
            die();
            gameManager.instance.AddBytes(byteValue);
        }
        else if (model != null)
        {
            StartCoroutine(FlashBlack());
        }
    }

    public virtual void die()
    {
        // REPORT TO CHALLENGE SYSTEM
        if (lastDamageWeapon != null)
        {
            challengeManager.instance?.ReportKill(lastDamageWeapon, lastDamageFromGround);
        }
        waveManager.instance.enemyKilled();
        if (gameManager.instance != null)
        {
            gameManager.instance.addKill();
            gameManager.instance.AddBytes(byteValue);
        }
        if (killChainManager.instance != null)
        {
            killChainManager.instance.RegisterKill();
        }
        Destroy(gameObject);
    }

    IEnumerator FlashBlack() //when enemy gets damaged they flash black
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
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 0.5f);
        float dist = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);
        if (dist > attackRange || attackTimer <= attackRate) return false;

        attackTimer = 0;
        gameManager.instance.player.GetComponent<IDamage>()?.takeDamage(attackDamage);
        return true;
    }

    public void ForceKill()
    {
        die();
    }

    public void throwWeapon(GameObject spawnedWeaponModel, Transform pivot)
    {
        if (spawnedWeaponModel == null) return;
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<pickWeapon>(out var picker)) picker.enabled = true;

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