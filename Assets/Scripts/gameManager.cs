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
    [SerializeField] timeManager timeManager;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] TextMeshProUGUI pauseKills;
    [SerializeField] TextMeshProUGUI killCounter;
    [SerializeField] TextMeshProUGUI waveCounter;
    [SerializeField] TextMeshProUGUI waveCountdownText;
    [SerializeField] public GameObject interactionUI;
    [SerializeField] public Image playerStaminaBar;
    [SerializeField] public GameObject checkpointPopup;
    public GameObject playerSpawnPos;

    int currentKill = 0;


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

    void updateUI()
    {
        if (waveManager.instance == null) return;
        int currentWave = waveManager.instance.getCurrentWave();

        waveCounter.text = currentWave.ToString("f0");
        killCounter.text = "Kills: " + currentKill;

        if (waveManager.instance.isWaitingForNextWave())
        {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();
            waveCountdownText.text = "Next Wave starts in " + secondsLeft;
            waveCountdownText.gameObject.SetActive(true);
        }
        else
        {
            waveCountdownText.gameObject.SetActive(false);
        }
    }
}
