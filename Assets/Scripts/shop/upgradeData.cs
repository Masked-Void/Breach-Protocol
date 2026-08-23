using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Upgrade")]
public class upgradeData : ScriptableObject
{
    [SerializeField] private string upgradeName;
    [SerializeField] private string id;
    [SerializeField] private string description;
    [SerializeField] private int cost;
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private float value;
    [SerializeField] public Sprite icon;

    [Header("Unlock Requirements")]
    [Tooltip("Leave EMPTY for anything available from the start (pistol, normal upgrades). " +
             "Drag in all three challenge assets to gate a weapon behind them.")]
    [SerializeField] private challengeData[] requiredChallenges;

    public string UpgradeName => upgradeName;
    public string Id => id;
    public string Description => description;
    public int Cost => cost;
    public float Value => value;
    public Sprite Icon => icon;
    public challengeData[] RequiredChallenges => requiredChallenges;

    public upgradeData equippableVersion;

    public enum UpgradeType
    {
        FireRate,
        ExplodingBullets,
        KunaiSpread
    }

    public void applyUpgrade()
    {
        switch (upgradeType)
        {
            case UpgradeType.FireRate:
                weaponManager.instance.activeWeapon.attackRate /= value;
                break;

            // case UpgradeType.ExplodingBullets:
            //     FindAnyObjectByType<playerController>().explodingBullets = true;
            //     break;

            // case UpgradeType.KunaiSpread:
            //     playerController player = FindAnyObjectByType<playerController>();
            //     player.kunaiSpread = true;
            //     break;
        }
    }
}
