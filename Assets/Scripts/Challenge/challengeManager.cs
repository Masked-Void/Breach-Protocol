using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using static upgradeManager;
using TMPro;
using UnityEngine.UI;

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

    public TextMeshProUGUI weaponName;
    public TextMeshProUGUI description;
    [SerializeField] private ChallengeUISlot[] challengeSlots;
    [SerializeField] private challengeData[] challenges;
    public GameObject statsPanel;
    public Button buyButton;
    public Button equipButton;

    bool canBuy;
    bool canEquip;

    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private Dictionary<string, bool> completed = new Dictionary<string, bool>();

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

        canBuy = true;
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
        InstantiateList();
        Save();
        Debug.Log("Challenges reset.");
    }

    void InstantiateList()
    {
        if (challenges == null) return;

        foreach (var cData in challenges)
        {
            foreach (var subchallenge in cData.challengesList)
            {
                if (string.IsNullOrEmpty(subchallenge.challengeID)) continue;

                if (!progress.ContainsKey(subchallenge.challengeID))
                    progress[subchallenge.challengeID] = 0;

                if (!completed.ContainsKey(subchallenge.challengeID))
                    completed[subchallenge.challengeID] = false;
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
                if(IsComplete(subchallenge.challengeID)) continue;
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
        if (challengeSlots == null || challengeSlots.Length == 0) return;
        bool allComplete = areAllChallengesComplete(weaponChallenge);
        if (statsPanel != null) statsPanel.SetActive(allComplete);

        if (weaponName != null && weaponChallenge.weapon != null)
            weaponName.text = weaponChallenge.weapon.Name;

        if (description != null)
            description.text = weaponChallenge.description;
        
        if (buyButton != null)
        {
            buyButton.interactable = allComplete;
        }

        if (equipButton != null)
        {
            equipButton.interactable = allComplete;
            equipButton.onClick.RemoveAllListeners();
        }

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

    public bool areAllChallengesComplete(challengeData weaponChallenge)
    {
        if (weaponChallenge == null && weaponChallenge.challengesList.Length == 0) return true;
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
    }

    void Load()
    {
        saveProg.loadWithJsonUtility();
        progress = saveProg.progressDict ?? new Dictionary<string, int>();

        saveComp.loadWithJsonUtility();
        completed = saveComp.completeDict ?? new Dictionary<string, bool>();
    }
}
