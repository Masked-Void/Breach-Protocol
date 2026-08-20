using System.Collections.Generic;
using UnityEngine;

public class challengeManager : MonoBehaviour
{
    public static challengeManager instance { get; private set; }

    [SerializeField] private challengeData[] challenges;

    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private HashSet<string> completed = new HashSet<string>();
    [ContextMenu("Reset Challenges")]
    void ResetChallenges()
    {
        progress.Clear();
        completed.Clear();
        PlayerPrefs.DeleteKey("Challenges_Completed");
        PlayerPrefs.Save();
        //Debug.Log("Challenges reset.");
    }
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        Load();
    }

    public void ReportKill(weaponStats weapon, bool fromGround)
    {
        if (weapon == null || challenges == null || challenges.Length == 0) return;

        foreach (var c in challenges)
        {
            if (completed.Contains(c.challengeID)) continue;
            if (c.targetWeaponID != weapon.weaponID) continue;
            if (c.requireGroundPickup && !fromGround) continue;

            if (!progress.ContainsKey(c.challengeID))
                progress[c.challengeID] = 0;

            progress[c.challengeID]++;

           // Debug.Log($"[{c.displayName}] {progress[c.challengeID]}/{c.killCount}");

            if (progress[c.challengeID] >= c.killCount)
            {
                Complete(c);
            }
        }
    }

    void Complete(challengeData c)
    {
        completed.Add(c.challengeID);
        //Debug.Log($"Challenge Complete: {c.displayName}! Unlocked {c.rewardWeaponID}");
        // TODO: Unlock in your shop here (e.g., ShopManager.instance.Unlock(c.rewardWeaponID))
        Save();
    }

    public bool IsComplete(string id) => completed.Contains(id);
    public int GetProgress(string id) => progress.ContainsKey(id) ? progress[id] : 0;

    void Save()
    {
        PlayerPrefs.SetString("Challenges_Completed", string.Join(",", completed));
        PlayerPrefs.Save();
    }

    void Load()
    {
        string comp = PlayerPrefs.GetString("Challenges_Completed", "");
        if (!string.IsNullOrEmpty(comp))
        {
            foreach (var id in comp.Split(','))
                if (!string.IsNullOrEmpty(id)) completed.Add(id);
        }
    }
}