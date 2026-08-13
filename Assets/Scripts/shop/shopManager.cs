using UnityEngine;
using System.Collections.Generic;

public class shopManager : MonoBehaviour
{
    [SerializeField] private populateShop[] shopSlots;
    [SerializeField] private upgradeData[] upgrades;
    [SerializeField] private List<upgradeData> unlockedUpgrades;
    public static shopManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < shopSlots.Length && i < unlockedUpgrades.Count; i++)
        {
            shopSlots[i].populateShopUI(unlockedUpgrades[i]);
                
        }
        
    }

    public void buyUpgrade(upgradeData upgrade)
    {
        if (gameManager.instance.totalBytes < upgrade.cost)
        {
            gameManager.instance.showShopWarning();
            return;
        }

        Debug.Log("Upgrade Bought: " + upgrade.upgradeName);
        gameManager.instance.totalBytes -= upgrade.cost;
        upgrade.applyUpgrade();
    }

    public void unlockUpgrade(upgradeData upgrade)
    {
        if (gameManager.instance.totalFiles < upgrade.cost)
        {
            //not enough files warning
            return;
        }
        unlockedUpgrades.Add(upgrade);
    }
    
    
}

