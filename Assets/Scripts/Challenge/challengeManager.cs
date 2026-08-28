using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using static upgradeManager;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class challengeManager : MonoBehaviour
{
    public static challengeManager instance { get; private set; }

    [System.Serializable]
    public struct ChallengeUISlot
    {
        public GameObject slotRoot;
        public TextMeshProUGUI challengeName;
        public Image progressBar;
    }

    [Header("Challenges Data")]
    public TextMeshProUGUI weaponName;
    public TextMeshProUGUI description;
    public GameObject statsPanel;
    public Button actionButton;
    public TextMeshProUGUI actionText;

    [Header("Currency")]
    public TextMeshProUGUI fileCountText;

    [SerializeField] ChallengeUISlot[] challengeSlots;
    [SerializeField] challengeData[] challenges;

    challengeData currentlySelectedChallenge;

    Dictionary<string, int> progress = new Dictionary<string, int>();
    Dictionary<string, bool> completed = new Dictionary<string, bool>();
    HashSet<string> purchasedWeapons = new HashSet<string>();
    Dictionary<weaponStats, List<challengeData>> weaponChallengeLookup = new Dictionary<weaponStats, List<challengeData>>();


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

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void InstantiateList()
    {
        weaponChallengeLookup.Clear();
        if (challenges == null) return;

        foreach (var cData in challenges)
        {
            if (cData == null || cData.weapon == null) continue;

            if (!weaponChallengeLookup.TryGetValue(cData.weapon, out var list))
            {
                list = new List<challengeData>();
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

    public bool IsWeaponBought(weaponStats weapon)
    {
        return weapon != null && !string.IsNullOrEmpty(weapon.Name) && purchasedWeapons.Contains(weapon.Name);
    }

    public void ReportKill(weaponStats weapon)
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

    public void displayWeaponChallenges(challengeData weaponChallenge)
    {
        if (weaponChallenge == null) return;
        currentlySelectedChallenge = weaponChallenge;

        bool allComplete = areAllChallengesComplete(weaponChallenge);
        bool isBought = IsWeaponBought(weaponChallenge.weapon);

        string savedEquipped = PlayerPrefs.GetString("EquippedWeapon", "");
        bool isEquipped = false;

        if (weaponChallenge.weapon != null && !string.IsNullOrEmpty(weaponChallenge.weapon.Name))
        {
            if (!string.IsNullOrEmpty(savedEquipped))
            {
                isEquipped = (weaponChallenge.weapon.Name == savedEquipped);
            }
            else if (weaponManager.instance != null)
            {
                isEquipped = (weaponManager.instance.activeWeapon == weaponChallenge.weapon);
            }
        }

        if (statsPanel != null) statsPanel.SetActive(allComplete);
        if (weaponName != null && weaponChallenge.weapon != null) weaponName.text = weaponChallenge.weapon.Name;
        if (description != null) description.text = weaponChallenge.description;
        if(fileCountText != null && upgradeManager.instance != null) fileCountText.text = "" + upgradeManager.instance.files;

        updateActionButton(weaponChallenge, isBought, isEquipped);
        displayProgressUI(weaponChallenge);
    }

    void updateActionButton(challengeData weaponChallenge, bool isBought, bool isEquipped)
    {
        if (actionButton != null && actionText != null)
        {
            actionButton.onClick.RemoveAllListeners();

            if (!isBought)
            {
                int cost = weaponChallenge.weapon != null ? weaponChallenge.weapon.cost : 0;
                actionText.text = $"Buy ({cost})";

                int currentFiles = upgradeManager.instance != null ? upgradeManager.instance.files : 0;
                bool canAfford = currentFiles >= cost; ;

                actionButton.interactable = canAfford;
                actionButton.onClick.AddListener(() => buyWeapon(weaponChallenge));
            }
            else if (!isEquipped)
            {
                actionText.text = "Equip";
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => equipWeapon(weaponChallenge));
            }
            else
            {
                actionText.text = "Equipped";
                actionButton.interactable = false;
            }
        }
    }

    void displayProgressUI(challengeData weaponChallenge)
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

    void buyWeapon(challengeData challenge)
    {
        if (audioManager.instance != null)
            audioManager.instance.playButtonClick();

        if (challenge == null || challenge.weapon == null) return;

        if (upgradeManager.instance != null && upgradeManager.instance.files >= challenge.weapon.cost)
        {
            upgradeManager.instance.files -= challenge.weapon.cost;

            purchasedWeapons.Add(challenge.weapon.Name);
            PlayerPrefs.SetInt("Bought_" + challenge.weapon.Name, 1);

            if (upgradeManager.instance != null) upgradeManager.instance.SaveUpgrades();
            PlayerPrefs.Save();

            displayWeaponChallenges(challenge);
        }
    }

    void equipWeapon(challengeData challenge)
    {
        if (audioManager.instance != null)
            audioManager.instance.playButtonClick();

        if (challenge == null || challenge.weapon == null) return;

        PlayerPrefs.SetString("EquippedWeapon", challenge.weapon.Name);
        PlayerPrefs.Save();

        if (weaponManager.instance != null)
            weaponManager.instance.activeWeapon = challenge.weapon;

        // Refresh UI so previous weapon returns to "Equip" state and current switches to "Equipped"
        displayWeaponChallenges(challenge);
    }

    public bool areAllChallengesComplete(challengeData weaponChallenge)
    {
        if (weaponChallenge == null || weaponChallenge.challengesList == null || weaponChallenge.challengesList.Length == 0) return true;
        foreach (var challenge in weaponChallenge.challengesList)
            if (GetProgress(challenge.challengeID) < challenge.killCount) return false;

        return true;
    }

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
        if (currentlySelectedChallenge != null) displayWeaponChallenges(currentlySelectedChallenge);
        Debug.Log("Challenges reset successfully.");
    }
}
