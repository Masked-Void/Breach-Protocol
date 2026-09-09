using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script: UpgradeManager
 *
 * Description:
 * Owns meta progression. Files earned during runs are spent here on upgrades
 * that persist between runs.
 *
 * Responsibilities:
 * - Hold the Files balance and the set of unlocked upgrades
 * - Save and load upgrade state
 * - Populate the upgrade UI and handle purchases
 * - Answer IsUpgradeActive queries from gameplay systems
 *
 * Interacts With:
 * - UpgradeData (the upgrade assets)
 * - GameManager, WaveManager (Files are transferred in)
 * - WeaponManager (fire rate upgrade)
 * - ChallengeManager (challenges gate some upgrades)
 * - SaveManager (owns the file all of this state lives in)
 *
 * Notes:
 * - files, purchasedUpgrades and activeUpgrades are properties reading straight
 *   through to SaveManager.Data. They are not fields, so they no longer appear
 *   in the inspector.
 * - Files accounting currently compounds. WaveManager and GameManager both add
 *   the running total rather than the amount earned, so the balance grows
 *   faster than intended.
 * - IsUpgradeActive takes a magic string. An enum would fail loudly on a typo.
 */

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

    [Header("Required Challenges UI")]
    [SerializeField] private RequiredChallengeUISlot[] requiredChallengeSlots;

    // all three read straight through to the save, so every call site in this
    // class and in ChallengeManager keeps working unchanged
    public int files
    {
        get => SaveManager.Data.files;
        set
        {
            SaveManager.Data.files = value;
            SaveManager.MarkDirty();
        }
    }

    // getters only. these mutate through Add and Remove on the save's own list,
    // so a setter would only be a way to swap that list out by accident.
    public List<string> purchasedUpgrades => SaveManager.Data.purchasedUpgradeIDs;
    public List<string> activeUpgrades => SaveManager.Data.activeUpgradeIDs;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public bool IsUpgradeActive(string id) => activeUpgrades.Contains(id);

    public bool IsUpgradeUnlocked(UpgradeData upgrade)
    {
        if (upgrade == null)
            return false;

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
        if (upgrade == null)
            return;
        if (upgradeName != null)
            upgradeName.text = upgrade.upgradeName;
        if (upgradeIcon != null)
            upgradeIcon.sprite = upgrade.icon;

        if (upgradeDescription != null)
        {
            upgradeDescription.text = upgrade.upgradeType == UpgradeData.UpgradeType.FireRate
                ? upgrade.description + $". Reduces firerate by 1/{upgrade.value}"
                : upgrade.description;
        }
        if (upgradeCost != null)
            upgradeCost.text = "" + upgrade.cost;
        if (upgradeValue != null)
            upgradeValue.text = "" + upgrade.value;
        if (fileCountText != null)
            fileCountText.text = "" + files;

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

    // kept under this name so the call sites across this class and
    // ChallengeManager need no edits
    public void SaveUpgrades()
    {
        SaveManager.Save();
    }

    [ContextMenu("Reset Saved Upgrades")]
    public void ResetUpgrades()
    {
        purchasedUpgrades.Clear();
        activeUpgrades.Clear();
        files = 0;

        SaveManager.Save();
    }

    private void displayRequiredChallenges(UpgradeData upgrade)
    {
        if (requiredChallengeSlots == null)
            return;

        int reqCount = (upgrade != null && upgrade.requiredChallenges != null) ? upgrade.requiredChallenges.Length : 0;

        for (int i = 0; i < requiredChallengeSlots.Length; i++)
        {
            if (requiredChallengeSlots[i].slotRoot == null)
                continue;

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
}
