using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class populateMainMenuShop : MonoBehaviour
{

    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;

    private upgradeData currentUpgrade;

    public void populateMainMenuShopUI(upgradeData Upgrade)
    {
        currentUpgrade = Upgrade;

        icon.sprite = Upgrade.icon;
        nameText.text = Upgrade.UpgradeName;
        descriptionText.text = Upgrade.Description;
        costText.text = "Files: " + Upgrade.Cost.ToString();
    }
}
