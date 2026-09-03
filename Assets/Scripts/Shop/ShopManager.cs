using UnityEngine;
using System.Collections.Generic;

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
    // public static ShopManager instance;

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

