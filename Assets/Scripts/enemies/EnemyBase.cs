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
    [Range(15, 180)][SerializeField] protected float FOV = 90f;
    [Range(.1f, 5)] public float attackRate = 1.5f;
    [Range(1, 20)] public float attackRange = 2f;
    [Range(1, 20)] public int attackDamage = 1;

    [Header("Roaming")]
    [SerializeField] float roamDist = 10f;
    [SerializeField] float roamWaitTime = 1.1f;
    float roamTimer;
    Vector3 startingPos;

    protected bool playerInTrigger;
    protected float angleToPlayer;
    protected float stoppingDistOrig;
    protected float attackTimer;

    protected Vector3 playerDir;
    public bool hasLeftSpawnRoom = false;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
        startingPos = transform.position;
        stoppingDistOrig = agent.stoppingDistance;

        if (model != null)
            colorOrig = model.material.color;
    }

    void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;
        attackTimer += Time.unscaledDeltaTime;
        if (playerInTrigger && canSeePlayer())
        {
        }
        else
        {
            checkRoam();
        }
    }

    public virtual bool canSeePlayer()
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
        return true;
    }

    public virtual void checkRoam()
    {
        if (agent.remainingDistance < 0.01f)
        {
            roamTimer += Time.deltaTime;
            if (roamTimer > roamWaitTime) roam();
        }
    }

    public virtual void roam()
    {
        roamTimer = 0;
        agent.stoppingDistance = 0;
        Vector3 ranPos = Random.insideUnitSphere * roamDist + startingPos;
        if (NavMesh.SamplePosition(ranPos, out NavMeshHit hit, roamDist, 1))
            agent.SetDestination(hit.position);
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

    IEnumerator FlashBlack()
    {
        model.material.color = Color.black;
        yield return new WaitForSeconds(.1f);
        model.material.color = colorOrig;
    }

    public void faceTarget()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, faceTargetSpeed * Time.deltaTime);
    }

    protected abstract void attack();

    public virtual void die()
    {
        waveManager.instance.enemyKilled();
        if (killChainManager.instance != null)
        {
            killChainManager.instance.RegisterKill();
        }
        Destroy(gameObject);
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