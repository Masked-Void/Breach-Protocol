using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable] 
public class SaveData
{

    public int saveVersion;
    public int files;

    public List<ChallengeEntry> challenges;

    // Update to the one above as added
    public List<string> purchasedWeaponNames;
    public List<string> purchasedUpgradeIDs;
    
    public SaveData()
    {
        saveVersion = 1;
        files = 0;
        challenges = new List<ChallengeEntry>();
        purchasedWeaponNames = new List<string>();
        purchasedUpgradeIDs = new List<string>();
    }

    public void FixNullLists()
    {
        if (challenges == null)
        {
            challenges = new List<ChallengeEntry>();
        }

        // Add the other ones in a similar manner
    }

    // finds a challenge by key, or null if it isn't tracked yet
    public ChallengeEntry GetChallenge(string key)
    {
        for (int i = 0; i < challenges.Count; i++)
        {
            if (challenges[i].key == key)
            {
                return challenges[i];
            }
        }

        return null;
    }

    public int GetProgress(string key)
    {
        ChallengeEntry entry = GetChallenge(key);
        return entry == null ? 0 : entry.progress;
    }

    public bool IsComplete(string key)
    {
        ChallengeEntry entry = GetChallenge(key);
        return entry != null && entry.complete;
    }
}
