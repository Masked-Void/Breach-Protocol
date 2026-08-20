using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class gameManager : MonoBehaviour
{

    public static gameManager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuSound;
    public GameObject shopUI;
    [SerializeField] timeManager timeManager;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] TextMeshProUGUI pauseKills;
    [SerializeField] TextMeshProUGUI killCounter;
    [SerializeField] TextMeshProUGUI waveCounter;
    [SerializeField] TextMeshProUGUI waveCountdownText;
    public GameObject interactionUI;
    public TMP_Text interactionText;
    public TMP_Text interactionKey;
    public Image playerStaminaBar;
    public GameObject checkpointPopup;
    [SerializeField]  TextMeshProUGUI bytesText;
    public GameObject shopMessage;
    
    public GameObject playerSpawnPos;

    int currentKill = 0;
    int previousWave = -1;


    [Header("Screen Flash")]
    public GameObject damageFlashUI;

    public bool isPaused;
    public GameObject player;
    public playerController playerScript;

    [Header("Currency")]
    [SerializeField] public int totalBytes = 0;
    [SerializeField] public int totalFiles = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();
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
        if (timeManager != null) timeManager.unpauseTime();
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
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
        scoreText.text = currentKill.ToString("f0");
        upgradeManager.instance.files += totalFiles;
        upgradeManager.instance.SaveUpgrades();
    }

    public void addKill()
    {
        currentKill++;
    }

    void updateUI()
    {
        if (waveManager.instance == null) return;

        int currentWave = waveManager.instance.getCurrentWave();
        

        if (currentWave != previousWave)
        {
            previousWave = currentWave;

            waveCounter.text = currentWave.ToString("f0");

            StartCoroutine(AnimateWaveText());
        }

        killCounter.text = currentKill.ToString("f0");

        if (waveManager.instance.isWaitingForNextWave())
        {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();

            waveCountdownText.text = "Next Wave Starts In: " + secondsLeft;
            waveCountdownText.gameObject.SetActive(true);
        }
        else
        {
            waveCountdownText.gameObject.SetActive(false);
        }
    }

    public IEnumerator WarningText()
    {
        if (gameManager.instance.shopMessage != null)
            gameManager.instance.shopMessage.SetActive(true);

        yield return new WaitForSecondsRealtime(5);

        if (gameManager.instance.shopMessage != null)
            gameManager.instance.shopMessage.SetActive(false);
    }

    public void showShopWarning()
    {
        StopCoroutine(nameof(WarningText));
        StartCoroutine(WarningText());
    }

    IEnumerator AnimateWaveText()
    {
        RectTransform rect = waveCounter.rectTransform;

        Vector3 originalScale = Vector3.one;

        float duration = 0.25f;
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
