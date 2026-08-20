using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class upgradeManager : MonoBehaviour
{
    public static upgradeManager instance;
    public upgradeData[] upgrades;
    public TextMeshProUGUI upgradeName;
    public TextMeshProUGUI upgradeDescription;
    public TextMeshProUGUI upgradeCost;
    public List<string> unlockedUpgrades = new List<string>();
    public List<string> purchasedUpgrades = new List<string>();
    public int files;
   public void  displayUpgrades(string id)
    {
        foreach (var upgrade in upgrades)
        {
            if (upgrade.Id == id)
            {
                upgradeName.text = upgrade.UpgradeName;
                upgradeDescription.text = upgrade.Description;
                upgradeCost.text = ""+ upgrade.Cost;
            }
        }
    }
    void Awake()
    {
        instance = this;
        LoadUpgrades();
    }

    public void UnlockUpgrade(string id)
    {
        if (!unlockedUpgrades.Contains(id))
        {
           
            unlockedUpgrades.Add(id);
            SaveUpgrades();
        }
    }

    public void PurchaseUpgrade(string id)
    {
        if (!purchasedUpgrades.Contains(id))
        {
            purchasedUpgrades.Add(id);
            
        }
    }

    

    [System.Serializable]
    public class upgradeSaveData
    {
        public List<string> unlocked;
        public int files;
    }

    public void SaveUpgrades()
    {
        upgradeSaveData data = new upgradeSaveData
        {
            unlocked = unlockedUpgrades,
            files = files
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("UnlockedUpgrades", json);
        PlayerPrefs.Save();
    }

    public void LoadUpgrades()
    {
        if (!PlayerPrefs.HasKey("UnlockedUpgrades")) return;

        string json = PlayerPrefs.GetString("UnlockedUpgrades");
        upgradeSaveData data = JsonUtility.FromJson<upgradeSaveData>(json);

        unlockedUpgrades = data.unlocked;
        files = data.files;
    }

    public void Debug_ResetUnlockables()
    {
        unlockedUpgrades.Clear();
        files = 0;
        SaveUpgrades();
    }
}
