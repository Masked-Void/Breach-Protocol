using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class upgradeManager : MonoBehaviour
{
    public static upgradeManager instance;
    [Header("Upgrade Info")]
    public upgradeData[] upgrades;
    public TextMeshProUGUI upgradeName;
    public TextMeshProUGUI upgradeDescription;
    public TextMeshProUGUI upgradeCost;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public int files;

    [HideInInspector]
    public List<string> unlockedUpgrades = new List<string>();
    public List<string> purchasedUpgrades = new List<string>();
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        LoadUpgrades();
    }

    // Display Upgrade Info i.e Name, cost etc
    public void displayUpgrades(upgradeData upgrade)
    {
        if (upgrade == null) return;
        if (upgradeName != null) upgradeName.text = upgrade.UpgradeName;
        if (upgradeDescription != null) upgradeDescription.text = upgrade.Description;
        if (upgradeCost != null) upgradeCost.text = "" + upgrade.Cost;

        bool isPurchased = purchasedUpgrades.Contains(upgrade.Id);
        bool isUnlocked = unlockedUpgrades.Contains(upgrade.Id);
        bool canBuy = files >= upgrade.Cost;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();

            // Change button status based on upgrade status
            if (isPurchased)
            {
                setButtonState("Purchased", Color.black, false);
            }
            else if (!isUnlocked)
            {
                setButtonState("Locked", Color.black, false);
            }
            else
            {
                setButtonState("Buy", canBuy ? Color.white : Color.black, canBuy);
                buyButton.onClick.AddListener(() => buyButtonClicked(upgrade));
            }
        }
    }

    private void setButtonState(string text, Color color, bool interactable)
    {
        if (buyButtonText != null)
        {
            buyButtonText.text = text;
            buyButtonText.color = color;
        }
        buyButton.interactable = interactable;
    }

    // Buy button click event
    void buyButtonClicked(upgradeData upgrade)
    {
        // Check if player can afford upgrade
        if (files >= upgrade.Cost && !purchasedUpgrades.Contains(upgrade.Id))
        {
            files -= upgrade.Cost;
            purchasedUpgrades.Add(upgrade.Id);
            SaveUpgrades();
            displayUpgrades(upgrade); // Immediately reflect the purchase status

            if (audioManager.instance != null)
                audioManager.instance.playButtonClick();
        }
    }

    [System.Serializable]
    public class upgradeSaveData
    {
        public List<string> unlocked;
        public List<string> purchased;
        public int files;
    }

    public void SaveUpgrades()
    {
        upgradeSaveData data = new upgradeSaveData
        {
            unlocked = unlockedUpgrades,
            purchased = purchasedUpgrades,
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

        unlockedUpgrades = data.unlocked ?? new List<string>();
        purchasedUpgrades = data.purchased ?? new List<string>();
        files = data.files;
    }
}
