using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;
    [System.Serializable]
    public struct RequiredChallengeUISlot
    {
        public GameObject slotRoot;
        public TextMeshProUGUI challengeName;
        public GameObject checkmark;
    }

    [Header("Upgrade Info")]
    public UpgradeData[] upgrades;
    public TextMeshProUGUI upgradeName;
    public Image upgradeIcon;
    public TextMeshProUGUI upgradeDescription;
    public TextMeshProUGUI upgradeCost;
    public TextMeshProUGUI upgradeValue;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    
    [Header("Currency")]
    public TextMeshProUGUI fileCountText;
    public int files;

    [Header("Required Challenges UI")]
    [SerializeField] private RequiredChallengeUISlot[] requiredChallengeSlots;

    [HideInInspector]
    public List<string> purchasedUpgrades = new List<string>();
    public List<string> activeUpgrades = new List<string>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        LoadUpgrades();
    }

    public bool IsUpgradeActive(string id) => activeUpgrades.Contains(id);

    public bool IsUpgradeUnlocked(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        // If no challenges are required, unlock automatically
        if (upgrade.requiredChallenges == null || upgrade.requiredChallenges.Length == 0)
            return true;

        if (ChallengeManager.instance == null)
            return false;

        // Verify all required challenge groups are completed
        foreach (var reqChallenge in upgrade.requiredChallenges)
        {
            if (reqChallenge != null && !ChallengeManager.instance.AreAllChallengesComplete(reqChallenge))
            {
                return false;
            }
        }

        return true;
    }

    // Display Upgrade Info i.e Name, cost etc
    public void DisplayUpgrades(UpgradeData upgrade)
    {
        if (upgrade == null) return;
        if (upgradeName != null) upgradeName.text = upgrade.upgradeName;
        if (upgradeIcon != null) upgradeIcon.sprite = upgrade.icon;

        if (upgradeDescription != null)
        {
            upgradeDescription.text = upgrade.upgradeType == UpgradeData.UpgradeType.FireRate
                ? upgrade.description + $". Reduces firerate by 1/{upgrade.value}"
                : upgrade.description;
        }
        if (upgradeCost != null) upgradeCost.text = "" + upgrade.cost;
        if (upgradeValue != null) upgradeValue.text = "" + upgrade.value;
        if (fileCountText != null) fileCountText.text = "" + files;

        displayRequiredChallenges(upgrade);

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

    private void displayRequiredChallenges(UpgradeData upgrade)
    {
        if (requiredChallengeSlots == null) return;

        int reqCount = (upgrade != null && upgrade.requiredChallenges != null) ? upgrade.requiredChallenges.Length : 0;

        for (int i = 0; i < requiredChallengeSlots.Length; i++)
        {
            if (requiredChallengeSlots[i].slotRoot == null) continue;

            if (i < reqCount)
            {
                var reqData = upgrade.requiredChallenges[i];
                requiredChallengeSlots[i].slotRoot.SetActive(true);

                if (requiredChallengeSlots[i].challengeName != null && reqData != null)
                    requiredChallengeSlots[i].challengeName.text = reqData.challengeName;

                bool isCompleted = reqData != null && ChallengeManager.instance != null && ChallengeManager.instance.AreAllChallengesComplete(reqData);

                if (requiredChallengeSlots[i].checkmark != null)
                    requiredChallengeSlots[i].checkmark.SetActive(isCompleted);
            }
            else
            {
                // Hide slot if this upgrade requires fewer challenges
                requiredChallengeSlots[i].slotRoot.SetActive(false);
            }
        }
    }

    private bool canApplyUpgrade(UpgradeData upgrade)
    {
        if (upgrade.upgradeType == UpgradeData.UpgradeType.KunaiSpread)
        {
            return WeaponManager.instance != null &&
                   WeaponManager.instance.activeWeapon is GunStats gun &&
                   gun.gunType == GunStats.GunType.Kunai;
        }
        return true;
    }

    void toggleUpgrade(UpgradeData upgrade)
    {
        if (activeUpgrades.Contains(upgrade.id))
            activeUpgrades.Remove(upgrade.id);
        else
            activeUpgrades.Add(upgrade.id);

        SaveUpgrades();
        DisplayUpgrades(upgrade);
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
    void buyButtonClicked(UpgradeData upgrade)
    {
        // Check if player can afford upgrade
        if (files >= upgrade.cost && !purchasedUpgrades.Contains(upgrade.id))
        {
            files -= upgrade.cost;
            purchasedUpgrades.Add(upgrade.id);
            SaveUpgrades();
            DisplayUpgrades(upgrade); // Immediately reflect the purchase status
        }
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();
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

    [ContextMenu("Reset Saved Upgrades")]
    public void ResetUpgrades()
    {
        PlayerPrefs.DeleteKey("UnlockedUpgrades");
        purchasedUpgrades.Clear();
        activeUpgrades.Clear();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
