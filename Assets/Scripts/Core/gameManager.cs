using JetBrains.Annotations;
using System.Collections;
using TMPro;
//using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{

    public static gameManager instance;

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
    public playerController playerScript;


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
            playerScript = tagged.GetComponentInParent<playerController>();
            player = playerScript != null ? playerScript.gameObject : tagged;
        }
        else
        {
            Debug.LogWarning("gameManager: nothing tagged Player in the scene", this);
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
            if (audioManager.instance != null) audioManager.instance.playButtonClick();
            if (menuActive == null)
            {
                statePause();
                menuActive = menuPause;
                menuActive.SetActive(true);
            }
            else if (menuActive == menuPause)
            {
                stateUnpause();
            }
        }


        updateUI();

        if (weaponManager.instance != null && weaponManager.instance.activeWeapon != null)
            magAmmoUI.text = weaponManager.instance.getCurrentAmmo().ToString();
    }

    // Pause the game
    public void statePause()
    {
        isPaused = true;
        timeManager.instance.pauseTime();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseScoreText.text = currentKill.ToString("f0");
        resetPauseUI();
        if (audioManager.instance != null)
        {
            audioManager.instance.pauseMusic();
            audioManager.instance.playPauseMenuMusicWithDelay(4.0f);
        }
    }

    public void resetPauseUI()
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
    public void stateUnpause()
    {
        isPaused = false;
        if (timeManager.instance != null) timeManager.instance.unpauseTime();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (menuActive != null)
        {
            menuActive.SetActive(false);
            menuActive = null;
        }
        if (audioManager.instance != null) audioManager.instance.restoreGameplayMusic();
    }

    // Handle the lose state
    public void stateLose()
    {

        endRun(menuLose);
    }

    //Handes the win state aka when the boss dies
    public void stateWin()
    {
        endRun(menuWin);
    }

    // Simple method so simplify states
    void endRun(GameObject endMenu)
    {
        statePause();

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
        if (upgradeManager.instance != null)
        {
            upgradeManager.instance.files += totalFiles;
            upgradeManager.instance.SaveUpgrades();
        }

        // lose screen gets its own music cue
        if (endMenu == menuLose && audioManager.instance != null)
        {
            audioManager.instance.playLoseMenuMusic();
        }
    }
    public void addKill()
    {
        currentKill++;
    }

    void updateUI()
    {
        if (waveManager.instance == null) return;

        if (waveCounter != null)waveCounter.text = waveManager.instance.getCurrentWave().ToString("f0");
        if(killCounter != null) killCounter.text = "Kills: " + waveManager.instance.getEnemiesKilled();

        if (waveManager.instance.isWaitingForNextWave())
        {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();

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

    public void showShopWarning()
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
