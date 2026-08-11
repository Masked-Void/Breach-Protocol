using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Upgrade")]
public class upgradeData : ScriptableObject
{
    [SerializeField] public string upgradeName;
    [SerializeField] public string description;
    [SerializeField] public int cost;
    [SerializeField] public UpgradeType upgradeType;
    
    
    public float value;

    [SerializeField] public Sprite icon;
    
    
    







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

            case UpgradeType.ExplodingBullets:
                
                FindAnyObjectByType<playerController>().explodingBullets = true;
                break;

            case UpgradeType.KunaiSpread:
                
                playerController player = FindAnyObjectByType<playerController>();
                player.kunaiSpread = true;
              
                break;
        }
    }
   
    

}
