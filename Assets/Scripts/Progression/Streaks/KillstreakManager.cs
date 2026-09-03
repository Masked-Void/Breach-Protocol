using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Script: KillstreakManager
 *
 * Description:
 * Rolls and runs the ten scorestreaks. ScoreManager decides when a roll is
 * earned; this picks one, activates it, and tracks it until it ends.
 *
 * Responsibilities:
 * - Roll a random streak when one is earned
 * - Activate it and hold a reference until it reports back
 * - Expose active-state flags other systems read
 * - Handle Fork Bomb secondary damage spreading between enemies
 *
 * Interacts With:
 * - ScoreManager (triggers rolls)
 * - KillstreakBase and its ten subclasses
 * - EnemyBase (several streaks mutate or kill enemies)
 * - HeartbeatManager, TimeManager, WeaponManager (streak effects)
 *
 * Notes:
 * - Audit finding: six of the ten streaks set flags nothing consumes. Setting
 *   a flag is not the same as having an effect.
 */

public class KillstreakManager : MonoBehaviour
{
    public static KillstreakManager instance;

    [Header("Scorestreak Pool")]
    [Tooltip("Attach the scorestreak components to this manager object and drag them here.")]
    [SerializeField] private KillstreakBase[] streakPool;

    [Tooltip("Off by default. When false, only one timed/manual streak can be active at once.")]
    [SerializeField] private bool allowStacking = false;

    [Header("Stored Scorestreak UI")]
    [Tooltip("label showing the held streak's name, or emptySlotText when nothing is stored")]
    [SerializeField] private TMP_Text slot3Text;

    [Tooltip("shown when no streak is stored")]
    [SerializeField] private string emptySlotText = "EMPTY";

    [Header("Packet Leech")]
    [Tooltip("Optional. Assign a component implementing IAmmoRefundReceiver. If empty, the player hierarchy is searched automatically.")]
    [SerializeField] private MonoBehaviour ammoRefundReceiverSource;
    // Flags set by the streaks and read by whatever the effect applies to.
    // Several of these are set and never read — the streak activates and the
    // effect never happens. See the audit before assuming one works.
    [Header("Runtime Effect State")]
    [Tooltip("root access is active, set at runtime")]
    [SerializeField] private bool rootAccessActive;

    [Tooltip("fork bomb is active, kills spread damage to nearby enemies")]
    [SerializeField] private bool chainReactionActive;

    [Tooltip("god mode is active, incoming stress is ignored")]
    [SerializeField] private bool invulnerableActive;

    [Tooltip("ghost protocol is active, ranged enemy aim is skewed")]
    [SerializeField] private bool ghostProtocolActive;

    [Tooltip("packet leech is active, kills refund ammo")]
    [SerializeField] private bool packetLeechActive;

    [Tooltip("ddos is active, enemy ai is frozen")]
    [SerializeField] private bool enemiesJammed;

    [Header("Effect Tuning")]
    [Tooltip("how far fork bomb damage spreads from the enemy that died, in metres")]
    [SerializeField] private float chainReactionRadius = 4f;

    [Tooltip("fraction of the original damage each nearby enemy takes")]
    [SerializeField, Range(0f, 1f)] private float chainReactionDamagePercent = 0.5f;

    [Tooltip("how far ghost protocol pushes enemy aim off target, in degrees")]
    [SerializeField] private float ghostAimErrorDegrees = 12f;

    [Tooltip("rounds packet leech refunds per kill")]
    [SerializeField] private int ammoRefundPerKill = 1;

    // the streak being held in the slot, spent when the player uses it
    private KillstreakBase storedStreak;

    private readonly HashSet<KillstreakBase> activeStreaks =
        new HashSet<KillstreakBase>();

    // reused so the overlap check doesn't allocate on every fork bomb kill
    private readonly Collider[] chainReactionBuffer = new Collider[64];

    // enemies already hit by the current fork, so secondary damage never re-forks
    private readonly HashSet<EnemyBase> chainReactionVictims = new HashSet<EnemyBase>();

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

    // picks a random streak from the pool and activates it, respecting allowStacking
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

    public void TryRoll()
    {
        TryAwardRandomStreak();
    }

    // spends the held streak
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

    // a streak reports in here when it finishes, so the manager can free the slot
    public void StreakEnded(KillstreakBase streak)
    {
        if (streak == null)
            return;

        activeStreaks.Remove(streak);
    }

    public void CancelActiveStreak()
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
        CancelActiveStreak();

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

        return storedStreak.KillstreakName();
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

    // skews an enemy's aim while ghost protocol is up. enemies call this on
    // their firing direction rather than checking the flag themselves.
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

    // fork bomb. spreads a fraction of the damage to everything in radius,
    // tracking victims so a chain can't loop back on itself.
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

    // finds whatever can receive refunded ammo, searching the player if the
    // inspector reference is empty
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

    // clears every effect flag, used between runs so nothing leaks
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
                ? storedStreak.KillstreakName()
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