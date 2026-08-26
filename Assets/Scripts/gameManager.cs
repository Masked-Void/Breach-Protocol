using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class gameManager : MonoBehaviour {

    public static gameManager instance;

    [Header("Auto Assign Names")]
    [Tooltip("Object names autoAssign looks for. Blank means wire that one by hand.")]
    [SerializeField] markerNamesGroup markerNames = new markerNamesGroup();

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
        public GameObject soundMenu;
        public GameObject controlsMenu;


    [Header("Kills UI")]
        public GameObject pauseScorePanel;
        [SerializeField] TMP_Text pauseScoreText;
        [SerializeField] TMP_Text loseScoreText;
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
        public Image playerStaminaBar;


    [Header("Currency")]
        [Tooltip("Bytes held during this run.")]
        public int totalBytes = 0;
        [Tooltip("Files held during this run, added to the meta total on death.")]
        public int totalFiles = 0;
        [Tooltip("Bytes counter on the hud.")]
        [SerializeField] TextMeshProUGUI bytesText;


    [Header("Shop")]
        public GameObject shopMessage;
        public GameObject shopUI;


    [Header("Screen Flash")]
        [Tooltip("Red overlay flashed when the player takes damage.")]
        public GameObject damageFlashUI;

    [Header("Weapon UI")]
        [Tooltip("Ammo panel root.")]
        public GameObject ammoPanel;
        [Tooltip("Rounds in the current magazine.")]
        public TextMeshProUGUI magAmmoUI;
        [Tooltip("Rounds held in reserve.")]
        public TextMeshProUGUI totalAmmoUI;
        [Tooltip("Icon for the weapon currently held.")]
        public Image activeWeapon;


    [Header("Runtime: Do not Change")]
        public bool isPaused;
        public GameObject player;
        public playerController playerScript;

    int currentKill = 0;

    // last wave number the counter showed, so the pop only plays when it actually changes
    int lastWave = -1;

    // coroutine handles, held so a restart can stop the running one instead of stacking
    Coroutine warningRoutine;
    Coroutine waveTextRoutine;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake() {


        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;


        autoAssign();

        player = GameObject.FindWithTag("Player");
        playerScript = player.GetComponent<playerController>();
    }

    void OnDestroy() {
        if (instance == this)
            instance = null;
    }


    //Currency Stuff
    public void AddBytes(int amount) {
        totalBytes += amount;
    }

    public void AddFiles(int amount) {
        totalFiles += amount;
    }

    public void SubtractBytes(int amount) {
        totalBytes -= amount;
    }

    public void SubtractFiles(int amount) {
        totalFiles -= amount;
    }


    void Update() {

        if (bytesText != null) {
            bytesText.text = "Bytes: " + totalBytes.ToString();
        }

        if (FindAnyObjectByType<playerInteraction>().shopOpen) {
            menuActive = shopUI;
            return;
        }

        if (Input.GetButtonDown("Cancel")) {
            if (audioManager.instance != null)
                audioManager.instance.playButtonClick();
            if (menuActive == null) {
                statePause();
                menuActive = menuPause;

                if (menuActive != null)
                    menuActive.SetActive(true);
            } else if (menuActive == menuPause) {
                stateUnpause();
            }
        }


        updateUI();

        if (weaponManager.instance != null && weaponManager.instance.activeWeapon != null)
            magAmmoUI.text = weaponManager.instance.getCurrentAmmo().ToString();
    }

    // Pause the game
    public void statePause() {
        isPaused = true;
        timeManager.instance.pauseTime();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (pauseScoreText != null)
            pauseScoreText.text = currentKill.ToString("f0");
        resetPauseUI();

        if (audioManager.instance != null) {
            audioManager.instance.pauseMusic();
            audioManager.instance.playPauseMenuMusicWithDelay(4.0f);
        }
    }

    public void resetPauseUI() {

        if (challengesCanvas != null)
            challengesCanvas.SetActive(false);

        if (settingsCanvas != null)
            settingsCanvas.SetActive(false);

        if (upgradesCanvas != null)
            upgradesCanvas.SetActive(false);

        if (soundMenu != null)
            soundMenu.SetActive(false);

        if (controlsMenu != null)
            controlsMenu.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (navTab != null)
            navTab.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        if (pauseScorePanel != null)
            pauseScorePanel.SetActive(true);
    }

    // Unpause the game
    public void stateUnpause() {
        isPaused = false;

        if (timeManager.instance != null)
            timeManager.instance.unpauseTime();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (menuActive != null) {
            menuActive.SetActive(false);
            menuActive = null;
        }

        if (audioManager.instance != null)
            audioManager.instance.restoreGameplayMusic();
    }

    // Handle the lose state
    public void stateLose() {

        endRun(menuLose);
    }

    //Handes the win state aka when the boss dies
    public void stateWin() {
        endRun(menuWin);
    }

    // Simple method so simplify states
    void endRun(GameObject endMenu) {
        statePause();

        if (menuActive != null && menuActive != endMenu) {
            menuActive.SetActive(false);
        }

        if (menuPause != null && menuPause != endMenu) {
            menuPause.SetActive(false);
        }

        menuActive = endMenu;

        if (menuActive != null) {
            menuActive.SetActive(true);
        }

        if (loseScoreText != null) {
            loseScoreText.text = currentKill.ToString("f0");
        }
        if (upgradeManager.instance != null) {
            upgradeManager.instance.files += totalFiles;
            upgradeManager.instance.SaveUpgrades();
        }

        // lose screen gets its own music cue
        if (endMenu == menuLose && audioManager.instance != null) {
            audioManager.instance.playLoseMenuMusic();
        }
    }
    public void addKill() {
        currentKill++;
    }

    void updateUI() {
        if (waveManager.instance == null)
            return;

        if (waveCounter != null) {
            int wave = waveManager.instance.getCurrentWave();
            waveCounter.text = wave.ToString("f0");

            if (wave != lastWave) {
                lastWave = wave;

                if (waveTextRoutine != null) {
                    StopCoroutine(waveTextRoutine);
                }

                waveTextRoutine = StartCoroutine(AnimateWaveText());
            }
        }

        if (waveManager.instance.isWaitingForNextWave()) {
            int secondsLeft = waveManager.instance.getSecondsUntilNextWave();

            if (waveCountdownText != null) {
                waveCountdownText.gameObject.SetActive(true);
            }

            if (waveCountdown != null) {
                waveCountdown.text = "" + secondsLeft;
            }

        } else {
            if (waveCountdown != null) {
                waveCountdownText.gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator WarningText() {

        if (shopMessage != null)
            shopMessage.SetActive(true);

        yield return new WaitForSecondsRealtime(5);

        if (shopMessage != null)
            shopMessage.SetActive(false);

        warningRoutine = null;
    }

    public void showShopWarning() {

        if (warningRoutine != null) {
            StopCoroutine(nameof(WarningText));
        }

        warningRoutine = StartCoroutine(WarningText());
    }

    IEnumerator AnimateWaveText() {

        if (waveCounter == null)
            yield break;

        RectTransform rect = waveCountdown.rectTransform;
        Vector3 originalScale = Vector3.one;
        float duration = .1f;
        float timer = 0f;

        rect.localScale = originalScale * 1.3f;

        while (timer < duration) {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f , 1f , timer / duration);
            rect.localScale = Vector3.Lerp(originalScale * 1.3f , originalScale , t);
            yield return null;
        }

        rect.localScale = originalScale;
    }


    /// <summary>
    /// Fills any empty ui reference whose matching name in markerNames is filled in.
    /// Exact object names first, then a looser contains match for whatever is left.
    /// </summary>
    void autoAssign() {
        Dictionary<string , Transform> objectsByName = new Dictionary<string , Transform>(System.StringComparer.OrdinalIgnoreCase);
        List<Transform> allChildren = new List<Transform>();
        HashSet<Transform> alreadyUsed = new HashSet<Transform>();

        // every name we are actually looking for, used to keep the duplicate warnings quiet
        HashSet<string> wantedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        FieldInfo[] nameFields = typeof(markerNamesGroup).GetFields();

        foreach (FieldInfo nameField in nameFields) {
            if (nameField.FieldType != typeof(string))
                continue;

            // read off the group instance, not off this script
            string value = nameField.GetValue(markerNames) as string;

            if (!string.IsNullOrEmpty(value))
                wantedNames.Add(value);
        }

        collectChildren(transform , objectsByName , allChildren , wantedNames);

        // NonPublic is needed, some fields are SerializeField private
        FieldInfo[] allFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        List<FieldInfo> unfilledFields = new List<FieldInfo>();
        List<string> unfilledNames = new List<string>();

        // first pass, exact name matches only
        foreach (FieldInfo field in allFields) {
            // skips ints, bools and the markerNames group itself
            if (!typeof(Object).IsAssignableFrom(field.FieldType))
                continue;

            // already dragged in by hand, the inspector wins
            if (field.GetValue(this) as Object != null)
                continue;

            // no matching entry in the group means this one is not auto assigned
            FieldInfo markerField = typeof(markerNamesGroup).GetField(field.Name);

            if (markerField == null || markerField.FieldType != typeof(string))
                continue;

            string wanted = markerField.GetValue(markerNames) as string;

            // blank is how you say leave this one to me
            if (string.IsNullOrEmpty(wanted))
                continue;

            Transform exactMatch;

            if (objectsByName.TryGetValue(wanted , out exactMatch) && tryAssign(field , exactMatch)) {
                alreadyUsed.Add(exactMatch);
                continue;
            }

            unfilledFields.Add(field);
            unfilledNames.Add(wanted);
        }

        if (unfilledFields.Count == 0)
            return;

        // second pass, whatever is still empty gets a contains match
        for (int i = 0 ; i < unfilledFields.Count ; i++) {
            FieldInfo field = unfilledFields[i];
            string wanted = unfilledNames[i];

            Transform looseMatch = null;
            bool tooManyMatches = false;

            foreach (Transform child in allChildren) {
                if (alreadyUsed.Contains(child))
                    continue;

                if (child.name.IndexOf(wanted , System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (looseMatch == null) {
                    looseMatch = child;
                } else {
                    // two is enough to know the guess is not safe
                    tooManyMatches = true;
                    break;
                }
            }

            // left empty on purpose, a null throws loudly instead of wiring the wrong label forever
            if (tooManyMatches) {
                Debug.LogWarning("gameManager: '" + wanted + "' loosely matched more than one object, leaving '" + field.Name + "' empty" , this);
                continue;
            }

            if (looseMatch == null) {
                Debug.LogWarning("gameManager: nothing named '" + wanted + "' for field '" + field.Name + "'" , this);
                continue;
            }

            Debug.Log("gameManager: '" + field.Name + "' fell back to '" + looseMatch.name + "'. think about renaming object to match" , looseMatch);

            if (tryAssign(field , looseMatch))
                alreadyUsed.Add(looseMatch);
        }
    }

    /// <summary>Writes one object into one field.</summary>
    /// <param name="field">The field being filled.</param>
    /// <param name="source">The object to pull from.</param>
    /// <returns>False if the object does not have the component the field wants.</returns>
    bool tryAssign(FieldInfo field , Transform source) {

        if (field.FieldType == typeof(GameObject)) {
            field.SetValue(this , source.gameObject);
            return true;
        }

        Component component = source.GetComponent(field.FieldType);
        if (component == null) { return false; }

        field.SetValue(this , component);
        return true;
    }

    /// <summary>Walks the whole subtree once and fills both the name lookup and the flat list.</summary>
    /// <param name="parent">Object whose children get walked.</param>
    /// <param name="objectsByName">Name to object lookup, gets filled in.</param>
    /// <param name="allChildren">Flat list of everything found, gets filled in.</param>
    /// <param name="wantedNames">Names a marker is pointing at, anything else can duplicate freely.</param>
    void collectChildren(Transform parent , Dictionary<string , Transform> objectsByName , List<Transform> allChildren , HashSet<string> wantedNames) {
        foreach (Transform child in parent) {
            allChildren.Add(child);

            if (objectsByName.ContainsKey(child.name)) {
                // only worth saying if something is actually searching for this name
                if (wantedNames.Contains(child.name)) {
                    Debug.LogWarning("gameManager: two objects named '" + child.name + "', the first one wins" , child);
                }
            } else {
                objectsByName.Add(child.name , child);
            }

            // child not parent, passing parent here walks the same level forever and crashes unity
            collectChildren(child , objectsByName , allChildren , wantedNames);
        }
    }

#if UNITY_EDITOR
    // runs autoAssign from the component gear menu so unity can save what it wired
    [ContextMenu("Auto assign UI")]
    void autoAssignFromMenu() {
        // lets ctrl z put things back if it wires something wrong
        UnityEditor.Undo.RecordObject(this , "Auto assign UI");

        autoAssign();

        // without these the changes look applied but never get written to the scene file
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

}


// object names autoAssign searches for, grouped so the inspector can fold them away
[System.Serializable]
public class markerNamesGroup {

    [Header("Menus")]
        public string menuPause = "Pause Menu";
        public string menuLose = "Lose Menu";
        public string menuWin = "Win Menu";


    [Header("UI Pages")]
        public string challengesCanvas = "ChallengesCanvas";
        public string settingsCanvas = "SettingsCanvas";
        public string upgradesCanvas = "UpgradesCanvas";


    [Header("Top Navigation")]
        public string navTab = "Nav";
        public string navChallengesButton = "navChallenges";
        public string navSettingsButton = "navSettings";
        public string navUpgradesButton = "navUpgrades";
        public string buttons = "Buttons";
        public string backButton = "Return";


    [Header("Settings Menu")]
        public string soundMenu = "soundMenu";
        public string controlsMenu = "ControlsCanvas";


    [Header("Kills UI")]
        public string pauseScorePanel = "Score";
        public string pauseScoreText = "Value";
        public string loseScoreText = "scoreValue";
        public string killCounter = "killsCounter";


    [Header("Wave UI")]
        public string waveCounter = "waveNumer";
        public string waveCountdownText = "waveCountDown";
        public string waveCountdown = "Countdown";


    [Header("Interaction UI")]
        public string interactionUI = "interactionUI";
        public string interactionText = "interactionText";
        public string interactionKey = "";


    [Header("Player")]
        public string playerSpawnPos = "";
        public string playerStaminaBar = "staminaBar";
        public string checkpointPopup = "";


    [Header("Currency")]
        string bytesText = "bytesText";


    [Header("Shop")]
        public string shopMessage = "shopMessage";
        public string shopUI = "shopUI";


    [Header("Screen Flash")]
        public string damageFlashUI = "Damage Flash";


    [Header("Weapon UI")]
        public string ammoPanel = "ammoPanel";
        public string magAmmoUI = "magAmmo";
        public string totalAmmoUI = "Total";
        public string activeWeapon = "";
}