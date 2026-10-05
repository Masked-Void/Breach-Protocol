using System;
using UnityEngine;

// connects the tutorial to WeaponManager and PickWeapon, which live in Bootstrap and on pickups. listens to their
// static events and passes them on, and takes a gun away again while arming isn't allowed
public class WeaponAdapter : MonoBehaviour, ITutorialWeapon
{
    private bool isArmingAllowed = true;

    public event Action DryFired;

    public event Action<GameObject> Thrown;
    public event Action<GameObject> PickedUp;

    public int CurrentAmmo => WeaponManager.instance != null ? WeaponManager.instance.CurrentAmmo : 0;

    public bool IsHoldingWeapon => WeaponManager.instance != null && WeaponManager.instance.activeWeapon != null;

    private void OnEnable()
    {
        WeaponManager.DryFired += handleDryFired;
        WeaponManager.Thrown += handleThrown;
        WeaponManager.Equipped += handleEquipped;
        PickWeapon.PickedUp += handlePickedUp;
    }

    private void OnDisable()
    {
        WeaponManager.DryFired -= handleDryFired;
        WeaponManager.Thrown -= handleThrown;
        WeaponManager.Equipped -= handleEquipped;
        PickWeapon.PickedUp -= handlePickedUp;
    }

    public void Disarm()
    {
        if (WeaponManager.instance != null)
        {
            WeaponManager.instance.Unequip();
        }
    }

    public void SetArmingAllowed(bool isAllowed)
    {
        isArmingAllowed= isAllowed;

        if (!isAllowed)
        {
            Disarm();
        }
    }

    private void handleDryFired()
    {
        DryFired?.Invoke();
    }

    private void handleThrown(GameObject thrownModel)
    {
        Thrown?.Invoke(thrownModel);
    }

    private void handleEquipped(WeaponStats weapon)
    {
        // WeaponManager equips after a one second delay, so a gun on its way in is caught here
        if (!isArmingAllowed)
        {
            Disarm();
        }
    }

    private void handlePickedUp(PickWeapon pickup)
    {
        PickedUp?.Invoke(pickup.gameObject);
    }
}
