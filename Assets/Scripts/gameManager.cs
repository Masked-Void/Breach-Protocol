using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{

    public static gameManager instance;

    [Header("Menu")]
    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuLose;

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
    public Image playerStaminaBar;
    public GameObject playerSpawnPos;
    public GameObject checkpointPopup;

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
        instance = this;
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();
    }

    // Update is called once per frame
    void Update()
    {
        bytesText.text = "Bytes: " + totalBytes.ToString();
        // if (FindAnyObjectByType<playerInteraction>().shopOpen)
        // {
        //     menuActive = shopUI;
        //     return;
        // }

        if (Input.GetButtonDown("Cancel"))
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
        if (audioManager.instance != null) audioManager.instance.pauseMusic();
        StartCoroutine(playAudioDelay());
    }

    IEnumerator playAudioDelay()
    {
        yield return new WaitForSeconds(5);
        if (audioManager.instance != null) audioManager.instance.playPauseMenuMusic();
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
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
        loseSoreText.text = currentKill.ToString("f0");
        if (upgradeManager.instance != null)
        {
            upgradeManager.instance.files += totalFiles;
            upgradeManager.instance.SaveUpgrades();
        }
        if (audioManager.instance != null) audioManager.instance.playLoseMenuMusic();
    }

    public void addKill() => currentKill++;

    void updateUI()
    {
        if (waveManager.instance == null) return;

        waveCounter.text = waveManager.instance.getCurrentWave().ToString("f0");
        StartCoroutine(AnimateWaveText());

        killCounter.text = "Kills: " + currentKill;

        if (waveManager.instance.isWaitingForNextWave())
        {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();

            waveCountdownText.gameObject.SetActive(true);
            waveCountdown.text = "" + secondsLeft;
        }
        else
        {
            waveCountdownText.gameObject.SetActive(false);
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
     
    // Currency Stuff
    public void AddBytes(int amount) { totalBytes += amount; }
    public void AddFiles(int amount) { totalFiles += amount; }
    public void SubtractBytes(int amount) { totalBytes -= amount; }
    public void SubtractFiles(int amount) { totalFiles -= amount; }
}
