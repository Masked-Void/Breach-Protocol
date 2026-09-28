using UnityEngine;

// sits on a weapon lying on the ground. enemies carry one with this disabled,
// and it turns back on when the weapon is thrown or dropped on death.
public class PickWeapon : MonoBehaviour
{
    [Tooltip("which weapon this pickup gives")]
    [SerializeField] public WeaponStats weapon;

    [Tooltip("Ammo left in this dropped weapon. -1 means a full magazine.")]
    public int remainingAmmo = -1;

    public static event System.Action<PickWeapon> PickedUp;

    // hands the weapon over and flags it as ground-sourced, which some
    // challenges check
    public void Interact(IPickWeapon pic)
    {
        if (pic == null || weapon == null)
            return;

        PickedUp?.Invoke(this);

        weapon.isFromGround = true;
        pic.EquipWeapon(weapon, remainingAmmo);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void clearOldEvents()
    {
        PickedUp = null;
    }
}
