using UnityEngine;

// turns a weapon model into a held prop or a pickup, flipping the same switches WeaponManager and EnemyBase do.
// the weapon ring's pistol and the gun behind the panel both come through here
public static class TutorialPickups
{
    // a pickup at a point, -1 rounds means a full gun
    public static GameObject Spawn(WeaponStats weapon, Transform point, int remainingAmmo)
    {
        if (weapon== null || weapon.weaponModel == null || point == null)
        {
            Debug.LogWarning("TutorialPickups: missing the weapon or the spawn point");
            return null;
        }

        GameObject model = Object.Instantiate(weapon.weaponModel,point.position,point.rotation);
        SetPickup(model,weapon,remainingAmmo,true);
        return model;
    }

    // switches a model between held and waiting on the ground to be taken
    public static void SetPickup(GameObject model,WeaponStats weapon, int remainingAmmo, bool isPickup)
    {
        if (model == null)
        {
            return;
        }

        if (model.TryGetComponent(out PickWeapon picker))
        {
            picker.weapon = weapon;
            picker.remainingAmmo = remainingAmmo;
            picker.enabled = isPickup;
        }
        else if (isPickup)
        {
            Debug.LogWarning("TutorialPickups: the model has no PicKWeapon, so it can't be picked up", model);
        }

        // wall avoidance and throw damage belong to a gun in flight or in the player's hand
        if (model.TryGetComponent(out WeaponWallAvoidance wallAvoidance))
        {
            wallAvoidance.enabled = false;
        }

        if (model.TryGetComponent(out Damage thrownDamage))
        {
            thrownDamage.enabled = false;
        }

        // a held gun shouldn't catch bullets meant for whoever holds it
        if (model.TryGetComponent(out Collider pickupCollider))
        {
            pickupCollider.enabled = isPickup;
        }

        // kinematic either way, a held gun follows the hand and a pickup stays where it was put
        if (model.TryGetComponent(out Rigidbody body))
        {
            body.isKinematic = true;
        }
    }
}
