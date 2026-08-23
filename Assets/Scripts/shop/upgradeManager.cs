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
    public Image upgradeIcon;
    public TextMeshProUGUI upgradeDescription;
    public TextMeshProUGUI upgradeCost;
    public TextMeshProUGUI upgradeValue;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public int files;

    [HideInInspector]
    public List<string> purchasedUpgrades = new List<string>();
    public List<string> activeUpgrades = new List<string>();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        LoadUpgrades();
    }

    public bool IsUpgradeActive(string id) => activeUpgrades.Contains(id);

    public bool IsUpgradeUnlocked(upgradeData upgrade)
    {
        if (upgrade == null) return false;

        // If no challenges are required, unlock automatically
        if (upgrade.requiredChallenges == null || upgrade.requiredChallenges.Length == 0)
            return true;

        if (challengeManager.instance == null)
            return false;

        // Verify all required challenge groups are completed
        foreach (var reqChallenge in upgrade.requiredChallenges)
        {
            if (reqChallenge != null && !challengeManager.instance.areAllChallengesComplete(reqChallenge))
            {
                return false;
            }
        }

        return true;
    }

    // Display Upgrade Info i.e Name, cost etc
    public void displayUpgrades(upgradeData upgrade)
    {
        if (upgrade == null) return;
        if (upgradeName != null) upgradeName.text = upgrade.upgradeName;
        if (upgradeIcon != null) upgradeIcon.sprite = upgrade.icon;

        if (upgradeDescription != null)
        {
            upgradeDescription.text = upgrade.upgradeType == upgradeData.UpgradeType.FireRate
                ? upgrade.description + $". Reduces reload time by 1/{upgrade.value}"
                : upgrade.description;
        }
        if (upgradeCost != null) upgradeCost.text = "" + upgrade.cost;
        if (upgradeValue != null) upgradeValue.text = "" + upgrade.value;

        bool isPurchased = purchasedUpgrades.Contains(upgrade.id);
        bool isUnlocked = IsUpgradeUnlocked(upgrade);
        bool isActive = activeUpgrades.Contains(upgrade.id);
        bool canBuy = files >= upgrade.cost;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            if (!isUnlocked)
            {
                setButtonState("Locked", Color.gray, false);
            }
            // Change button status based on upgrade status
            else if (!isPurchased)
            {
                setButtonState("Buy", canBuy ? Color.white : Color.gray, canBuy);
                buyButton.onClick.AddListener(() => buyButtonClicked(upgrade));
            }
            else
            {
                if (isActive)
                {
                    setButtonState("Remove", Color.red, true);
                    buyButton.onClick.AddListener(() => toggleUpgrade(upgrade));
                }
                else
                {
                    bool canApply = canApplyUpgrade(upgrade);
                    setButtonState(canApply ? "Apply" : "Requires Kunai", canApply ? Color.green : Color.gray, canApply);
                    if (canApply)
                        buyButton.onClick.AddListener(() => toggleUpgrade(upgrade));
                }
            }
        }
    }

    private bool canApplyUpgrade(upgradeData upgrade)
    {
        if (upgrade.upgradeType == upgradeData.UpgradeType.KunaiSpread)
        {
            return weaponManager.instance != null &&
                   weaponManager.instance.activeWeapon is gunStats gun &&
                   gun.gunType == gunStats.GunType.Kunai;
        }
        return true;
    }

    void toggleUpgrade(upgradeData upgrade)
    {
        if (activeUpgrades.Contains(upgrade.id))
            activeUpgrades.Remove(upgrade.id);
        else
            activeUpgrades.Add(upgrade.id);

        SaveUpgrades();
        displayUpgrades(upgrade);
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
        if (files >= upgrade.cost && !purchasedUpgrades.Contains(upgrade.id))
        {
            files -= upgrade.cost;
            purchasedUpgrades.Add(upgrade.id);
            SaveUpgrades();
            displayUpgrades(upgrade); // Immediately reflect the purchase status
        }
        if (audioManager.instance != null)
            audioManager.instance.playButtonClick();
    }

    [System.Serializable]
    public class upgradeSaveData
    {
        public List<string> unlocked;
        public List<string> purchased;
        public List<string> active;
        public int files;
    }

    public void SaveUpgrades()
    {
        upgradeSaveData data = new upgradeSaveData
        {
            purchased = purchasedUpgrades,
            active = activeUpgrades,
            files = files
        };

        PlayerPrefs.SetString("UnlockedUpgrades", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void LoadUpgrades()
    {
        if (!PlayerPrefs.HasKey("UnlockedUpgrades")) return;
        upgradeSaveData data = JsonUtility.FromJson<upgradeSaveData>(PlayerPrefs.GetString("UnlockedUpgrades"));
        purchasedUpgrades = data.purchased ?? new List<string>();
        activeUpgrades = data.active ?? new List<string>();
        files = data.files;
    }
}
