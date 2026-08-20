using UnityEngine;

[CreateAssetMenu(fileName = "Challenge", menuName = "Weapons/Challenge")]
public class challengeData : ScriptableObject
{
    [System.Serializable]
    public struct challengeStruct
    {
        public string challengeID;      // unique key, e.g. "kunai_200"
        public string displayName;      // e.g. "Kunai Collector"
        int progress;
        public int killCount;
    }

    [Header("Info")]
    [SerializeField] public challengeStruct[] challengesList;
    public string description;      // e.g. "Kill 200 enemies with kunais you picked up"

    [Header("Requirements")]
    public weaponStats weapon;
    public bool requireGroundPickup = true;
}
