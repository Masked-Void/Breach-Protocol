using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class heavyEnemy : EnemyBase
{
    [Header("Heavy Melee")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float stoppingDistance = 2f;
    [SerializeField] private float attackRate = 1.25f;

    [Header("Pushback")]
    [SerializeField] private float pushbackForce = 2f;

    [Header("Movement")]
    [SerializeField] private float repathInterval = 0.18f;
    [SerializeField] private float navSampleRadius = 2f;

    private float attackRangeSqr;
    private float stoppingDistanceSqr;

    private float nextAttackTime;
    private float nextRepathTime;

    private playerController playerController;

    protected override void Start()
    {
        base.Start();

        attackRangeSqr = attackRange * attackRange;
        stoppingDistanceSqr =
            stoppingDistance * stoppingDistance;

        if (playerTransform != null)
        {
            playerController =
                playerTransform.GetComponent<playerController>();
        }

        nextRepathTime =
            Time.unscaledTime + Random.Range(0f, repathInterval);
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null)
            return;

        float distanceSqr = playerDir.sqrMagnitude;

        UpdateMovement(distanceSqr);

        if (distanceSqr <= attackRangeSqr &&
            Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackRate;
            HeavyAttack();
        }
    }

    private void UpdateMovement(float distanceSqr)
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        if (distanceSqr <= stoppingDistanceSqr)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;

                if (agent.hasPath)
                    agent.ResetPath();
            }

            return;
        }

        if (agent.isStopped)
            agent.isStopped = false;

        if (Time.unscaledTime < nextRepathTime ||
            agent.pathPending)
        {
            return;
        }

        nextRepathTime =
            Time.unscaledTime + repathInterval;

        if (NavMesh.SamplePosition(
            playerTransform.position,
            out NavMeshHit hit,
            navSampleRadius,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void HeavyAttack()
    {
        if (playerController == null &&
            playerTransform != null)
        {
            playerController =
                playerTransform.GetComponent<playerController>();
        }

        if (playerController == null)
            return;

        Vector3 toPlayer =
            playerTransform.position - transform.position;

        if (toPlayer.sqrMagnitude > attackRangeSqr)
            return;

        // Standard enemy damage still flows through the player's
        // heartbeat/stress damage handling.
        playerController.takeDamage();

        Vector3 pushDir = toPlayer;
        pushDir.y = 0f;

        if (pushDir.sqrMagnitude > 0.0001f)
        {
            pushDir.Normalize();

            playerController.PushBack(
                pushDir,
                pushbackForce
            );
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        attackRange = Mathf.Max(0.1f, attackRange);
        stoppingDistance =
            Mathf.Clamp(stoppingDistance, 0.1f, attackRange);

        attackRate = Mathf.Max(0.05f, attackRate);
        pushbackForce = Mathf.Max(0f, pushbackForce);

        repathInterval = Mathf.Max(0.02f, repathInterval);
        navSampleRadius = Mathf.Max(0.1f, navSampleRadius);
    }
}
