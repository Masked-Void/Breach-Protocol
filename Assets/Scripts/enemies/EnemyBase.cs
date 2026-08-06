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
    [Range(15, 180)][SerializeField] float FOV = 90f;
    [Range(.1f, 5)][SerializeField] public float attackRate = 1.5f;
    [Range(1, 20)][SerializeField] public float attackRange = 2f;
    [Range(1, 20)][SerializeField] public int attackDamage = 1;

    [Header("Roaming")]
    [SerializeField] float roamWaitTime = 1.1f;
    float roamTimer;
    [SerializeField] GameObject fixedRoamPos;
    [SerializeField] float roamChance = .1f;

    protected bool playerInTrigger;
    protected float angleToPlayer;
    protected float stoppingDistOrig;
    protected float attackTimer;

    protected Vector3 playerDir;

    [Header("Spawn and Roam")]
    public bool hasLeftSpawnRoom = false;
    public bool willRoam = false;
    [SerializeField] GameObject roamPoint;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
        stoppingDistOrig = agent.stoppingDistance;

        roamFixed();

        if (model != null)
            colorOrig = model.material.color;
    }

    void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;
        attackTimer += Time.deltaTime;

        // If this enemy is set to roam, never chase the player.
        if (!willRoam && playerInTrigger && canSeePlayer())
        {
            // chase + attack happen inside canSeePlayer
        }
        else if (willRoam)
        {
            // While roaming, don't abandon roam to chase, but still attempt to attack if player is visible/within range.
            tryAttackFromCurrentPosition();
            checkRoamFixed();
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
        // Debug.DrawRay(transform.position, playerDir);

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

    // Legacy roaming methods removed; fixed roaming (roamFixed) is used instead.

    void checkRoamFixed()
    {
        if (agent.remainingDistance < 0.00001f)
        {
            roamTimer += Time.deltaTime;
            if (roamTimer > roamWaitTime)
            {
                float randomNumber = Random.Range(0f, 1f);
                if (randomNumber < roamChance)
                {
                    roamFixed();
                }
                else
                {
                    roamTimer = 0;
                }
            }
        }
    }

    void roamFixed()
    {
        roamTimer = 0;
        if (waveManager.instance != null)
        {
            agent.SetDestination(waveManager.instance.newRoamPos());
        }
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

    public void takeDamage(int amount)
    {
        currentHP -= amount;
        if (gameManager.instance?.player != null)
            agent.SetDestination(gameManager.instance.player.transform.position);

        if (currentHP <= 0)
        {
            die();
        }
        else if (model != null)
        {
            StartCoroutine(FlashBlack());
        }
    }

    void die()
    {
        waveManager.instance.enemyKilled();
        if (gameManager.instance != null)
        {
            gameManager.instance.addKill();
        }
        if (killChainManager.instance != null)
        {
            killChainManager.instance.RegisterKill();
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
}