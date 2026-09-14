using System.Collections.Generic;
using NUnit.Framework;
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

    public bool TryBuy(UpgradeData upgrade)
    {
        return false;
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
        if (GameManager.instance.shopUI != null)
            GameManager.instance.shopUI.SetActive(true);
    }

    // picks which upgrades this wave's shop offers. stub for now, so the freeze and
    // close timing can be tested against an empty panel first.
    private void buildOffer()
    {
        offeredUpgrades.Clear();
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

