using UnityEngine;

[CreateAssetMenu(fileName = "Challenge", menuName = "Weapons/Challenge")]
public class ChallengeData : ScriptableObject
{
    public string challengeName;
    [System.Serializable]
    public struct challengeStruct
    {
        public string challengeID;      // unique key, e.g. "kunai_200"
        public string displayName;      // e.g. "Kunai Collector"
        public int killCount;
    }

    [Header("Info")]
    [SerializeField] public challengeStruct[] challengesList;
    public string description;      // e.g. "Kill 200 enemies with kunais you picked up"
    [Header("Requirements")]
    public WeaponStats weapon;
    // public bool requireGroundPickup = true;
}
