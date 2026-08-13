using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class gameManager : MonoBehaviour
{

    public static gameManager instance;

    [SerializeField] GameObject menuActive;
    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuLose;
    [SerializeField] GameObject menuSound;
    [SerializeField] timeManager timeManager;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] TextMeshProUGUI pauseKills;
    [SerializeField] TextMeshProUGUI killCounter;
    [SerializeField] TextMeshProUGUI waveCounter;
    [SerializeField] TextMeshProUGUI waveCountdownText;
    [SerializeField] public GameObject pickUpUI;
    [SerializeField] public Image playerStaminaBar;
    [SerializeField] public GameObject checkpointPopup;
    public GameObject playerSpawnPos;

    int currentKill = 0;
    int previousWave = -1;


    [Header("Screen Flash")]
    public GameObject damageFlashUI;

    public bool isPaused;
    public GameObject player;
    public playerController playerScript;

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

            rect.localScale = Vector3.Lerp(originalScale * 1.3f,originalScale,t);

            yield return null;
        }

        rect.localScale = originalScale;
    }
}
