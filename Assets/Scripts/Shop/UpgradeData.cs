using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("cost")]
    [SerializeField] public int filesCost;


    [Tooltip("which effect this applies, gameplay code switches on it")]
    [SerializeField] public UpgradeType upgradeType;

    [Tooltip("strength with no tiers bought this run. fire rate: fire speed divisor, 1.5 is 50% faster. kunai spread: kunai per throw. exploding bullets: fraction of the full blast radius, 0 to 1")]
    [SerializeField] public float value;

    [Tooltip("added to value for each tier bought in the between-wave shop. exploding bullets should reach exactly 1 at the tier cap")]
    [SerializeField] public float valuePerTier;

    [Tooltip("icon on the shop card")]
    [SerializeField] public Sprite icon;

    [Tooltip("all of these must be complete before this can be bought, leave empty for none")]
    [SerializeField] public ChallengeData[] requiredChallenges;

    [Header("Bytes")]
    [Tooltip("price in bytes")]
    [SerializeField] public int bytesCost;
    public enum UpgradeType
    {
        FireRate,
        ExplodingBullets,
        KunaiSpread
    }
}
