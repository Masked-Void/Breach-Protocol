using UnityEngine;

public class PickWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats weapon;

    [Tooltip("Ammo left in this dropped weapon. -1 means a full magazine.")]
    public int remainingAmmo = -1;

    public void interact(IPickWeapon pic)
    {
        if (pic == null || weapon == null) return;

        weapon.isFromGround = true;
        pic.equipWeapon(weapon, remainingAmmo);
    }
}