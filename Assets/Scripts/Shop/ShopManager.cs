using System.Collections.Generic;
using UnityEngine;

/*
 * Script: ShopManager
 *
 * Description:
 * In-run shop. Currently disabled — the buy logic below is commented out
 * pending a decision on whether the shop ships. The component is still
 * attached to live scenes, so it loads and does nothing.
 *
 * Interacts With:
 * - UpgradeManager, UpgradeData, GameManager (bytes)
 *
 * Notes:
 * - Do not delete the commented block without deciding the shop's fate first.
 */


public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    private bool isOpen = false;

    // true while the pause menu is covering an open shop, so resuming knows to
    // bring the shop back rather than hand control to the player mid-wave
    private bool isSuspended = false;
    private List<UpgradeData> offeredUpgrades = new();

    [Tooltip("key that closes the shop and starts the next wave")]
    [SerializeField] private KeyCode closeKey = KeyCode.Q;
    [Header("Cards")]
    [Tooltip("the card objects inside shopUI, one upgrade per card. spare cards are hidden when fewer upgrades are offered")]
    [SerializeField] private ShopPopulator[] shopSlots;
    [Header("Economy")]
    [Tooltip("same EconomyConfig asset WaveManager uses. supplies the tier cap and the cost multiplier")]
    [SerializeField] private EconomyConfig economy;
    public bool IsOpen => isOpen;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
    private void OnEnable()
    {
        WaveManager.WaveCleared += handleWaveCleared;
    }

    private void OnDisable()
    {
        WaveManager.WaveCleared -= handleWaveCleared;
    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isOpen || isSuspended)
        {
            return;
        }

        if (Input.GetKeyDown(closeKey))
        {
            CloseShop();
        }
    }

    // hides the shop for the pause menu without ending it or unfreezing the game
    public void Suspend()
    {
        if (!isOpen || isSuspended)
        {
            return;
        }

        isSuspended = true;

        if (GameManager.instance.shopUI != null)
        {
            GameManager.instance.shopUI.SetActive(false);
        }
    }

    // brings the shop back after the pause menu closes
    public void Resume()
    {
        if (!isOpen || !isSuspended)
        {
            return;
        }

        isSuspended = false;

        if (GameManager.instance.shopUI != null)
        {
            GameManager.instance.shopUI.SetActive(true);
        }
    }

    // spends Bytes on the next tier of an upgrade. returns false without
    // spending anything if the shop is closed, the upgrade is maxed, or the
    // player cannot afford it.
    public bool TryBuy(UpgradeData upgrade)
    {
        if (upgrade == null)
        {
            return false;
        }

        if (!isOpen || isSuspended)
        {
            return false;
        }

        if (UpgradeManager.instance == null || GameManager.instance == null || economy == null)
        {
            Debug.LogError("ShopManager: missing UpgradeManager, GameManager or EconomyConfig, purchase skipped", this);
            return false;
        }

        if (isMaxed(upgrade))
        {
            Debug.Log("ShopManager: " + upgrade.upgradeName + " is maxed", this);
            return false;
        }

        // computed once so the check and the charge can never disagree
        int price = getNextPrice(upgrade);

        if (GameManager.instance.totalBytes < price)
        {
            GameManager.instance.ShowShopWarning();
            return false;
        }

        GameManager.instance.SubtractBytes(price);
        UpgradeManager.instance.AddUpgradeTier(upgrade.id);

        Debug.Log("ShopManager bought: " + upgrade.upgradeName + " for " + price + ", now tier " + UpgradeManager.instance.GetUpgradeTier(upgrade.id), this);

        refreshCards();
        return true;
    }

    public void CloseShop()
    {
        if (isOpen == false)
        {
            return;
        }

        if (GameManager.instance.shopUI != null)
        {
            GameManager.instance.shopUI.SetActive(false);
        }

        isOpen = false;
        isSuspended = false;
        GameManager.instance.UnfreezeGame();
    }


    private void handleWaveCleared()
    {
        if (isOpen == true)
            return;
        GameManager.instance.FreezeGame();
        isOpen = true;
        buildOffer();
        refreshCards();
        if (GameManager.instance.shopUI != null)
            GameManager.instance.shopUI.SetActive(true);
    }

    // picks which upgrades this wave's shop offers. stub for now, so the freeze and
    // close timing can be tested against an empty panel first.
    private void buildOffer()
    {
        offeredUpgrades.Clear();
        if (UpgradeManager.instance == null)
        {
            return;
        }
        foreach (UpgradeData upgrade in UpgradeManager.instance.upgrades)
        {
            if (upgrade == null)
            {
                continue;
            }
            if (UpgradeManager.instance.IsUpgradeActive(upgrade.id))
            {
                offeredUpgrades.Add(upgrade);
                Debug.Log(upgrade.upgradeName);
            }
        }
    }
    // shows one offered upgrade per card, wires each card's buy button, and
    // hides the spares. called when the shop opens and again after every
    // purchase so prices stay current.
    private void refreshCards()
    {
        if (shopSlots == null)
        {
            return;
        }

        bool canPrice = UpgradeManager.instance != null && economy != null;

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null)
            {
                continue;
            }

            if (i < offeredUpgrades.Count)
            {
                // declared inside the loop so each button's lambda keeps its
                // own upgrade instead of reading i after the loop has finished
                UpgradeData upgrade = offeredUpgrades[i];

                int price = canPrice ? getNextPrice(upgrade) : upgrade.bytesCost;
                bool maxed = canPrice && isMaxed(upgrade);

                shopSlots[i].gameObject.SetActive(true);
                shopSlots[i].Populate(upgrade, price, maxed);

                if (shopSlots[i].BuyButton != null)
                {
                    shopSlots[i].BuyButton.onClick.RemoveAllListeners();
                    shopSlots[i].BuyButton.onClick.AddListener(() => TryBuy(upgrade));
                }
            }
            else
            {
                shopSlots[i].gameObject.SetActive(false);
            }
        }
    }
    // price of the next tier. the tier read here is how many the player already
    // owns this run, so tier 0 costs the base price, tier 1 double, tier 2 four times.
    private int getNextPrice(UpgradeData upgrade)
    {
        int currentTier = UpgradeManager.instance.GetUpgradeTier(upgrade.id);
        return Mathf.RoundToInt(upgrade.bytesCost * Mathf.Pow(economy.multiplier, currentTier));
    }

    // true once this upgrade has reached the tier cap for this run
    private bool isMaxed(UpgradeData upgrade)
    {
        return UpgradeManager.instance.GetUpgradeTier(upgrade.id) >= economy.tierCap;
    }
    // [SerializeField] private ShopPopulator[] shopSlots;
    // [SerializeField] private UpgradeData[] allUpgrades;


    // private void Awake()
    // {
    //     instance = this;
    // }

    // private void Start()
    // {
    //     PopulateShop();
    // }

    // private void PopulateShop()
    // {
    //     Debug.Log("Unlocked upgrades: " + string.Join(", ", UpgradeManager.instance.unlockedUpgrades));
    //     var unlockedIds = UpgradeManager.instance.unlockedUpgrades;

    //     int slotIndex = 0;

    //     foreach (string id in unlockedIds)
    //     {
    //         UpgradeData unlockable = FindUpgradeById(id);

    //         if (unlockable != null && unlockable.equippableVersion != null)
    //         {
    //             shopSlots[slotIndex].populateShopUI(unlockable.equippableVersion);
    //             slotIndex++;
    //         }
    //     }
    // }

    // private UpgradeData FindUpgradeById(string id)
    // {
    //     foreach (var upgrade in allUpgrades)
    //     {
    //         Debug.Log("In-game upgrade available: " + upgrade.Id);
    //         if (upgrade.Id == id)
    //         {
    //             return upgrade;
    //         }
    //     }
    //     return null;
    // }

    // public void buyUpgrade(UpgradeData upgrade)
    // {
    //     if (GameManager.instance.totalBytes < upgrade.Cost)
    //     {
    //         GameManager.instance.showShopWarning();
    //         return;
    //     }

    //     Debug.Log("Upgrade Bought: " + upgrade.UpgradeName);
    //     GameManager.instance.totalBytes -= upgrade.Cost;
    //     //UpgradeManager.instance.PurchaseUpgrade(upgrade.Id);
    //     upgrade.applyUpgrade();
    // }



    // public ShopPopulator[] getShopSlots()
    // {
    //     return shopSlots;

    // }


}

