using UnityEngine;
using TMPro;
using System.Collections;

public class mainMenuShopManager : MonoBehaviour
{
    [SerializeField] private upgradeData[] allUpgrades;
    [SerializeField] private populateMainMenuShop[] shopSlots;
    [SerializeField] private TMP_Text shopWarning;
    [SerializeField] private TMP_Text filesText;

    private Coroutine warningRoutine;

    void Start()
    {
        if (shopWarning != null) shopWarning.gameObject.SetActive(false);

        UpdateFilesUI();
        RefreshShop();
    }


    public void RefreshShop()
    {
        if (allUpgrades == null || shopSlots == null) return;

        for (int i = 0; i < allUpgrades.Length && i < shopSlots.Length; i++)
        {
            upgradeData upgrade = allUpgrades[i];
            if (upgrade == null || shopSlots[i] == null) continue;

            bool owned = upgradeManager.instance.unlockedUpgrades.Contains(upgrade.Id);
            bool challengesDone = ChallengesDone(upgrade);
            int completedCount = CompletedChallengeCount(upgrade);

            shopSlots[i].populateMainMenuShopUI(upgrade, owned, challengesDone, completedCount);

            var slot = shopSlots[i].GetComponent<mainMenuShopSlot>();
            if (slot != null)
                slot.SetUpgrade(upgrade, this);
            else
                Debug.LogError($"Shop slot {i} is missing a mainMenuShopSlot component.");
        }
    }


    public void UnlockUpgrade(upgradeData upgrade)
    {
        if (upgrade == null) return;

        if (upgradeManager.instance.unlockedUpgrades.Contains(upgrade.Id))
        {
            Warn("Upgrade already unlocked!");
            return;
        }

        if (!ChallengesDone(upgrade))
        {
            int done = CompletedChallengeCount(upgrade);
            int total = upgrade.RequiredChallenges.Length;
            Warn($"Complete this weapon's challenges first ({done}/{total}).");
            return;
        }

        if (upgradeManager.instance.files < upgrade.Cost)
        {
            Warn("Not enough files to purchase that unlock.");
            return;
        }

        upgradeManager.instance.files -= upgrade.Cost;
        upgradeManager.instance.UnlockUpgrade(upgrade.Id);
        upgradeManager.instance.SaveUpgrades();

        UpdateFilesUI();
        RefreshShop();
    }

    public bool ChallengesDone(upgradeData upgrade)
    {
        var required = upgrade.RequiredChallenges;
        if (required == null || required.Length == 0) return true;   // pistol + normal upgrades

        if (challengeManager.instance == null)
        {
            Debug.LogError("No challengeManager in this scene — gated upgrades will stay locked.");
            return false;
        }

        return challengeManager.instance.AreAllComplete(required);
    }

    private int CompletedChallengeCount(upgradeData upgrade)
    {
        if (challengeManager.instance == null) return 0;
        return challengeManager.instance.CountComplete(upgrade.RequiredChallenges);
    }

    private void Warn(string message)
    {
        if (shopWarning == null) return;

        shopWarning.text = message;

        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(ShowShopWarning());
    }

    private IEnumerator ShowShopWarning()
    {
        shopWarning.gameObject.SetActive(true);
        yield return new WaitForSeconds(5f);
        shopWarning.gameObject.SetActive(false);
        warningRoutine = null;
    }

    private void UpdateFilesUI()
    {
        if (filesText != null)
            filesText.text = "Files: " + upgradeManager.instance.files;
    }
}
