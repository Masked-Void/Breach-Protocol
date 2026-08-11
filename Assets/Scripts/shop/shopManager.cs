using UnityEngine;

public class shopManager : MonoBehaviour
{
    [SerializeField] private populateShop[] shopSlots;
    [SerializeField] private upgradeData[] upgrades;
    public static shopManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < shopSlots.Length && i < upgrades.Length; i++)
        {
            shopSlots[i].populateShopUI(upgrades[i]);
                
        }
        
    }

    public void buyUpgrade(upgradeData upgrade)
    {
        Debug.Log("Upgrade Bought: " + upgrade.upgradeName);
        upgrade.applyUpgrade();
    }
}

