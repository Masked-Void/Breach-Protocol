using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Upgrade")]
public class upgradeData : ScriptableObject
{
    [SerializeField] public string upgradeName;
    [SerializeField] public string id;
    [SerializeField] public string description;
    [SerializeField] public int cost;
    [SerializeField] public UpgradeType upgradeType;
    [SerializeField] public float value;
    [SerializeField] public Sprite icon;
    [SerializeField] public challengeData[] requiredChallenges;

    public enum UpgradeType
    {
        FireRate,
        ExplodingBullets,
        KunaiSpread
    }
}
