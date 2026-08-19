using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using static upgradeManager;
using TMPro;
using UnityEngine.UI;
using System.ComponentModel;

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
    [SerializeField] private ChallengeUISlot[] challengeSlots;
    [SerializeField] private challengeData[] challenges;

    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private Dictionary<string, bool> completed = new Dictionary<string, bool>();

    public saveProgressSystemNative saveProg = new saveProgressSystemNative();
    public saveCompleteSystemNative saveComp = new saveCompleteSystemNative();

    [ContextMenu("Reset Challenges")]
    void ResetChallenges()
    {
        progress.Clear();
        completed.Clear();
        Save();
        Debug.Log("Challenges reset.");
    }
    public void displayWeaponChallenges(challengeData[] weaponChallenges)
    {
        if (weaponChallenges == null || weaponChallenges.Length == 0) return;

        if (weaponName != null)
        {
            weaponName.text = weaponChallenges[0].targetWeaponID;
        }

        for (int i = 0; i < challengeSlots.Length; i++)
        {
            if (i < weaponChallenges.Length)
            {
                // Show slot & populate data
                challengeSlots[i].slotRoot.SetActive(true);

                var challenge = weaponChallenges[i];
                int currentProg = GetProgress(challenge.challengeID);
                float progressRatio = challenge.killCount > 0 ? (float)currentProg / challenge.killCount : 0f;

                if (challengeSlots[i].challengeName != null)
                {
                    challengeSlots[i].challengeName.text = challenge.displayName;
                }

                if (challengeSlots[i].progressBar != null)
                {
                    challengeSlots[i].progressBar.fillAmount = Mathf.Clamp01(progressRatio);
                }
            }
            else
            {
                // Hide slot if the weapon has fewer than 3 challenges
                challengeSlots[i].slotRoot.SetActive(false);
            }
        }
    }
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (saveProg != null || saveComp != null)
        {
            Load();
            InstantiateList();
            Save();
        }
        else
        {
            if (saveProg == null)
            {
                Debug.LogError("challengeManager: saveProg not assigned");
            }
            if (saveComp == null)
            {
                Debug.LogError("challengeManager: saveComp not assigned");
            }
        }

    }

    private void OnDestroy() {
        if (instance == this)
            instance = null;
    }

    void InstantiateList()
    {
        for (int i = 0; i < challenges.Length; i++)
        {
            if (!progress.ContainsKey(challenges[i].challengeID))
            {
                progress[challenges[i].challengeID] = 0;
            }
            if (!completed.ContainsKey(challenges[i].challengeID))
            {
                completed[challenges[i].challengeID] = false;
            }
        }
    }

    public void ReportKill(weaponStats weapon, bool fromGround)
    {

        if (weapon == null || challenges == null || challenges.Length == 0) return;

        foreach (var challenge in challenges)
        {
            if (completed[challenge.challengeID] == true) continue;
            if (challenge.targetWeaponID != weapon.weaponID) continue;
            if (challenge.requireGroundPickup && !fromGround) continue;

            progress[challenge.challengeID]++;

            Debug.Log($"[{challenge.displayName}] {progress[challenge.challengeID]}/{challenge.killCount}");

            if (progress[challenge.challengeID] >= challenge.killCount)
            {
                Complete(challenge);
            }
        }

        Save();
    }

    void Complete(challengeData challenge)
    {
        completed[challenge.challengeID] = true;
        Debug.Log($"Challenge Complete: {challenge.displayName}! Unlocked {challenge.rewardWeaponID}");
        // TODO: Unlock in your shop here (e.g., ShopManager.instance.Unlock(c.rewardWeaponID))
        Save();
    }

    public bool IsComplete(string id) => completed[id];
    public int GetProgress(string id) => progress.ContainsKey(id) ? progress[id] : 0;

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
        progress = saveProg.progressDict;

        saveComp.loadWithJsonUtility();
        completed = saveComp.completeDict;
    }
}