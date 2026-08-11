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
    [SerializeField] GameObject shopUI;
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

    [SerializeField] TMP_Text gameGoalCountText;
    int gameGoalCount;
    int currentKill = 0;
    int enemiesAlive;
    int previousEnemiesAlive;
    int previousWave;


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
        if (FindAnyObjectByType<playerInteraction>().shopOpen)
        { 
            menuActive = shopUI;
            return;
        }
        
        if (!FindAnyObjectByType<playerInteraction>().shopOpen && Input.GetButtonDown("Cancel"))
        {
            audioManager.instance.playButtonClick();
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
        timeManager.pauseTime();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        pauseKills.text = currentKill.ToString("f0");
        if (audioManager.instance != null) audioManager.instance.pauseMusic();
    }

    // Unpause the game
    public void stateUnpause()
    {
        isPaused = false;
        timeManager.unpauseTime();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menuActive.SetActive(false);
        menuActive = null;
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

    // Update the heart rate in UI only, moving it to just heartBeatManager
    public void updateHeartRate(int bpm)
    {
        // Update the heart rate in the UI (not implemented here)
    }

    // Handle the lose state
    public void stateLose()
    {
        statePause();
        menuActive = menuLose;
        menuActive.SetActive(true);
        scoreText.text = currentKill.ToString("f0");
    }

    public void updateGameGoal(int amount)
    {
        gameGoalCount += amount;
        //gameGoalCountText.text = gameGoalCount.ToString("f0");

        if (gameGoalCount <= 0)
        {
        }
    }

    void updateUI()
    {
        int currentWave = waveManager.instance.getCurrentWave();
        enemiesAlive = waveManager.instance.getEnemiesAlive();

        if (currentWave != previousWave)
        {
            previousWave = currentWave;
            previousEnemiesAlive = enemiesAlive;
        }
        else if (enemiesAlive < previousEnemiesAlive)
        {
            currentKill += previousEnemiesAlive - enemiesAlive;
        }

        previousEnemiesAlive = enemiesAlive;

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
