using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillstreakManager : MonoBehaviour
{
    public static KillstreakManager instance;

    [Header("Scorestreak Pool")]
    [Tooltip("Attach the scorestreak components to this manager object and drag them here.")]
    [SerializeField] private KillstreakBase[] streakPool;

    [Tooltip("Off by default. When false, only one timed/manual streak can be active at once.")]
    [SerializeField] private bool allowStacking = false;

    [Header("Stored Scorestreak UI")]
    [SerializeField] private TMP_Text slot3Text;
    [SerializeField] private string emptySlotText = "EMPTY";

    [Header("Packet Leech")]
    [Tooltip("Optional. Assign a component implementing IAmmoRefundReceiver. If empty, the player hierarchy is searched automatically.")]
    [SerializeField] private MonoBehaviour ammoRefundReceiverSource;

    [Header("Runtime Effect State")]
    [SerializeField] private bool rootAccessActive;
    [SerializeField] private bool chainReactionActive;
    [SerializeField] private bool invulnerableActive;
    [SerializeField] private bool ghostProtocolActive;
    [SerializeField] private bool packetLeechActive;
    [SerializeField] private bool enemiesJammed;

    [SerializeField] private float chainReactionRadius = 4f;
    [SerializeField, Range(0f, 1f)] private float chainReactionDamagePercent = 0.5f;
    [SerializeField] private float ghostAimErrorDegrees = 12f;
    [SerializeField] private int ammoRefundPerKill = 1;

    private KillstreakBase storedStreak;

    private readonly HashSet<KillstreakBase> activeStreaks =
        new HashSet<KillstreakBase>();

    private readonly HashSet<EnemyBase> chainReactionVictims =
        new HashSet<EnemyBase>();

    private readonly Collider[] chainReactionBuffer =
        new Collider[64];

    private IAmmoRefundReceiver ammoRefundReceiver;

    public bool IsRootAccessActive => rootAccessActive;
    public bool IsInvulnerable => invulnerableActive;
    public bool IsGhostProtocolActive => ghostProtocolActive;
    public bool IsPacketLeechActive => packetLeechActive;
    public bool AreEnemiesJammed => enemiesJammed;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        ResolveAmmoRefundReceiver();
        UpdateSlotUI();
    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.digit3Key.wasPressedThisFrame)
            UseStoredStreak();
    }

    public bool TryAwardRandomStreak()
    {
        if (storedStreak != null)
            return false;

        if (streakPool == null || streakPool.Length == 0)
        {
            Debug.LogWarning(
                "KillstreakManager: no scorestreaks are assigned to the pool.",
                this
            );

            return false;
        }

        List<KillstreakBase> candidates = new List<KillstreakBase>();

        for (int i = 0; i < streakPool.Length; i++)
        {
            KillstreakBase candidate = streakPool[i];

            if (candidate == null)
                continue;

            if (activeStreaks.Contains(candidate))
                continue;

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return false;

        storedStreak =
            candidates[Random.Range(0, candidates.Count)];

        UpdateSlotUI();

        return true;
    }

    public void tryRoll()
    {
        TryAwardRandomStreak();
    }

    public bool UseStoredStreak()
    {
        if (storedStreak == null)
            return false;

        if (!allowStacking && HasActiveStreak())
            return false;

        KillstreakBase streak = storedStreak;
        storedStreak = null;

        UpdateSlotUI();

        activeStreaks.Add(streak);

        // ScoreManager owns the GDD threshold increase.
        if (ScoreManager.instance != null)
            ScoreManager.instance.NotifyStreakActivated();

        streak.Activate();

        // If enough score is already banked for the new requirement,
        // award the next streak after this one is consumed.
        if (ScoreManager.instance != null)
            ScoreManager.instance.TryAwardPendingStreak();

        return true;
    }

    // Kept so any existing code that calls UseSlot(0) still works.
    public bool UseSlot(int slotIndex)
    {
        if (slotIndex != 0)
            return false;

        return UseStoredStreak();
    }

    public void streakEnded(KillstreakBase streak)
    {
        if (streak == null)
            return;

        activeStreaks.Remove(streak);
    }

    public void cancelActiveStreak()
    {
        if (activeStreaks.Count > 0)
        {
            KillstreakBase[] snapshot =
                new KillstreakBase[activeStreaks.Count];

            activeStreaks.CopyTo(snapshot);

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] != null && snapshot[i].isActive)
                    snapshot[i].Deactivate();
            }

            activeStreaks.Clear();
        }

        ResetRuntimeEffects();
    }

    public void ResetForNewRun()
    {
        cancelActiveStreak();

        storedStreak = null;

        ResetRuntimeEffects();
        UpdateSlotUI();
    }

    public bool HasOpenSlot()
    {
        return storedStreak == null;
    }

    public bool HasActiveStreak()
    {
        foreach (KillstreakBase streak in activeStreaks)
        {
            if (streak != null && streak.isActive)
                return true;
        }

        return false;
    }

    public string GetSlotName(int slotIndex)
    {
        if (slotIndex != 0 || storedStreak == null)
            return string.Empty;

        return storedStreak.GetKillstreakName();
    }

    public void SetRootAccess(bool active)
    {
        rootAccessActive = active;
    }

    public void SetChainReaction(
        bool active,
        float radius,
        float damagePercent)
    {
        chainReactionActive = active;
        chainReactionRadius = Mathf.Max(0.1f, radius);
        chainReactionDamagePercent = Mathf.Clamp01(damagePercent);
    }

    public void SetInvulnerable(bool active)
    {
        invulnerableActive = active;
    }

    public void SetGhostProtocol(
        bool active,
        float aimErrorDegrees)
    {
        ghostProtocolActive = active;
        ghostAimErrorDegrees = Mathf.Max(0f, aimErrorDegrees);
    }

    public void SetPacketLeech(
        bool active,
        int ammoPerKill)
    {
        packetLeechActive = active;
        ammoRefundPerKill = Mathf.Max(1, ammoPerKill);

        if (active && ammoRefundReceiver == null)
            ResolveAmmoRefundReceiver();
    }

    public void SetEnemiesJammed(bool active)
    {
        enemiesJammed = active;
    }

    public void NotifyPlayerKill()
    {
        if (!packetLeechActive)
            return;

        if (ammoRefundReceiver == null)
            ResolveAmmoRefundReceiver();

        ammoRefundReceiver?.RefundAmmo(ammoRefundPerKill);
    }

    public Vector3 ApplyGhostAimError(Vector3 normalizedDirection)
    {
        if (!ghostProtocolActive || ghostAimErrorDegrees <= 0f)
            return normalizedDirection;

        float yaw =
            Random.Range(
                -ghostAimErrorDegrees,
                ghostAimErrorDegrees
            );

        float pitch =
            Random.Range(
                -ghostAimErrorDegrees,
                ghostAimErrorDegrees
            );

        Quaternion errorRotation =
            Quaternion.Euler(pitch, yaw, 0f);

        return
            (errorRotation * normalizedDirection).normalized;
    }

    public void TriggerChainReaction(
        EnemyBase source,
        int originalDamage)
    {
        if (!chainReactionActive ||
            source == null ||
            originalDamage <= 0)
        {
            return;
        }

        int hitCount =
            Physics.OverlapSphereNonAlloc(
                source.transform.position,
                chainReactionRadius,
                chainReactionBuffer
            );

        if (hitCount <= 0)
            return;

        chainReactionVictims.Clear();

        int spreadDamage =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    originalDamage *
                    chainReactionDamagePercent
                )
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = chainReactionBuffer[i];

            if (hit == null)
                continue;

            EnemyBase enemy =
                hit.GetComponentInParent<EnemyBase>();

            if (enemy == null ||
                enemy == source ||
                enemy.IsDead)
            {
                continue;
            }

            if (!chainReactionVictims.Add(enemy))
                continue;

            enemy.TakeSecondaryDamage(spreadDamage);
        }

        chainReactionVictims.Clear();
    }

    private void ResolveAmmoRefundReceiver()
    {
        ammoRefundReceiver =
            ammoRefundReceiverSource as IAmmoRefundReceiver;

        if (ammoRefundReceiver != null)
            return;

        if (GameManager.instance == null ||
            GameManager.instance.player == null)
        {
            return;
        }

        MonoBehaviour[] behaviours =
            GameManager.instance.player
                .GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IAmmoRefundReceiver receiver)
            {
                ammoRefundReceiver = receiver;
                ammoRefundReceiverSource = behaviours[i];
                return;
            }
        }
    }

    private void ResetRuntimeEffects()
    {
        rootAccessActive = false;
        chainReactionActive = false;
        invulnerableActive = false;
        ghostProtocolActive = false;
        packetLeechActive = false;
        enemiesJammed = false;
    }

    private void UpdateSlotUI()
    {
        if (slot3Text == null)
            return;

        string name =
            storedStreak != null
                ? storedStreak.GetKillstreakName()
                : emptySlotText;

        slot3Text.text =
            "[3] " + name;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}