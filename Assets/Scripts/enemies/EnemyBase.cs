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
        if(gameManager.instance != null && gameManager.instance.isPaused) return;
        attackTimer += Time.unscaledDeltaTime;
        if (playerInTrigger && canSeePlayer())
        {
        }
        else
        {
            checkRoam();
        }
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
        return true;
    }

    void checkRoam()
    {
        if (agent.remainingDistance < 0.01f)
        {
            roamTimer += Time.deltaTime;
            if (roamTimer > roamWaitTime) roam();
        }
    }

    void roam()
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
            waveManager.instance.enemyKilled();
            FindAnyObjectByType<killChainManager>()?.RegisterKill();
            //gameManager.updateGameGoal(-1);
            Destroy(gameObject);
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

    public void ForceKill()
    {
        waveManager.instance.enemyKilled();
        FindAnyObjectByType<killChainManager>()?.RegisterKill();
        Destroy(gameObject);
    }

    public virtual void SetWeaponPrefab(GameObject weaponPrefab)
    {
        // This method can be overridden in derived classes to set the weapon prefab.
    }
}