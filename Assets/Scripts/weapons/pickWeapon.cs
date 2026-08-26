using UnityEngine;

public class pickWeapon : MonoBehaviour
{
    [SerializeField] public weaponStats weapon;

    [Tooltip("Ammo left in this dropped weapon. -1 means a full magazine.")]
    public int remainingAmmo = -1;

    public void interact(IPickWeapon pic)
    {
        if (pic == null || weapon == null) return;

        weapon.isFromGround = true;
        pic.equipWeapon(weapon, remainingAmmo);
    }
}