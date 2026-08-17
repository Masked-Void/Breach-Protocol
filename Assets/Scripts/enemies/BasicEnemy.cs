using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemy : EnemyBase
{
    [Header("Melee")]
    [SerializeField] private GameObject weapon;
    [SerializeField] private Transform handPos;

    [SerializeField] private float attackRange = 2.25f;
    [SerializeField] private float stoppingDistance = 1.75f;
    [SerializeField] private float attackRate = 1f;

    [Header("Movement")]
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float navSampleRadius = 2f;

    private Quaternion katanaOrigRot;
    private Transform katanaTransform;

    private float attackRangeSqr;
    private float stoppingDistanceSqr;

    private float nextAttackTime;
    private float nextRepathTime;

    private playerController playerController;

    protected override void Start()
    {
        base.Start();

        attackRangeSqr = attackRange * attackRange;
        stoppingDistanceSqr = stoppingDistance * stoppingDistance;

        if (weapon != null && handPos != null)
        {
            GameObject weaponInstance = Instantiate(weapon, handPos);

            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            katanaTransform = weaponInstance.transform;
            katanaOrigRot = katanaTransform.localRotation;
        }

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
            MeleeAttack();
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

    private void MeleeAttack()
    {
        if (playerController == null &&
            playerTransform != null)
        {
            playerController =
                playerTransform.GetComponent<playerController>();
        }

        if (playerController == null)
            return;

        // Re-check range at the instant the hit occurs.
        Vector3 toPlayer =
            playerTransform.position - transform.position;

        if (toPlayer.sqrMagnitude > attackRangeSqr)
            return;

        playerController.takeDamage();

        if (katanaTransform != null)
        {
            StopCoroutine(nameof(KatanaSwing));
            StartCoroutine(KatanaSwing());
        }
    }

    private IEnumerator KatanaSwing()
    {
        if (katanaTransform == null)
            yield break;

        const float duration = 0.1f;

        Quaternion startRot = katanaOrigRot;

        Quaternion endRot =
            katanaOrigRot *
            Quaternion.Euler(
                28.9087696f,
                148.389023f,
                97.1623077f
            );

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            katanaTransform.localRotation =
                Quaternion.Lerp(
                    startRot,
                    endRot,
                    t
                );

            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            katanaTransform.localRotation =
                Quaternion.Lerp(
                    endRot,
                    startRot,
                    t
                );

            yield return null;
        }

        katanaTransform.localRotation =
            katanaOrigRot;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        attackRange = Mathf.Max(0.1f, attackRange);
        stoppingDistance =
            Mathf.Clamp(stoppingDistance, 0.1f, attackRange);

        attackRate = Mathf.Max(0.05f, attackRate);
        repathInterval = Mathf.Max(0.02f, repathInterval);
        navSampleRadius = Mathf.Max(0.1f, navSampleRadius);
    }
}
