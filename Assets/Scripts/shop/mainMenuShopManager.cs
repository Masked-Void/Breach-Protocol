using UnityEngine;
using TMPro;
using System.Collections;

public class mainMenuShopManager : MonoBehaviour
{
    [SerializeField] private upgradeData[] allUpgrades;
    [SerializeField] private populateMainMenuShop[] shopSlots;
    [SerializeField] private TMP_Text shopWarning;
    [SerializeField] private TMP_Text filesText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateFilesUI();
        PopulateMainMenuShop();
    }

    private void PopulateMainMenuShop()
    {
        for (int i = 0; i < allUpgrades.Length && i < shopSlots.Length; i++)
        {
            upgradeData upgrade = allUpgrades[i];
            shopSlots[i].populateMainMenuShopUI(upgrade);
            shopSlots[i].GetComponent<mainMenuShopSlot>().SetUpgrade(upgrade);
        }
    }

    public void UnlockUpgrade(upgradeData upgrade)
    {
        if (upgradeManager.instance.files < upgrade.Cost)
        {
            shopWarning.text = "Not enough files to purchase that unlock.";
            StartCoroutine(ShowShopWarning());
            return;
        }

        if (upgradeManager.instance.unlockedUpgrades.Contains(upgrade.Id))
        {
            shopWarning.text = "Upgrade already unlocked!";
            StartCoroutine(ShowShopWarning());
            return;
        }
        upgradeManager.instance.files -= upgrade.Cost;
        upgradeManager.instance.UnlockUpgrade(upgrade.Id);
        upgradeManager.instance.SaveUpgrades();
        UpdateFilesUI();
    }

    public IEnumerator ShowShopWarning()
    {
        shopWarning.gameObject.SetActive(true);
        yield return new WaitForSeconds(5f);
        shopWarning.gameObject.SetActive(false);
    }

    private void UpdateFilesUI()
    {
        filesText.text = "Files: " + upgradeManager.instance.files.ToString();
    }
}
