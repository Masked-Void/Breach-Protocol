using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using static UpgradeManager;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/*
 * Script: ChallengeManager
 *
 * Description:
 * Tracks per-weapon challenges and their completion state, and drives the
 * challenge UI panel. Progress persists between runs.
 *
 * Responsibilities:
 * - Record kills against the weapon that made them
 * - Save and load challenge progress
 * - Populate the challenge UI slots and their button state
 * - Report when every challenge for a weapon is complete
 *
 * Interacts With:
 * - EnemyEvents (subscribes to Killed)
 * - ChallengeData, ChallengeButtonData (the challenge assets)
 * - UpgradeManager (completed challenges unlock upgrades)
 * - WeaponStats (challenges are per weapon)
 *
 * Notes:
 * - This class has a history of save-system bugs: a MonoBehaviour instantiated
 *   with new, a broken null check, and a KeyNotFoundException. Test saves after
 *   any change here.
 */

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager instance { get; private set; }

    // one row in the challenge panel, a name and a progress bar
    [System.Serializable]
    public struct ChallengeUISlot
    {
        [Tooltip("the whole row, hidden when a weapon has fewer challenges than there are slots")]
        public GameObject slotRoot;

        [Tooltip("challenge display name, e.g. Kunai Collector")]
        public TextMeshProUGUI challengeName;

        [Tooltip("fills 0 to 1 as kills accumulate toward the target")]
        public Image progressBar;
    }

    [Header("Challenges Data")]
    [Tooltip("heading showing the selected weapon's name")]
    public TextMeshProUGUI weaponName;

    [Tooltip("description text under the heading")]
    public TextMeshProUGUI description;

    [Tooltip("root of the challenge rows, hidden when nothing is selected")]
    public GameObject statsPanel;

    [Tooltip("buy or equip button, its behaviour changes with the weapon's state")]
    public Button actionButton;

    [Tooltip("label on the action button, swaps between Buy, Equip and Equipped")]
    public TextMeshProUGUI actionText;

    [Header("Currency")]
    [Tooltip("Files balance shown in the corner of the panel")]
    public TextMeshProUGUI fileCountText;

    [Tooltip("the challenge rows, more slots than any weapon needs, spares are hidden")]
    [SerializeField] ChallengeUISlot[] challengeSlots;

    [Tooltip("every challenge set in the game, one per weapon")]
    [SerializeField] ChallengeData[] challenges;

    // which weapon's challenges the panel is currently showing
    ChallengeData currentlySelectedChallenge;

    // progress and completion keyed by challengeID, saved between runs
    Dictionary<string, int> progress = new Dictionary<string, int>();
    Dictionary<string, bool> completed = new Dictionary<string, bool>();

    // weapons the player has bought, keyed by weapon name
    HashSet<string> purchasedWeapons = new HashSet<string>();

    // weapon to its challenge sets, built once on Awake so ReportKill does not
    // walk the whole challenge array on every kill
    Dictionary<WeaponStats, List<ChallengeData>> weaponChallengeLookup = new Dictionary<WeaponStats, List<ChallengeData>>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InstantiateList();
        LoadData();
    }


    private void OnEnable() => EnemyEvents.Killed += handleKill;
    private void OnDisable() => EnemyEvents.Killed -= handleKill;

    private void handleKill(EnemyBase enemy)
    {
        if (enemy.LastDamageWeapon != null){
            ReportKill(enemy.LastDamageWeapon);
        }
        
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // builds the weapon to challenge lookup so kills can be credited without
    // searching the whole array each time
    void InstantiateList()
    {
        weaponChallengeLookup.Clear();
        if (challenges == null) return;

        foreach (var cData in challenges)
        {
            if (cData == null || cData.weapon == null) continue;

            if (!weaponChallengeLookup.TryGetValue(cData.weapon, out var list))
            {
                list = new List<ChallengeData>();
                weaponChallengeLookup[cData.weapon] = list;
            }
            list.Add(cData);
        }
    }

    public bool IsComplete(string id)
    {
        return !string.IsNullOrEmpty(id) && completed.TryGetValue(id, out bool done) && done;
    }

    public int GetProgress(string id)
    {
        return !string.IsNullOrEmpty(id) && progress.TryGetValue(id, out int p) ? p : 0;
    }

    public bool IsWeaponBought(WeaponStats weapon)
    {
        return weapon != null && !string.IsNullOrEmpty(weapon.Name) && purchasedWeapons.Contains(weapon.Name);
    }

    // credits a kill to every challenge tier that uses this weapon, and marks
    // any that just hit their target as complete
    public void ReportKill(WeaponStats weapon)
    {
        if (weapon == null || !weaponChallengeLookup.TryGetValue(weapon, out var associatedChallenges)) return;
        bool hasProgressChanged = false;

        foreach (var cData in associatedChallenges)
        {
            if (cData == null || cData.challengesList == null) continue;

            foreach (var subchallenge in cData.challengesList)
            {
                string id = subchallenge.challengeID;
                if (string.IsNullOrEmpty(id) || IsComplete(id)) continue;

                int newProgress = GetProgress(id) + 1;
                progress[id] = newProgress;
                PlayerPrefs.SetInt("Prog_" + id, newProgress);

                if (newProgress >= subchallenge.killCount)
                {
                    completed[id] = true;
                    PlayerPrefs.SetInt("Comp_" + id, 1);
                }

                hasProgressChanged = true;
            }
        }
        if (hasProgressChanged) PlayerPrefs.Save();
    }

    // ---------- UI ----------

    // fills the panel with one weapon's challenges and sets the action button
    public void DisplayWeaponChallenges(ChallengeData weaponChallenge)
    {
        if (weaponChallenge == null) return;
        currentlySelectedChallenge = weaponChallenge;

        bool allComplete = AreAllChallengesComplete(weaponChallenge);
        bool isBought = IsWeaponBought(weaponChallenge.weapon);

        string savedEquipped = PlayerPrefs.GetString("EquippedWeapon", "");
        bool isEquipped = false;

        if (weaponChallenge.weapon != null && !string.IsNullOrEmpty(weaponChallenge.weapon.Name))
        {
            if (!string.IsNullOrEmpty(savedEquipped))
            {
                isEquipped = (weaponChallenge.weapon.Name == savedEquipped);
            }
            else if (WeaponManager.instance != null)
            {
                isEquipped = (WeaponManager.instance.activeWeapon == weaponChallenge.weapon);
            }
        }

        if (statsPanel != null) statsPanel.SetActive(allComplete);
        if (weaponName != null && weaponChallenge.weapon != null) weaponName.text = weaponChallenge.challengeName + $"  ({weaponChallenge.weapon.Name})";
        if (description != null) description.text = weaponChallenge.description;
        if(fileCountText != null && UpgradeManager.instance != null) fileCountText.text = "" + UpgradeManager.instance.files;

        UpdateActionButton(weaponChallenge, isBought, isEquipped);
        displayProgressUI(weaponChallenge);
    }

    // the button does something different depending on whether the weapon is
    // locked, affordable, owned, or already equipped
    void UpdateActionButton(ChallengeData weaponChallenge, bool isBought, bool isEquipped)

    {
        if (actionButton != null && actionText != null)
        {
            actionButton.onClick.RemoveAllListeners();

            if (!isBought)
            {
                int cost = weaponChallenge.weapon != null ? weaponChallenge.weapon.cost : 0;
                if(weaponChallenge.weapon.name != "Pistol") actionText.text = $"Buy ({cost})";

                int currentFiles = UpgradeManager.instance != null ? UpgradeManager.instance.files : 0;
                bool canAfford = currentFiles >= cost;
                bool allComplete = AreAllChallengesComplete(weaponChallenge);

                actionButton.interactable = canAfford && allComplete;
                actionButton.onClick.AddListener(() => buyWeapon(weaponChallenge));
            }
            else if (!isEquipped)
            {
                actionText.text = "Equip";
                actionButton.interactable = SceneManager.GetActiveScene().name == "Title";
                actionButton.onClick.AddListener(() => equipWeapon(weaponChallenge));
            }
            else
            {
                actionText.text = "Equipped";
                actionButton.interactable = false;
            }
        }
    }

    // fills one row per challenge tier and hides the spare slots
    void displayProgressUI(ChallengeData weaponChallenge)
    {
        if (challengeSlots == null || weaponChallenge.challengesList == null) return;

        // Display progress slots
        for (int i = 0; i < challengeSlots.Length; i++)
        {
            if (challengeSlots[i].slotRoot == null) continue;

            if (i < weaponChallenge.challengesList.Length)
            {
                challengeSlots[i].slotRoot.SetActive(true);

                var challenge = weaponChallenge.challengesList[i];
                int currentProg = GetProgress(challenge.challengeID);
                float progressRatio = challenge.killCount > 0
                    ? (float)currentProg / challenge.killCount
                    : 0f;

                if (challengeSlots[i].challengeName != null)
                    challengeSlots[i].challengeName.text = challenge.displayName + $"   ({challenge.killCount} kills)";

                if (challengeSlots[i].progressBar != null)
                    challengeSlots[i].progressBar.fillAmount = Mathf.Clamp01(progressRatio);
            }
            else
            {
                challengeSlots[i].slotRoot.SetActive(false);
            }
        }
    }

    // spends Files and marks the weapon owned
    void buyWeapon(ChallengeData challenge)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        if (challenge == null || challenge.weapon == null) return;

        if (UpgradeManager.instance != null && UpgradeManager.instance.files >= challenge.weapon.cost)
        {
            UpgradeManager.instance.files -= challenge.weapon.cost;

            purchasedWeapons.Add(challenge.weapon.Name);
            PlayerPrefs.SetInt("Bought_" + challenge.weapon.Name, 1);

            if (UpgradeManager.instance != null) UpgradeManager.instance.SaveUpgrades();
            PlayerPrefs.Save();

            DisplayWeaponChallenges(challenge);
        }
    }

    // equips an owned weapon straight from the panel. named the same as the
    // WeaponManager method but unrelated to it.
    void equipWeapon(ChallengeData challenge)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();

        if (challenge == null || challenge.weapon == null) return;

        PlayerPrefs.SetString("EquippedWeapon", challenge.weapon.Name);
        PlayerPrefs.Save();

        if (WeaponManager.instance != null)
            WeaponManager.instance.activeWeapon = challenge.weapon;

        // Refresh UI so previous weapon returns to "Equip" state and current switches to "Equipped"
        DisplayWeaponChallenges(challenge);
    }

    public bool AreAllChallengesComplete(ChallengeData weaponChallenge)
    {
        if (weaponChallenge == null || weaponChallenge.challengesList == null || weaponChallenge.challengesList.Length == 0) return true;
        foreach (var challenge in weaponChallenge.challengesList)
            if (GetProgress(challenge.challengeID) < challenge.killCount) return false;

        return true;
    }

    // reads saved progress into the dictionaries on Awake
    void LoadData()
    {
        progress.Clear();
        completed.Clear();
        purchasedWeapons.Clear();

        if (challenges == null) return;

        foreach (var cData in challenges)
        {
            if (cData == null) continue;

            if (cData.challengesList != null)
            {
                foreach (var sub in cData.challengesList)
                {
                    if (string.IsNullOrEmpty(sub.challengeID)) continue;

                    int progValue = PlayerPrefs.GetInt("Prog_" + sub.challengeID, 0);
                    bool compValue = PlayerPrefs.GetInt("Comp_" + sub.challengeID, 0) == 1;

                    progress[sub.challengeID] = progValue;
                    completed[sub.challengeID] = compValue;
                }
            }

            if (cData.weapon != null && !string.IsNullOrEmpty(cData.weapon.Name))
            {
                if (PlayerPrefs.GetInt("Bought_" + cData.weapon.Name, 0) == 1)
                {
                    purchasedWeapons.Add(cData.weapon.Name);
                }
            }
        }
    }

    [ContextMenu("Reset Challenges")]
    // wipes all progress. inspector and debug only.
    public void ResetChallenges()
    {
        if (challenges != null)
        {
            foreach (var cData in challenges)
            {
                if (cData == null) continue;

                if (cData.challengesList != null)
                {
                    foreach (var sub in cData.challengesList)
                    {
                        if (string.IsNullOrEmpty(sub.challengeID)) continue;
                        PlayerPrefs.DeleteKey("Prog_" + sub.challengeID);
                        PlayerPrefs.DeleteKey("Comp_" + sub.challengeID);
                    }
                }

                if (cData.weapon != null && !string.IsNullOrEmpty(cData.weapon.Name))
                {
                    PlayerPrefs.DeleteKey("Bought_" + cData.weapon.Name);
                }
            }
        }

        PlayerPrefs.Save();
        LoadData();
        if (currentlySelectedChallenge != null) DisplayWeaponChallenges(currentlySelectedChallenge);
        Debug.Log("Challenges reset successfully.");
    }
}
