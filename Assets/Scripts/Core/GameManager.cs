using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    [Header("Menu")]
    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuWin;

    [Header("UI Pages")]
    public GameObject challengesCanvas;
    public GameObject settingsCanvas;
    public GameObject upgradesCanvas;

    [Header("Top Navigation Buttons")]
    public GameObject navTab;
    public Button navChallengesButton;
    public Button navSettingsButton;
    public Button navUpgradesButton;
    public GameObject buttons;
    public GameObject backButton;

    [Header("Settings Menu")]
    [FormerlySerializedAs("soundMenu")]
    [SerializeField] public GameObject soundMenu;
    [SerializeField] public GameObject controlsMenu;

    [Header("Kills UI")]
    [SerializeField] public GameObject pauseScorePanel;
    [SerializeField] private TMP_Text pauseScoreText;
    [SerializeField] private TMP_Text loseSoreText;
    [SerializeField] TextMeshProUGUI killCounter;

    [Header("Wave UI")]
    [SerializeField] TextMeshProUGUI waveCounter;
    [SerializeField] TextMeshProUGUI waveCountdownText;
    [SerializeField] TextMeshProUGUI waveCountdown;

    [Header("Interaction UI")]
    public GameObject interactionUI;
    public TMP_Text interactionText;
    public TMP_Text interactionKey;

    [Header("Player")]
    public GameObject playerSpawnPos;
    [SerializeField] public Image playerStaminaBar;
    [SerializeField] public GameObject checkpointPopup;

    [Header("Currency")]
    [SerializeField] public int totalBytes = 0;
    [SerializeField] public int totalFiles = 0;
    [SerializeField] TextMeshProUGUI bytesText;

    [Header("Shop")]
    public GameObject shopMessage;
    public GameObject shopUI;

    [Header("Screen Flash")]
    public GameObject damageFlashUI;

    [Header("Weapon UI")]
    public GameObject ammoPanel;
    public TextMeshProUGUI magAmmoUI;
    public TextMeshProUGUI totalAmmoUI;
    public Image activeWeapon;

    [Header("Runtime: Do not Change")]
    public bool isPaused;
    public GameObject player;
    public PlayerController playerScript;


    int currentKill = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
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
        // player lives in bootstrap now and awake order between scenes is not guaranteed,
        // so find it here instead, start always runs after every awake
        GameObject tagged = GameObject.FindWithTag("Player");

        if (tagged != null)
        {
            playerScript = tagged.GetComponentInParent<PlayerController>();
            player = playerScript != null ? playerScript.gameObject : tagged;
        }
        else
        {
            Debug.LogWarning("GameManager: nothing tagged Player in the scene", this);
        }

        if (player!= null)
        {
            PlayerReady?.Invoke();
        }

    }

    public static event System.Action PlayerReady;

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    //Currency Stuff
    public void AddBytes(int amount)
    {
        totalBytes += amount;
        //Debug.Log("Current Bytes: " + totalBytes);
    }
    public void AddFiles(int amount)
    {
        totalFiles += amount;
        //Debug.Log("Current Files: " + totalFiles);
    }
    public void SubtractBytes(int amount)
    {
        totalBytes -= amount;
    }
    public void SubtractFiles(int amount)
    {
        totalFiles -= amount;
    }
    // Update is called once per frame
    void Update()
    {
        bytesText.text = "Bytes: " + totalBytes.ToString();

        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayButtonClick();
            if (menuActive == null)
            {
                StatePause();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if (menuActive == menuPause)
            {
                StateUnpause();
            }
        }


        UpdateUI();

        if (WeaponManager.instance != null && WeaponManager.instance.activeWeapon != null)
            magAmmoUI.text = WeaponManager.instance.CurrentAmmo.ToString();
    }

    // Pause the game
    public void StatePause()
    {
        isPaused = true;
        TimeManager.instance.PauseTime();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseScoreText.text = currentKill.ToString("f0");
        ResetPauseUI();
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PauseMusic();
            AudioManager.instance.PlayPauseMenuMusicWithDelay(4.0f);
        }
    }

    public void ResetPauseUI()
    {
        if (challengesCanvas != null) challengesCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
        if (upgradesCanvas != null) upgradesCanvas.SetActive(false);
        if (soundMenu != null) soundMenu.SetActive(false);
        if (controlsMenu != null) controlsMenu.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (navTab != null) navTab.SetActive(false);
        if (buttons != null) buttons.SetActive(true);
        if (pauseScorePanel != null) pauseScorePanel.SetActive(true);
    }

    // Unpause the game
    public void StateUnpause()
    {
        isPaused = false;
        if (TimeManager.instance != null) TimeManager.instance.UnpauseTime();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (menuActive != null)
        {
            menuActive.SetActive(false);
            menuActive = null;
        }
        if (AudioManager.instance != null) AudioManager.instance.RestoreGameplayMusic();
    }

    // Handle the lose state
    public void StateLose()
    {

        EndRun(menuLose);
    }

    //Handes the win state aka when the boss dies
    public void StateWin()
    {
        EndRun(menuWin);
    }

    // Simple method so simplify states
    void EndRun(GameObject endMenu)
    {
        StatePause();

        if (menuActive != null && menuActive != endMenu)
        {
            menuActive.SetActive(false);
        }

        if (menuPause != null && menuPause != endMenu)
        {
            menuPause.SetActive(false);
        }

        menuActive = endMenu;

        if (menuActive != null)
        {
            menuActive.SetActive(true);
        }

        if (loseSoreText != null)
        {
            loseSoreText.text = currentKill.ToString("f0");
        }
        if (UpgradeManager.instance != null)
        {
            UpgradeManager.instance.files += totalFiles;
            UpgradeManager.instance.SaveUpgrades();
        }

        // lose screen gets its own music cue
        if (endMenu == menuLose && AudioManager.instance != null)
        {
            AudioManager.instance.PlayLoseMenuMusic();
        }
    }
    public void AddKill()
    {
        currentKill++;
    }

    void UpdateUI()
    {
        if (WaveManager.instance == null) return;

        if (waveCounter != null)waveCounter.text = WaveManager.instance.CurrentWave.ToString("f0");
        if(killCounter != null) killCounter.text = "Kills: " + WaveManager.instance.EnemiesKilled;

        if (WaveManager.instance.IsWaitingForNextWave)
        {
            int secondsLeft = WaveManager.instance.SecondsUntilNextWave;

            if (waveCountdownText != null)
            {
                waveCountdownText.gameObject.SetActive(true);
                waveCountdown.text = "" + secondsLeft;
                StartCoroutine(AnimateWaveText());
            }
        }
        else
        {
            if (waveCountdown != null)
            {
                waveCountdownText.gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator WarningText()
    {
        if (shopMessage != null) shopMessage.SetActive(true);
        yield return new WaitForSecondsRealtime(5);
        if (shopMessage != null) shopMessage.SetActive(false);
    }

    public void ShowShopWarning()
    {
        StopCoroutine(nameof(WarningText));
        StartCoroutine(WarningText());
    }

    IEnumerator AnimateWaveText()
    {
        RectTransform rect = waveCountdown.rectTransform;
        Vector3 originalScale = Vector3.one;
        float duration = .1f;
        float timer = 0f;
        rect.localScale = originalScale * 1.3f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            rect.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
            yield return null;
        }

        rect.localScale = originalScale;
    }

}
