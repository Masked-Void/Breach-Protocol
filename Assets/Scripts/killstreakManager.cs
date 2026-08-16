using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Scorestreak inventory and runtime effect state.
/// The player can store exactly three streaks and activate them with 3, 4 and 5.
/// </summary>
public class killstreakManager : MonoBehaviour
{
    public static killstreakManager instance;

    [Header("Scorestreak Pool")]
    [Tooltip("Attach the scorestreak components to this manager object and drag them here.")]
    [SerializeField] private killstreakBase[] streakPool;

    [Tooltip("Off by default. When false, only one timed/manual streak can be active at once.")]
    [SerializeField] private bool allowStacking = false;

    [Header("3 / 4 / 5 Slot UI")]
    [SerializeField] private TMP_Text slot3Text;
    [SerializeField] private TMP_Text slot4Text;
    [SerializeField] private TMP_Text slot5Text;
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

    private readonly killstreakBase[] slots = new killstreakBase[3];
    private readonly HashSet<killstreakBase> activeStreaks = new HashSet<killstreakBase>();
    private readonly HashSet<EnemyBase> chainReactionVictims = new HashSet<EnemyBase>();

    private readonly Collider[] chainReactionBuffer = new Collider[64];

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
        UpdateAllSlotUI();
    }

    private void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.digit3Key.wasPressedThisFrame)
            UseSlot(0);

        if (keyboard.digit4Key.wasPressedThisFrame)
            UseSlot(1);

        if (keyboard.digit5Key.wasPressedThisFrame)
            UseSlot(2);
    }

    public bool TryAwardRandomStreak()
    {
        int emptySlot = FindFirstEmptySlot();

        if (emptySlot < 0)
            return false;

        if (streakPool == null || streakPool.Length == 0)
        {
            Debug.LogWarning("killstreakManager: no scorestreaks are assigned to the pool.", this);
            return false;
        }

        // Prefer a program the player is not already holding or running.
        List<killstreakBase> candidates = new List<killstreakBase>();

        for (int i = 0; i < streakPool.Length; i++)
        {
            killstreakBase candidate = streakPool[i];

            if (candidate == null)
                continue;

            if (IsStored(candidate) || activeStreaks.Contains(candidate))
                continue;

            candidates.Add(candidate);
        }

        // If every available type is already represented, allow any non-active type.
        if (candidates.Count == 0)
        {
            for (int i = 0; i < streakPool.Length; i++)
            {
                killstreakBase candidate = streakPool[i];

                if (candidate != null && !activeStreaks.Contains(candidate))
                    candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
            return false;

        killstreakBase awarded = candidates[Random.Range(0, candidates.Count)];

        slots[emptySlot] = awarded;
        UpdateSlotUI(emptySlot);

        return true;
    }

    // Backwards-compatible name so any leftover old call still compiles.
    // New code should award streaks through scoreManager thresholds instead.
    public void tryRoll()
    {
        TryAwardRandomStreak();
    }

    public bool UseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return false;

        killstreakBase streak = slots[slotIndex];

        if (streak == null)
            return false;

        if (!allowStacking && HasActiveStreak())
            return false;

        slots[slotIndex] = null;
        UpdateSlotUI(slotIndex);

        activeStreaks.Add(streak);

        if (scoreManager.instance != null)
            scoreManager.instance.NotifyStreakActivated();

        streak.Activate();

        // If the streak was instant, it has already called streakEnded().
        // If it is timed/manual, it remains in activeStreaks.
        if (scoreManager.instance != null)
            scoreManager.instance.TryAwardPendingStreak();

        return true;
    }

    public void streakEnded(killstreakBase streak)
    {
        if (streak == null)
            return;

        activeStreaks.Remove(streak);
    }

    public void cancelActiveStreak()
    {
        if (activeStreaks.Count == 0)
            return;

        killstreakBase[] snapshot = new killstreakBase[activeStreaks.Count];
        activeStreaks.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null && snapshot[i].isActive)
            {
                snapshot[i].Deactivate();
            }
        }

        activeStreaks.Clear();
        ResetRuntimeEffects();
    }

    public void ResetForNewRun()
    {
        cancelActiveStreak();

        for (int i = 0; i < slots.Length; i++)
            slots[i] = null;

        ResetRuntimeEffects();
        UpdateAllSlotUI();
    }

    public bool HasOpenSlot()
    {
        return FindFirstEmptySlot() >= 0;
    }

    public bool HasActiveStreak()
    {
        foreach (killstreakBase streak in activeStreaks)
        {
            if (streak != null && streak.isActive)
                return true;
        }

        return false;
    }

    public string GetSlotName(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null)
            return string.Empty;

        return slots[slotIndex].GetKillstreakName();
    }

    // =========================================================
    // EFFECT STATE API
    // =========================================================

    public void SetRootAccess(bool active)
    {
        rootAccessActive = active;
    }

    public void SetChainReaction(bool active, float radius, float damagePercent)
    {
        chainReactionActive = active;
        chainReactionRadius = Mathf.Max(0.1f, radius);
        chainReactionDamagePercent = Mathf.Clamp01(damagePercent);
    }

    public void SetInvulnerable(bool active)
    {
        invulnerableActive = active;
    }

    public void SetGhostProtocol(bool active, float aimErrorDegrees)
    {
        ghostProtocolActive = active;
        ghostAimErrorDegrees = Mathf.Max(0f, aimErrorDegrees);
    }

    public void SetPacketLeech(bool active, int ammoPerKill)
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

        float yaw = Random.Range(-ghostAimErrorDegrees, ghostAimErrorDegrees);
        float pitch = Random.Range(-ghostAimErrorDegrees, ghostAimErrorDegrees);

        Quaternion errorRotation = Quaternion.Euler(pitch, yaw, 0f);
        return (errorRotation * normalizedDirection).normalized;
    }

    public void TriggerChainReaction(EnemyBase source, int originalDamage)
    {
        if (!chainReactionActive || source == null || originalDamage <= 0)
            return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            source.transform.position,
            chainReactionRadius,
            chainReactionBuffer
        );

        if (hitCount <= 0)
            return;

        chainReactionVictims.Clear();

        int spreadDamage = Mathf.Max(
            1,
            Mathf.CeilToInt(originalDamage * chainReactionDamagePercent)
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = chainReactionBuffer[i];

            if (hit == null)
                continue;

            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();

            if (enemy == null || enemy == source || enemy.IsDead)
                continue;

            if (!chainReactionVictims.Add(enemy))
                continue;

            enemy.TakeSecondaryDamage(spreadDamage);
        }

        chainReactionVictims.Clear();
    }

    private void ResolveAmmoRefundReceiver()
    {
        ammoRefundReceiver = ammoRefundReceiverSource as IAmmoRefundReceiver;

        if (ammoRefundReceiver != null)
            return;

        if (gameManager.instance == null || gameManager.instance.player == null)
            return;

        MonoBehaviour[] behaviours =
            gameManager.instance.player.GetComponentsInChildren<MonoBehaviour>(true);

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

    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }

        return -1;
    }

    private bool IsStored(killstreakBase streak)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == streak)
                return true;
        }

        return false;
    }

    private void UpdateAllSlotUI()
    {
        for (int i = 0; i < slots.Length; i++)
            UpdateSlotUI(i);
    }

    private void UpdateSlotUI(int index)
    {
        TMP_Text text = null;

        if (index == 0)
            text = slot3Text;
        else if (index == 1)
            text = slot4Text;
        else if (index == 2)
            text = slot5Text;

        if (text == null)
            return;

        string key = (index + 3).ToString();
        string name = slots[index] != null
            ? slots[index].GetKillstreakName()
            : emptySlotText;

        text.text = "[" + key + "] " + name;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

