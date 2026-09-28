using System;
using UnityEngine;

// what the tutorial needs from the weapon, throw and pickup code
public interface ITutorialWeapon
{
    // rounds left in the held gun
    int CurrentAmmo { get; }

    // true if the player is holding a gun
    bool IsHoldingWeapon { get; }

    // raised when the trigger is pulled on an empty gun
    event Action DryFired;

    // raised when a gun leaves the hand, passes the thrown model
    event Action<GameObject> Thrown;

    // raised when a pickup is taken, passes the pickup
    event Action<GameObject> PickedUp;

    // takes the held gun away without dropping anything
    void Disarm();

    // while false, any gun the player gets is taken away again, the tutorial starts unarmed
    void SetArmingAllowed(bool isAllowed);
}
