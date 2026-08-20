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
    [SerializeField] private ChallengeUISlot[] challengeSlots;
    [SerializeField] private challengeData[] challenges;

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

        for (int i = 0; i < challenges.Length; i++)
        {
            if (challenges[i] == null) continue;

            if (!progress.ContainsKey(challenges[i].challengeID))
                progress[challenges[i].challengeID] = 0;

            if (!completed.ContainsKey(challenges[i].challengeID))
                completed[challenges[i].challengeID] = false;
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


    public bool AreAllComplete(challengeData[] required)
    {
        if (required == null || required.Length == 0) return true;

        foreach (var c in required)
        {
            if (c == null) continue;
            if (!IsComplete(c.challengeID)) return false;
        }
        return true;
    }

  
    public int CountComplete(challengeData[] required)
    {
        if (required == null) return 0;

        int n = 0;
        foreach (var c in required)
        {
            if (c != null && IsComplete(c.challengeID)) n++;
        }
        return n;
    }


    public void ReportKill(weaponStats weapon, bool fromGround)
    {
        if (weapon == null || challenges == null || challenges.Length == 0) return;

        foreach (var challenge in challenges)
        {
            if (challenge == null) continue;
            if (IsComplete(challenge.challengeID)) continue;
            if (challenge.targetWeaponID != weapon.weaponID) continue;
            if (challenge.requireGroundPickup && !fromGround) continue;

            int newProgress = GetProgress(challenge.challengeID) + 1;
            progress[challenge.challengeID] = newProgress;

            Debug.Log($"[{challenge.displayName}] {newProgress}/{challenge.killCount}");

            if (newProgress >= challenge.killCount)
                Complete(challenge);
        }

        Save();
    }

    void Complete(challengeData challenge)
    {
        completed[challenge.challengeID] = true;
        Debug.Log($"Challenge Complete: {challenge.displayName}! Unlocked {challenge.rewardWeaponID}");
  
    }

    // ---------- UI ----------

    public void displayWeaponChallenges(challengeData[] weaponChallenges)
    {
        if (weaponChallenges == null || weaponChallenges.Length == 0) return;
        if (challengeSlots == null || challengeSlots.Length == 0) return;

        if (weaponName != null)
            weaponName.text = weaponChallenges[0].targetWeaponID;

        for (int i = 0; i < challengeSlots.Length; i++)
        {
            if (challengeSlots[i] == null || challengeSlots[i].slotRoot == null) continue;

            if (i < weaponChallenges.Length && weaponChallenges[i] != null)
            {
                challengeSlots[i].slotRoot.SetActive(true);

                var challenge = weaponChallenges[i];
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
