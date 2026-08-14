using UnityEngine;

public class mainMenuShopSlot : MonoBehaviour
{
    private upgradeData upgrade;

    public void SetUpgrade(upgradeData data)
    {
        upgrade = data;
    }

    public void OnButtonClick()
    {
        upgradeManager.instance.UnlockUpgrade(upgrade.Id);

    }
}
