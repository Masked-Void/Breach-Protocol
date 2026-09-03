using UnityEngine;

/*
 * Script: ChallengeData
 *
 * Description:
 * A set of kill challenges attached to one weapon. Each entry has its own
 * target count, so a weapon can have a tiered set — kill 10, then 50, then 200.
 *
 * Interacts With:
 * - ChallengeManager (tracks progress against these)
 * - UpgradeData (some upgrades are gated behind a completed set)
 */


[CreateAssetMenu(fileName = "Challenge", menuName = "Weapons/Challenge")]
public class ChallengeData : ScriptableObject
{
    [Tooltip("name of the whole set, shown as the panel heading")]
    public string challengeName;

    [System.Serializable]
    public struct challengeStruct
    {
        [Tooltip("unique key used to save progress, e.g. kunai_200. never shown to the player")]
        public string challengeID;

        [Tooltip("shown on the challenge card, e.g. Kunai Collector")]
        public string displayName;

        [Tooltip("kills needed with this weapon to complete the tier")]
        public int killCount;
    }

    [Header("Info")]
    [Tooltip("the tiers in this set, usually easiest first")]
    [SerializeField] public challengeStruct[] challengesList;

    [Tooltip("shown under the heading, e.g. Kill 200 enemies with kunais you picked up")]
    public string description;

    [Header("Requirements")]
    [Tooltip("kills only count toward this set when made with this weapon")]
    public WeaponStats weapon;

    // parked: was going to require the weapon be picked up off the ground
    // rather than bought. WeaponManager has a matching commented field.
    // public bool requireGroundPickup = true;
}
