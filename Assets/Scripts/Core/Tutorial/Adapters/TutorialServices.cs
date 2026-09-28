using UnityEngine;

// hands out the four adapters and switches the game in and out of tutorial mode. the adapters are found on this same
// object the first time they're asked for, so this works no matter which component's Awake ran first
public class TutorialServices : MonoBehaviour
{
    private ITutorialStress stress;
    private ITutorialWeapon weapon;
    private ITutorialShop shop;
    private ITutorialMovement movement;

    public ITutorialStress Stress
    {
        get
        {
            if ( stress == null)
            {
                TryGetComponent(out stress);
            }

            return stress;
        }
    }

    public ITutorialWeapon Weapon
    {
        get
        {
            if (weapon == null)
            {
                TryGetComponent(out weapon);
            }

            return weapon;
        }
    }

    public ITutorialShop Shop
    {
        get
        {
            if (shop == null)
            {
                TryGetComponent(out shop);
            }

            return shop;
        }
    }

    public ITutorialMovement Movement
    {
        get
        {
            if (movement == null)
            {
                TryGetComponent(out movement);
            }

            return movement;
        }
    }

    private void Awake()
    {
        if (Stress == null || Weapon == null || Shop == null || Movement == null)
        {
            Debug.LogError("TutorialServices: all four adapters belong on this object", this);
        }
    }

    // the manager calls this when the tutorial begins
    public void EnterTutorialMode()
    {
        // max bpm restarts a space instead of ending a run that doesn't exist
        if (Stress != null)
        {
            Stress.SetTutorialMode(true);
            Stress.ResetToResting();
        }

        // the pistol on ring 2's pedestal is the player's first gun
        if (Weapon != null)
        {
            Weapon.SetArmingAllowed(false);
        }
    }

    // the manager calls this when the tutorial ends, nothing tutorial only reaches what comes after
    public void ExitTutorialMode()
    {
        if (Shop != null)
        {
            Shop.ClearTutorialState();
        }

        if (Stress!= null)
        {
            Stress.SetTutorialMode(false);
        }

        if (Weapon!= null)
        {
            Weapon.SetArmingAllowed(true);
        }

        if (Movement!= null)
        {
            Movement.SetMovementLocked(false);
        }
    }
}
