using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
public abstract class EnemyBase : MonoBehaviour, IDamage
{
    [Header("Health")]
    [Tooltip("GDD default is one-hit regular enemies.")]
    [SerializeField] protected int maxHP = 1;
    [SerializeField] protected int currentHP;

    [Header("Movement")]
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] protected float faceTargetSpeed = 540f;

    [Header("Visuals")]
    [Tooltip("Optional. All child renderers are cached automatically.")]
    [SerializeField] protected Renderer model;
    [SerializeField] protected Material flashMaterial;
    [SerializeField] private float damageFlashDuration = 0.08f;

    [Header("Prefab Setup")]
    [SerializeField] private bool disableRootMotion = true;
    [SerializeField] private bool autoConfigureBodyCollider = true;
    [SerializeField] private Vector3 bodyColliderCenter = new Vector3(0f, 1f, 0f);
    [SerializeField] private float bodyColliderHeight = 2f;
    [SerializeField] private float bodyColliderRadius = 0.45f;
    [SerializeField] private float spawnNavMeshSnapRadius = 2f;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected CapsuleCollider bodyCollider;
    protected Transform playerTransform;
    protected Vector3 playerDir;
    protected bool isDead;

    public bool IsDead => isDead;

    private Renderer[] cachedRenderers;
    private Material[][] originalMaterials;
    private Material[][] flashMaterials;
    private Coroutine flashRoutine;

    protected abstract void UpdateBehavior();

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>(true);

        if (bodyCollider != null && autoConfigureBodyCollider)
        {
            bodyCollider.isTrigger = false;
            bodyCollider.direction = 1;
            bodyCollider.center = bodyColliderCenter;
            bodyCollider.height = Mathf.Max(bodyColliderHeight, bodyColliderRadius * 2f);
            bodyCollider.radius = bodyColliderRadius;
        }

        if (agent != null)
            agent.updateRotation = false;

        if (animator != null && disableRootMotion)
            animator.applyRootMotion = false;
    }

    protected virtual void Start()
    {
        currentHP = Mathf.Max(1, maxHP);

        CacheRenderers();
        FindPlayer();
        SnapToNavMeshIfNeeded();
    }

    protected virtual void Update()
    {
        if (isDead || playerTransform == null)
            return;

        if (gameManager.instance != null && gameManager.instance.isPaused)
            return;

        // DDoS suspends enemy AI but does not freeze existing world projectiles.
        if (killstreakManager.instance != null &&
            killstreakManager.instance.AreEnemiesJammed)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
                if (agent.hasPath)
                    agent.ResetPath();
            }

            return;
        }

        playerDir = playerTransform.position - transform.position;

        FaceTarget();
        UpdateBehavior();
    }

    private void FindPlayer()
    {
        if (gameManager.instance != null && gameManager.instance.player != null)
        {
            playerTransform = gameManager.instance.player.transform;
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");

        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    private void SnapToNavMeshIfNeeded()
    {
        if (agent == null || !agent.enabled || agent.isOnNavMesh)
            return;

        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            spawnNavMeshSnapRadius,
            NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if ((cachedRenderers == null || cachedRenderers.Length == 0) && model != null)
            cachedRenderers = new Renderer[] { model };

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return;

        originalMaterials = new Material[cachedRenderers.Length][];
        flashMaterials = new Material[cachedRenderers.Length][];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];

            if (renderer == null)
                continue;

            Material[] originals = renderer.sharedMaterials;
            originalMaterials[i] = originals;

            Material[] flashes = new Material[originals.Length];

            for (int m = 0; m < flashes.Length; m++)
                flashes[m] = flashMaterial != null ? flashMaterial : originals[m];

            flashMaterials[i] = flashes;
        }
    }

    public virtual void takeDamage(int amount)
    {
        ApplyDamage(amount, true, true);
    }

    /// <summary>
    /// Used by Fork Bomb. Counts as a player kill, but does not recursively
    /// start another chain reaction.
    /// </summary>
    public void TakeSecondaryDamage(int amount)
    {
        ApplyDamage(amount, true, false);
    }

    private void ApplyDamage(int amount, bool countsAsPlayerKill, bool allowChainReaction)
    {
        if (isDead || amount <= 0)
            return;

        // Root Access turns any damaging hit into an execution on a regular enemy.
        if (countsAsPlayerKill &&
            killstreakManager.instance != null &&
            killstreakManager.instance.IsRootAccessActive)
        {
            // Score/refund the directly hit enemy before any propagated kills
            // can lower stress and change this kill's score value.
            RequestDeath(true);

            if (allowChainReaction && killstreakManager.instance != null)
                killstreakManager.instance.TriggerChainReaction(this, amount);

            return;
        }

        currentHP -= amount;

        if (currentHP <= 0)
        {
            // Score/refund the directly hit enemy first.
            RequestDeath(countsAsPlayerKill);

            if (allowChainReaction &&
                countsAsPlayerKill &&
                killstreakManager.instance != null)
            {
                killstreakManager.instance.TriggerChainReaction(this, amount);
            }

            return;
        }

        if (allowChainReaction &&
            countsAsPlayerKill &&
            killstreakManager.instance != null)
        {
            killstreakManager.instance.TriggerChainReaction(this, amount);
        }

        if (flashMaterial != null &&
            cachedRenderers != null &&
            cachedRenderers.Length > 0)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashDamage());
        }
    }

    private bool pendingDeathCountsAsPlayerKill = true;

    private void RequestDeath(bool countsAsPlayerKill)
    {
        if (isDead)
            return;

        pendingDeathCountsAsPlayerKill = countsAsPlayerKill;
        Die();
    }

    /// <summary>
    /// Scorestreak/environment forced kills should normally pass false so they
    /// clear the wave without creating scorestreak recursion or kill credit.
    /// Optional parameter preserves old ForceKill() call sites.
    /// </summary>
    public void ForceKill(bool countsAsPlayerKill = false)
    {
        RequestDeath(countsAsPlayerKill);
    }

    /// <summary>
    /// Kept as the original virtual signature so Heavy/Basic/etc. subclasses
    /// that override Die() continue to compile. Derived overrides should call base.Die().
    /// </summary>
    protected virtual void Die()
    {
        if (isDead)
            return;

        bool countsAsPlayerKill = pendingDeathCountsAsPlayerKill;
        pendingDeathCountsAsPlayerKill = true;

        isDead = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // IMPORTANT: calculate score before the kill's stress relief.
        if (countsAsPlayerKill)
        {
            if (scoreManager.instance != null)
                scoreManager.instance.RegisterKill();

            if (killChainManager.instance != null)
                killChainManager.instance.RegisterKill();

            if (killstreakManager.instance != null)
                killstreakManager.instance.NotifyPlayerKill();

            if (heartbeatManager.instance != null)
                heartbeatManager.instance.enemyKilled();
        }

        NotifyWaveManager();
        OnDeath();

        Destroy(gameObject);
    }

    protected virtual void NotifyWaveManager()
    {
        if (waveManager.instance != null)
            waveManager.instance.enemyKilled();
    }

    /// <summary>
    /// Original no-argument hook preserved for existing enemy subclasses.
    /// </summary>
    protected virtual void OnDeath()
    {
    }

    private IEnumerator FlashDamage()
    {
        SetFlashMaterials(true);

        yield return new WaitForSecondsRealtime(damageFlashDuration);

        SetFlashMaterials(false);
        flashRoutine = null;
    }

    private void SetFlashMaterials(bool useFlash)
    {
        if (cachedRenderers == null)
            return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials = useFlash ? flashMaterials[i] : originalMaterials[i];

            if (materials != null)
                renderer.sharedMaterials = materials;
        }
    }

    protected virtual void FaceTarget()
    {
        Vector3 flatDirection = new Vector3(playerDir.x, 0f, playerDir.z);

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceTargetSpeed * Time.deltaTime
        );
    }

    protected Transform FindChildRecursive(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }

    protected virtual void OnValidate()
    {
        maxHP = Mathf.Max(1, maxHP);
        faceTargetSpeed = Mathf.Max(0f, faceTargetSpeed);
        damageFlashDuration = Mathf.Max(0f, damageFlashDuration);
        bodyColliderRadius = Mathf.Max(0.05f, bodyColliderRadius);
        bodyColliderHeight = Mathf.Max(bodyColliderRadius * 2f, bodyColliderHeight);
        spawnNavMeshSnapRadius = Mathf.Max(0.1f, spawnNavMeshSnapRadius);
    }
}
