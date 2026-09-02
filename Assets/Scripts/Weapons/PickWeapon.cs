using UnityEngine;

public class PickWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats weapon;

    [Tooltip("Ammo left in this dropped weapon. -1 means a full magazine.")]
    public int remainingAmmo = -1;

    public void Interact(IPickWeapon pic)
    {
        if (pic == null || weapon == null) return;

        weapon.isFromGround = true;
        pic.EquipWeapon(weapon, remainingAmmo);
    }
}