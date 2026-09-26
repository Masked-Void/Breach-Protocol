using System.Collections.Generic;
using System;


[Serializable] 
public class SaveData
{
    public const int CurrentSaveVersion = 1;
    public int saveVersion;
    public int files;

    public List<ChallengeEntry> challengeEntries;

    public List<UpgradeEntry> upgradeEntries;

    public string equippedWeaponName;
   
    public List<string> purchasedWeaponNames;
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
        upgradeEntries = new List<UpgradeEntry>();
        purchasedWeaponNames = new List<string>();
    }

    public void FixNulls()
    {
        if (challengeEntries == null)
        {
            challengeEntries = new List<ChallengeEntry>();
        }

        if (upgradeEntries == null)
        {
            upgradeEntries = new List<UpgradeEntry>();
        }

        if (purchasedWeaponNames == null)
        {
            purchasedWeaponNames = new List<string>();
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

    // finds a upgrade by id, or null if it isn't tracked yet
    public UpgradeEntry GetUpgrade(string id)
    {
        for (int i = 0; i < upgradeEntries.Count; i++)
        {
            if (upgradeEntries[i].id == id)
            {
                return upgradeEntries[i];
            }
        }

        return null;
    }

    public bool IsUpgradePurchased(string id)
    {
        UpgradeEntry entry = GetUpgrade(id);
        return entry != null && entry.purchased;
    }

    public bool IsUpgradeActive(string id)
    {
        UpgradeEntry entry = GetUpgrade(id);
        return entry != null && entry.active;
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
