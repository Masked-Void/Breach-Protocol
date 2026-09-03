using UnityEngine;

/*
 * Script: UpgradeData
 *
 * Description:
 * One meta upgrade, bought with Files between runs. The upgradeType tells
 * gameplay code which effect to apply and value is how much.
 *
 * Interacts With:
 * - UpgradeManager (owns the list, handles purchases)
 * - ChallengeData (some upgrades are gated behind challenges)
 */
[CreateAssetMenu(menuName = "Shop/Upgrade")]
public class UpgradeData : ScriptableObject
{
    [Tooltip("shown on the shop card")]
    [SerializeField] public string upgradeName;

    [Tooltip("string key gameplay code checks with IsUpgradeActive, must be unique")]
    [SerializeField] public string id;

    [Tooltip("shown under the name on the shop card")]
    [SerializeField] public string description;

    [Tooltip("price in Files")]
    [SerializeField] public int cost;

    [Tooltip("which effect this applies, gameplay code switches on it")]
    [SerializeField] public UpgradeType upgradeType;

    [Tooltip("how much the effect applies, meaning depends on upgradeType")]
    [SerializeField] public float value;

    [Tooltip("icon on the shop card")]
    [SerializeField] public Sprite icon;

    [Tooltip("all of these must be complete before this can be bought, leave empty for none")]
    [SerializeField] public ChallengeData[] requiredChallenges;

    public enum UpgradeType
    {
        FireRate,
        ExplodingBullets,
        KunaiSpread
    }
}