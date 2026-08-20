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
    public class ChallengeUISlot
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

    [SerializeField] private ChallengeUISlot[] challengeSlots;
    [SerializeField] private challengeData[] challenges;

    private challengeData currentlySelectedChallenge;

    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private Dictionary<string, bool> completed = new Dictionary<string, bool>();
    private Dictionary<string, bool> purchasedWeapons = new Dictionary<string, bool>();

    public saveProgressSystemNative saveProg = new saveProgressSystemNative();
    public saveCompleteSystemNative saveComp = new saveCompleteSystemNative();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        Load();
        InstantiateList();
        Save();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    [ContextMenu("Reset Challenges")]
    void ResetChallenges()
    {
        progress.Clear();
        completed.Clear();
        purchasedWeapons.Clear();
        InstantiateList();
        Save();
        Debug.Log("Challenges reset.");
    }

    void InstantiateList()
    {
        if (challenges == null) return;

        foreach (var cData in challenges)
        {
            if (cData == null || cData.challengesList == null) continue;

            foreach (var subchallenge in cData.challengesList)
            {
                if (string.IsNullOrEmpty(subchallenge.challengeID)) continue;

                if (!progress.ContainsKey(subchallenge.challengeID))
                    progress[subchallenge.challengeID] = 0;

                if (!completed.ContainsKey(subchallenge.challengeID))
                    completed[subchallenge.challengeID] = false;
            }
            if (cData.weapon != null && !purchasedWeapons.ContainsKey(cData.weapon.Name))
            {
                purchasedWeapons[cData.weapon.Name] = false;
            }
        }
    }
    public bool IsComplete(string id)
    {
        return completed.TryGetValue(id, out bool done) && done;
    }

    public int GetProgress(string id)
    {
        return progress.TryGetValue(id, out int p) ? p : 0;
    }

    public bool IsWeaponBought(weaponStats weapon)
    {
        if (weapon == null) return false;
        return purchasedWeapons.TryGetValue(weapon.Name, out bool bought) && bought;
    }

    public void ReportKill(weaponStats weapon, bool fromGround)
    {
        if (weapon == null || challenges == null || challenges.Length == 0) return;

        foreach (var cData in challenges)
        {
            if (cData == null || cData.challengesList == null) continue;
            if (cData.weapon != weapon) continue;
            if (cData.requireGroundPickup && !fromGround) continue;

            foreach (var subchallenge in cData.challengesList)
            {
                if (IsComplete(subchallenge.challengeID)) continue;
                int newProgress = GetProgress(subchallenge.challengeID) + 1;
                progress[subchallenge.challengeID] = newProgress;

                if (newProgress >= subchallenge.killCount)
                    completed[subchallenge.challengeID] = true;
            }
        }

        Save();
    }

    // ---------- UI ----------

    public void displayWeaponChallenges(challengeData weaponChallenge)
    {
        if (weaponChallenge == null || weaponChallenge.challengesList == null) return;
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

        if (weaponName != null && weaponChallenge.weapon != null)
            weaponName.text = weaponChallenge.weapon.Name;

        if (description != null)
            description.text = weaponChallenge.description;

        if (actionButton != null && actionText != null)
        {
            actionButton.onClick.RemoveAllListeners();

            if (!isBought)
            {
                int cost = weaponChallenge.weapon != null ? weaponChallenge.weapon.cost : 0;
                actionText.text = $"Buy ({cost})";

                bool canAfford = upgradeManager.instance != null && upgradeManager.instance.files >= cost;
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
        displayProgressUI(weaponChallenge);

    }

    void displayProgressUI(challengeData weaponChallenge)
    {
        // Display progress slots
        for (int i = 0; i < challengeSlots.Length; i++)
        {
            if (challengeSlots[i] == null || challengeSlots[i].slotRoot == null) continue;

            if (i < weaponChallenge.challengesList.Length)
            {
                challengeSlots[i].slotRoot.SetActive(true);

                var challenge = weaponChallenge.challengesList[i];
                int currentProg = GetProgress(challenge.challengeID);
                float progressRatio = challenge.killCount > 0
                    ? (float)currentProg / challenge.killCount
                    : 0f;

                if (challengeSlots[i].challengeName != null)
                    challengeSlots[i].challengeName.text = challenge.displayName;

                if (challengeSlots[i].progressBar != null)
                    challengeSlots[i].progressBar.fillAmount = Mathf.Clamp01(progressRatio);
            }
            else
            {
                challengeSlots[i].slotRoot.SetActive(false);
            }
        }
    }

    private void buyWeapon(challengeData challenge)
    {
        if (challenge == null || challenge.weapon == null) return;

        if (upgradeManager.instance != null && upgradeManager.instance.files >= challenge.weapon.cost)
        {
            upgradeManager.instance.files -= challenge.weapon.cost;
            purchasedWeapons[challenge.weapon.Name] = true;
            Save();

            displayWeaponChallenges(challenge);
        }
    }

    private void equipWeapon(challengeData challenge)
    {
        if (challenge == null || challenge.weapon == null) return;

        PlayerPrefs.SetString("EquippedWeapon", challenge.weapon.Name);
        PlayerPrefs.Save();

        if (weaponManager.instance != null)
        {
            weaponManager.instance.activeWeapon = challenge.weapon;
        }

        // Refresh UI so previous weapon returns to "Equip" state and current switches to "Equipped"
        displayWeaponChallenges(challenge);
    }

    public bool areAllChallengesComplete(challengeData weaponChallenge)
    {
        if (weaponChallenge == null || weaponChallenge.challengesList.Length == 0) return true;
        foreach (var challenge in weaponChallenge.challengesList)
        {
            if (GetProgress(challenge.challengeID) < challenge.killCount) return false;
        }
        return true;
    }

    void Save()
    {
        saveProg.progressDict = progress;
        saveProg.saveWithJsonUtility();

        saveComp.completeDict = completed;
        saveComp.saveWithJsonUtility();

        // Save purchased weapons
        foreach (var pair in purchasedWeapons)
        {
            PlayerPrefs.SetInt("Bought_" + pair.Key, pair.Value ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    void Load()
    {
        saveProg.loadWithJsonUtility();
        progress = saveProg.progressDict ?? new Dictionary<string, int>();

        saveComp.loadWithJsonUtility();
        completed = saveComp.completeDict ?? new Dictionary<string, bool>();

        // Load purchased weapons
        purchasedWeapons.Clear();
        if (challenges != null)
        {
            foreach (var cData in challenges)
            {
                if (cData != null && cData.weapon != null)
                {
                    purchasedWeapons[cData.weapon.Name] = PlayerPrefs.GetInt("Bought_" + cData.weapon.Name, 0) == 1;
                }
            }
        }
    }
}
