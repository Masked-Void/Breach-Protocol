using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject bullet;

    [Tooltip("Optional manual muzzle. Leave empty for Robot_Soldier_Black; the RightHand bone is used automatically.")]
    [SerializeField] private Transform shootPos;

    [SerializeField] private float shootRate = 1f;
    [SerializeField] private float shootingRange = 18f;
    [SerializeField] private float aimHeight = 1f;

    [Header("Robot Soldier Muzzle")]
    [SerializeField] private float muzzleForwardOffset = 0.35f;
    [SerializeField] private float muzzleUpOffset = 0.02f;
    [SerializeField] private float muzzleRightOffset = 0f;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 8f;
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float navSampleRadius = 2f;

    private Transform rightHand;
    private float shootingRangeSqr;
    private float stoppingDistanceSqr;
    private float nextShotTime;
    private float nextRepathTime;

    protected override void Start()
    {
        base.Start();

        shootingRangeSqr = shootingRange * shootingRange;
        stoppingDistanceSqr = stoppingDistance * stoppingDistance;

        FindRobotSoldierHand();

        nextRepathTime =
            Time.unscaledTime + Random.Range(0f, repathInterval);
    }

    private void FindRobotSoldierHand()
    {
        if (animator != null && animator.isHuman)
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightHand == null)
            rightHand = FindChildRecursive("RightHand");

        if (shootPos == null && rightHand == null)
        {
            Debug.LogWarning(
                "RangedEnemy could not find RightHand. Assign Shoot Pos manually.",
                this
            );
        }
    }

    protected override void UpdateBehavior()
    {
        if (playerTransform == null)
            return;

        float distanceSqr = playerDir.sqrMagnitude;

        UpdateMovement(distanceSqr);

        if (distanceSqr <= shootingRangeSqr &&
            Time.time >= nextShotTime)
        {
            Shoot();
        }
    }

    private void UpdateMovement(float distanceSqr)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

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

        if (Time.unscaledTime < nextRepathTime || agent.pathPending)
            return;

        nextRepathTime = Time.unscaledTime + repathInterval;

        if (NavMesh.SamplePosition(
            playerTransform.position,
            out NavMeshHit hit,
            navSampleRadius,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void Shoot()
    {
        nextShotTime = Time.time + shootRate;

        if (bullet == null)
        {
            Debug.LogWarning("RangedEnemy is missing its Bullet prefab.", this);
            return;
        }

        Vector3 origin = GetMuzzlePosition();
        Vector3 target = playerTransform.position + Vector3.up * aimHeight;
        Vector3 direction = target - origin;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // Ghost Protocol intentionally corrupts enemy aim.
        if (killstreakManager.instance != null)
        {
            direction = killstreakManager.instance.ApplyGhostAimError(direction);
        }

        Quaternion shotRotation =
            Quaternion.LookRotation(direction, Vector3.up);

        Instantiate(bullet, origin, shotRotation);
    }

    private Vector3 GetMuzzlePosition()
    {
        if (shootPos != null)
            return shootPos.position;

        if (rightHand != null)
        {
            return rightHand.position
                + transform.forward * muzzleForwardOffset
                + transform.up * muzzleUpOffset
                + transform.right * muzzleRightOffset;
        }

        return transform.position
            + transform.up * 1.35f
            + transform.forward * 0.5f;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        shootRate = Mathf.Max(0.05f, shootRate);
        shootingRange = Mathf.Max(0.1f, shootingRange);
        stoppingDistance = Mathf.Clamp(stoppingDistance, 0.1f, shootingRange);
        repathInterval = Mathf.Max(0.02f, repathInterval);
        navSampleRadius = Mathf.Max(0.1f, navSampleRadius);
    }
}
