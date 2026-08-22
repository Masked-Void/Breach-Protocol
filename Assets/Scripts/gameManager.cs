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
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuSound;

    [Header("Kills UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] TextMeshProUGUI pauseKills;
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
    int previousWave = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();
    }

    void OnDestroy() {
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
        if (FindAnyObjectByType<playerInteraction>().shopOpen)
        {
            menuActive = shopUI;
            return;
        }

        if (!FindAnyObjectByType<playerInteraction>().shopOpen && Input.GetButtonDown("Cancel"))
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
            else if (menuActive == menuSound)
            {
                openPauseMenu();
            }

        }


        updateUI();

        if (weaponManager.instance != null && weaponManager.instance.activeWeapon != null)
        {
            magAmmoUI.text = weaponManager.instance.getCurrentAmmo().ToString();
        }
    }

    // Pause the game
    public void statePause()
    {
        isPaused = true;
        timeManager.instance.pauseTime();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseKills.text = currentKill.ToString("f0");
        if (audioManager.instance != null) audioManager.instance.pauseMusic();
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
        if (audioManager.instance != null) audioManager.instance.resumeMusic();
    }

    public void openSoundMenu()
    {
        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }
        menuActive = menuSound;
        menuActive.SetActive(true);
    }

    public void openPauseMenu()
    {
        if (menuActive != null)
        {
            menuActive.SetActive(false);
        }
        menuActive = menuPause;
        menuActive.SetActive(true);
    }

    // Handle the lose state
    public void stateLose()
    {
        endRun(menuLose);
    }

    //Handes the win state aka when the boss dies
    public void stateWin() {
        endRun(menuWin);
    }

    // Simple method so simplify states
    void endRun(GameObject endMenu) {
        statePause();

        if (endMenu != null) {
            menuActive = endMenu;
            endMenu.SetActive(false);
        }

        if (scoreText != null) {
            scoreText.text = currentKill.ToString("f0");
        }

        if (upgradeManager.instance != null) {
            upgradeManager.instance.files += totalFiles;
            upgradeManager.instance.SaveUpgrades();
        }
    }
    public void addKill()
    {
        currentKill++;
    }

    void updateUI()
    {
        if (waveManager.instance == null) return;

        if (waveCounter != null) {
            waveCounter.text = waveManager.instance.getCurrentWave().ToString("f0");
            StartCoroutine(AnimateWaveText());

            killCounter.text = "Kills: " + currentKill;
        }

        if (waveManager.instance.isWaitingForNextWave()) {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();

            if (waveCountdownText != null) {
                waveCountdownText.gameObject.SetActive(true);
                waveCountdown.text = "" + secondsLeft;
            }
        } else {
            if (waveCountdown != null) {
                waveCountdownText.gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator WarningText()
    {
        if (shopMessage != null)
            shopMessage.SetActive(true);

        yield return new WaitForSecondsRealtime(5);

        if (shopMessage != null)
            shopMessage.SetActive(false);
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

            float t = timer / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            rect.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);

            yield return null;
        }

        rect.localScale = originalScale;
    }
}
