using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class populateMainMenuShop : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;

    [Header("Locked state (all optional)")]
    [SerializeField] Button buyButton;
    [SerializeField] GameObject lockedOverlay;
    [SerializeField] Color lockedTint = new Color(1f, 1f, 1f, 0.35f);

    private upgradeData currentUpgrade;

    public void populateMainMenuShopUI(upgradeData upgrade, bool owned, bool challengesDone, int challengesComplete)
    {
        if (upgrade == null) return;

        currentUpgrade = upgrade;

        if (icon != null) icon.sprite = upgrade.Icon;
        if (nameText != null) nameText.text = upgrade.UpgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.Description;

        int total = upgrade.RequiredChallenges != null ? upgrade.RequiredChallenges.Length : 0;

        if (costText != null)
        {
            if (owned)
                costText.text = "Owned";
            else if (!challengesDone)
                costText.text = $"Locked — {challengesComplete}/{total} challenges";
            else
                costText.text = "Files: " + upgrade.Cost;
        }

        bool canBuy = !owned && challengesDone;

        if (buyButton != null) buyButton.interactable = canBuy;
        if (lockedOverlay != null) lockedOverlay.SetActive(!owned && !challengesDone);
        if (icon != null) icon.color = (owned || challengesDone) ? Color.white : lockedTint;
    }
}
