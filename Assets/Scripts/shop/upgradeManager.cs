using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class upgradeManager : MonoBehaviour
{
    public static upgradeManager instance;
    public upgradeData[] upgrades;
    public TextMeshProUGUI upgradeName;
    public TextMeshProUGUI upgradeDescription;
    public TextMeshProUGUI upgradeCost;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public List<string> unlockedUpgrades = new List<string>();
    public List<string> purchasedUpgrades = new List<string>();
    public int files;
    void Awake()
    {
        instance = this;
        LoadUpgrades();
    }

    // Display Upgrade Info i.e Name, cost etc
    public void displayUpgrades(string id)
    {
        // Loop through all upgrades
        foreach (var upgrade in upgrades)
        {
            // Check if IDs match
            if (upgrade.Id == id)
            {
                if (upgradeName != null) upgradeName.text = upgrade.UpgradeName;
                if (upgradeDescription != null) upgradeDescription.text = upgrade.Description;
                if (upgradeCost != null) upgradeCost.text = "" + upgrade.Cost;

                bool isPurchased = purchasedUpgrades.Contains(id);
                bool isUnlocked = unlockedUpgrades.Contains(id);
                bool canBuy = files >= upgrade.Cost;

                if (buyButton != null)
                {
                    buyButton.onClick.RemoveAllListeners();

                    // Change button status based on upgrade status
                    if (isPurchased)
                    {
                        if (buyButtonText != null) buyButtonText.text = "Purchsed";
                        buyButton.interactable = false;
                    }
                    else if (!isUnlocked)
                    {
                        if (buyButtonText != null) buyButtonText.text = "Locked";
                        buyButton.interactable = false;
                    }
                    else
                    {
                        if (buyButtonText != null) buyButtonText.text = "Buy";
                        buyButton.interactable = canBuy;
                        buyButton.onClick.AddListener(() => buyButtonClicked(upgrade));
                    }
                }
                break;
            }
        }
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

    // Buy button click event
    void buyButtonClicked(upgradeData upgrade)
    {
        // Check if player can afford upgrade
        if (files >= upgrade.Cost && !purchasedUpgrades.Contains(upgrade.Id))
        {
            files -= upgrade.Cost;
            PurchaseUpgrade(upgrade.Id);
            SaveUpgrades();
            displayUpgrades(upgrade.Id); // Immediately reflect the purchase status

            if (audioManager.instance != null)
            {
                audioManager.instance.playButtonClick();
            }
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
