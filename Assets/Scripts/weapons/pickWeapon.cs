using UnityEngine;

public class pickWeapon : MonoBehaviour
{
    [SerializeField] weaponStats weapon;

    public void interact(IPickWeapon pic)
    {
        if (pic != null)
        {
            weaponManager.instance.currentWeaponFromGround = true;
            pic.equipWeapon(weapon);
        }
    }
}