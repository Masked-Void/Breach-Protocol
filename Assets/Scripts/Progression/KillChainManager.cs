using TMPro;
using UnityEngine;

/// <summary>
/// Quick-kill combat feedback only.
/// This no longer awards scorestreaks; ScoreManager owns scorestreak progression.
/// </summary>
public class KillChainManager : MonoBehaviour
{
    public static KillChainManager instance;

    [Header("Kill Chain")]
    [Tooltip("Real-world seconds allowed between kills before the chain resets.")]
    [SerializeField] private float chainTimeLimit = 3f;

    [Header("UI (Optional)")]
    [SerializeField] private TMP_Text killChainCountUI;
    [SerializeField] private TMP_Text chainAnnouncementUI;

    [Header("Runtime")]
    [SerializeField] private int killChainCount;
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

    public int GetKillChainCount()
    {
        return killChainCount;
    }

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

