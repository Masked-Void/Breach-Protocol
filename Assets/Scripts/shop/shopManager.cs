using UnityEngine;
using System.Collections.Generic;

public class shopManager : MonoBehaviour
{
    // public static shopManager instance;

    // [SerializeField] private populateShop[] shopSlots;
    // [SerializeField] private upgradeData[] allUpgrades;


    private void Awake()
    {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // private void Start()
    // {
    //     PopulateShop();
    // }

    private void OnDestroy() {
        if (instance == this)
            instance = null;
    }

    private void PopulateShop()
    {
        //Debug.Log("Unlocked upgrades: " + string.Join(", ", upgradeManager.instance.unlockedUpgrades));
        var unlockedIds = upgradeManager.instance.unlockedUpgrades;

    //     int slotIndex = 0;

    //     foreach (string id in unlockedIds)
    //     {
    //         upgradeData unlockable = FindUpgradeById(id);

    //         if (unlockable != null && unlockable.equippableVersion != null)
    //         {
    //             shopSlots[slotIndex].populateShopUI(unlockable.equippableVersion);
    //             slotIndex++;
    //         }
    //     }
    // }

    private upgradeData FindUpgradeById(string id)
    {
        foreach (var upgrade in allUpgrades)
        {
            //Debug.Log("In-game upgrade available: " + upgrade.Id);
            if (upgrade.Id == id)
            {
                return upgrade;
            }
        }
        return null;
    }

    // public void buyUpgrade(upgradeData upgrade)
    // {
    //     if (gameManager.instance.totalBytes < upgrade.Cost)
    //     {
    //         gameManager.instance.showShopWarning();
    //         return;
    //     }

        //Debug.Log("Upgrade Bought: " + upgrade.UpgradeName);
        gameManager.instance.totalBytes -= upgrade.Cost;
        //upgradeManager.instance.PurchaseUpgrade(upgrade.Id);
        upgrade.applyUpgrade();
    }

    

    // public populateShop[] getShopSlots()
    // {
    //     return shopSlots;
        
    // }
    
    
}

