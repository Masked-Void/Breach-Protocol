using UnityEngine;

[CreateAssetMenu(fileName = "Challenge", menuName = "Weapons/Challenge")]
public class challengeData : ScriptableObject
{
    [Header("Info")]
    public string challengeID;      // unique key, e.g. "kunai_200"
    public string displayName;      // e.g. "Kunai Collector"
    public string description;      // e.g. "Kill 200 enemies with kunais you picked up"

    
    [Header("Requirements")]
    public string targetWeaponID;   // must match weaponStats.weaponID
    public int killCount;
    public bool requireGroundPickup = true;

    [Header("Reward")]
    public string rewardWeaponID;   // what the shop unlocks

}
