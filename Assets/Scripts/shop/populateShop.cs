using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class populateShop : MonoBehaviour
{
    
    
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;
    private populateShop instance;




    private upgradeData currentUpgrade;

    public void populateShopUI(upgradeData upgrade)
    {
        currentUpgrade = upgrade;
        
        icon.sprite = upgrade.icon;
        nameText.text = upgrade.upgradeName;
        descriptionText.text = upgrade.description;
        costText.text = "Bytes: " + upgrade.cost.ToString();

        
    }

    public void BuyUpgrade()
    {
        shopManager.instance.buyUpgrade(currentUpgrade);
    }

   
}
