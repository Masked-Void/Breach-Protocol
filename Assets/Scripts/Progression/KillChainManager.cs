using TMPro;
using UnityEngine;
/*
 * Script: KillChainManager
 *
 * Description:
 * Quick-kill combat feedback only. Counts kills that land close together and
 * shows a chain count and an announcement. Does not award scorestreaks —
 * ScoreManager owns that.
 *
 * Interacts With:
 * - EnemyEvents (subscribes to Killed)
 * - GameManager (stops the timer while paused)
 *
 * Notes:
 * - Chain timing is real time on purpose. Slowing the world down should not
 *   make a chain easier to hold.
 */
public class KillChainManager : MonoBehaviour
{
    public static KillChainManager instance;

    [Header("Kill Chain")]
    [Tooltip("Real-world seconds allowed between kills before the chain resets.")]
    [SerializeField] private float chainTimeLimit = 3f;

    [Header("UI (Optional)")]
    [Tooltip("shows CHAIN x2, x3 and so on, hidden when the chain is empty")]
    [SerializeField] private TMP_Text killChainCountUI;

    [Tooltip("shows DOUBLE KILL, TRIPLE KILL and so on")]
    [SerializeField] private TMP_Text chainAnnouncementUI;

    [Header("Runtime")]
    [Tooltip("current chain length, set at runtime")]
    [SerializeField] private int killChainCount;

    [Tooltip("seconds since the last kill, resets the chain at the limit")]
    [SerializeField] private float killChainTimer;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        UpdateUI();
    }

    private void Update()
    {
        if (killChainCount <= 0)
            return;

        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;

        // Kill-chain timing is real time; slow motion does not extend it.
        killChainTimer += Time.unscaledDeltaTime;

        if (killChainTimer >= chainTimeLimit)
            ResetChain();
    }

    private void OnEnable() => EnemyEvents.Killed += handleKill;
    private void OnDisable() => EnemyEvents.Killed -= handleKill;

    private void handleKill(EnemyBase enemy) => RegisterKill();

    public void RegisterKill()
    {
        killChainCount++;
        killChainTimer = 0f;

        UpdateUI();
        UpdateAnnouncement();
    }

    public void ResetChain()
    {
        killChainCount = 0;
        killChainTimer = 0f;

        UpdateUI();

        if (chainAnnouncementUI != null)
            chainAnnouncementUI.text = string.Empty;
    }

    public int KillChainCount => killChainCount;

    private void UpdateUI()
    {
        if (killChainCountUI != null)
        {
            killChainCountUI.text =
                killChainCount > 0
                    ? "CHAIN x" + killChainCount
                    : string.Empty;
        }
    }

    private void UpdateAnnouncement()
    {
        if (chainAnnouncementUI == null)
            return;

        if (killChainCount == 2)
            chainAnnouncementUI.text = "DOUBLE KILL";
        else if (killChainCount == 3)
            chainAnnouncementUI.text = "TRIPLE KILL";
        else if (killChainCount == 4)
            chainAnnouncementUI.text = "QUAD KILL";
        else if (killChainCount >= 5)
            chainAnnouncementUI.text = "KILLING FRENZY";
        else
            chainAnnouncementUI.text = string.Empty;
    }

    private void OnValidate()
    {
        chainTimeLimit = Mathf.Max(0.1f, chainTimeLimit);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

