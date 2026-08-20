using UnityEngine;

public class mainMenuShopSlot : MonoBehaviour
{
    private upgradeData upgrade;
    private mainMenuShopManager manager;

    public void SetUpgrade(upgradeData data, mainMenuShopManager shopManager)
    {
        upgrade = data;
        manager = shopManager;
    }

    public void OnButtonClick()
    {
        if (upgrade == null || manager == null)
        {
            Debug.LogWarning("Shop slot clicked before it was set up.");
            return;
        }


        manager.UnlockUpgrade(upgrade);
    }
}
