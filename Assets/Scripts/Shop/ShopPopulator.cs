using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script: ShopPopulator
 *
 * Description:
 * One card in the between-wave shop. Shows a single upgrade's name, icon,
 * description and Bytes price. ShopManager fills it each time the shop opens.
 *
 * Interacts With:
 * - ShopManager (calls Populate, and later owns the purchase)
 * - UpgradeData (the upgrade shown on this card)
 *
 * Notes:
 * - The buy button is not wired yet. It will be hooked up from code with
 *   AddListener rather than in the inspector, so a rename can't silently
 *   break it.
 */
public class ShopPopulator : MonoBehaviour
{
    [Header("Card UI")]
    [Tooltip("upgrade icon at the top of the card")]
    [SerializeField] private Image icon;

    [Tooltip("upgrade name, e.g. Fire Rate Up")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("one line explaining what the upgrade does")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("price in Bytes for the next tier")]
    [SerializeField] private TMP_Text costText;

    [Tooltip("buy button on this card, wired from code, leave its OnClick list empty")]
    [SerializeField] private Button buyButton;

    // the upgrade this card is currently showing, set by Populate
    private UpgradeData currentUpgrade;

    public UpgradeData CurrentUpgrade => currentUpgrade;
    public Button BuyButton => buyButton;

    // fills the card with one upgrade. called by ShopManager every time the
    // shop opens, so it overwrites whatever the card showed last wave.
    public void Populate(UpgradeData upgrade)
    {
        currentUpgrade = upgrade;

        if (upgrade == null)
        {
            return;
        }

        if (icon != null)
        {
            icon.sprite = upgrade.icon;
        }

        if (nameText != null)
        {
            nameText.text = upgrade.upgradeName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = upgrade.description;
        }

        if (costText != null)
        {
            costText.text = "Bytes: " + upgrade.bytesCost;
        }
    }
}
