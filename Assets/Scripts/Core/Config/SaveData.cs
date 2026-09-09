using System.Collections.Generic;
using System;


[Serializable] 
public class SaveData
{
    public const int CurrentSaveVersion = 1;
    public int saveVersion;
    public int files;

    public List<ChallengeEntry> challengeEntries;

    public string equippedWeaponName;
   
    public List<string> purchasedWeaponNames;
    public List<string> purchasedUpgradeIDs;
    public List<string> activeUpgradeIDs;
    //Audio
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    public bool isMuted;

    public SaveData()
    {
        saveVersion = CurrentSaveVersion;
        files = 0;
        masterVolume = 1f;
        musicVolume = 1f;
        sfxVolume = 1f;
        isMuted = false;
        equippedWeaponName = string.Empty;
        challengeEntries = new List<ChallengeEntry>();
        purchasedWeaponNames = new List<string>();
        purchasedUpgradeIDs = new List<string>();
        activeUpgradeIDs = new List<string>();
    }

    public void FixNulls()
    {
        if (challengeEntries == null)
        {
            challengeEntries = new List<ChallengeEntry>();
        }

        if (purchasedWeaponNames == null)
        {
            purchasedWeaponNames = new List<string>();
        }

        if (purchasedUpgradeIDs == null)
        {
            purchasedUpgradeIDs = new List<string>();
        }

        if (activeUpgradeIDs == null)
        {
            activeUpgradeIDs = new List<string>();
        }

        if (equippedWeaponName == null)
        {
            equippedWeaponName = string.Empty;
        }
  
    }

    // finds a challenge by id, or null if it isn't tracked yet
    public ChallengeEntry GetChallenge(string id)
    {
        for (int i = 0; i < challengeEntries.Count; i++)
        {
            if (challengeEntries[i].id == id)
            {
                return challengeEntries[i];
            }
        }

        return null;
    }

    public int GetProgress(string id)
    {
        ChallengeEntry entry = GetChallenge(id);
        return entry == null ? 0 : entry.progress;
    }

    public bool IsComplete(string id)
    {
        ChallengeEntry entry = GetChallenge(id);
        return entry != null && entry.complete;
    }
}
