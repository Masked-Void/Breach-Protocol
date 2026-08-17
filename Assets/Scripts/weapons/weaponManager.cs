using UnityEngine;

public class weaponManager : MonoBehaviour, IAmmoRefundReceiver
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    public GameObject spawnedWeaponModel;

    [Header("Inventory")]
    public weaponStats[] weapons = new weaponStats[4];

    private Transform gunBarrel;
    private float attackTimer;

    [Header("Challenge")]
    public bool currentWeaponFromGround = false;

    private int activeSlot = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }


    private void Update()
    {
        // Weapon fire rate should remain based on real time.
        attackTimer += Time.unscaledDeltaTime;
    }

    public void equipWeapon(weaponStats newWeapon)
    {
        if (newWeapon == null)
            return;

        // Don't add the exact same weapon reference twice.
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == newWeapon)
                return;
        }

        // Find first open inventory slot.
        int slot = -1;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                slot = i;
                break;
            }
        }

        // Inventory full.
        if (slot == -1)
            return;

        weapons[slot] = newWeapon;
    }

    public Transform getBarrel()
    {
        return gunBarrel;
    }

    public void showActiveweapon(Transform weaponHolder)
    {
        if (weaponHolder == null)
            return;

        activeWeapon = weapons[activeSlot];

        if (activeWeapon == null)
            return;

        // Destroy an existing held model before spawning another.
        if (spawnedWeaponModel != null)
        {
            Destroy(spawnedWeaponModel);
        }

        spawnedWeaponModel = Instantiate(
            activeWeapon.weaponModel,
            weaponHolder,
            false
        );

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;


        // Held weapon should not use physics.
        if (spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true;
        }


        // Enable the existing clip component if the weapon uses one.
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip weaponClip))
        {
            weaponClip.enabled = true;
        }


        // Guns look for "Muzzle".
        // Non-guns look for "HitPoint".
        string targetName =
            activeWeapon is gunStats
                ? "Muzzle"
                : "HitPoint";

        gunBarrel = FindDeepChild(
            spawnedWeaponModel.transform,
            targetName
        );


        if (gunBarrel == null)
        {
            Debug.LogWarning(
                "weaponManager: Could not find " +
                targetName +
                " on " +
                activeWeapon.name
            );
        }
    }

    public void Throw()
    {
        if (spawnedWeaponModel == null)
            return;


        spawnedWeaponModel.transform.SetParent(null);

        Rigidbody projectileRb;

        if (!spawnedWeaponModel.TryGetComponent(
            out projectileRb))
        {
            projectileRb =
                spawnedWeaponModel.AddComponent<Rigidbody>();
        }

        projectileRb.isKinematic = false;
        projectileRb.useGravity = true;

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning(
                "weaponManager: No Main Camera found."
            );

            return;
        }


        Vector3 forceDirection =
            mainCamera.transform.forward;


        if (gameManager.instance != null &&
            gameManager.instance.playerScript != null &&
            gameManager.instance.playerScript.weaponHoldPos != null)
        {
            Transform holdTransform =
                gameManager.instance
                    .playerScript
                    .weaponHoldPos
                    .transform;


            if (Physics.Raycast(
                holdTransform.position,
                holdTransform.forward,
                out RaycastHit hit,
                500f))
            {
                forceDirection =
                    (
                        hit.point -
                        holdTransform.position
                    ).normalized;
            }
        }

        if (gameManager.instance != null &&
            gameManager.instance.playerScript != null &&
            gameManager.instance.player != null)
        {
            Vector3 forceToAdd =
                forceDirection *
                gameManager.instance
                    .playerScript
                    .throwForce
                +
                gameManager.instance
                    .player
                    .transform
                    .up *
                gameManager.instance
                    .playerScript
                    .throwUpwardForce;


            projectileRb.AddForce(
                forceToAdd,
                ForceMode.Impulse
            );
        }


        // Add some rotational motion.
        projectileRb.AddTorque(
            mainCamera.transform.right * 10f,
            ForceMode.Impulse
        );

        if (spawnedWeaponModel.TryGetComponent<Collider>(
            out Collider weaponCollider))
        {
            weaponCollider.enabled = true;
        }

        weapons[activeSlot] = null;

        activeWeapon = null;
        spawnedWeaponModel = null;
        gunBarrel = null;
    }

    public Transform FindDeepChild(
        Transform parent,
        string targetName)
    {
        if (parent == null)
            return null;


        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }


            Transform result =
                FindDeepChild(
                    child,
                    targetName
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }

    /// <summary>
    /// Packet Leech calls this whenever a valid player kill
    /// should return ammunition to the currently held weapon.
    ///
    /// The actual ammo variable currently lives somewhere
    /// outside weaponManager, so this method is intentionally
    /// compile-safe until gunStats / weaponStats / clip are wired.
    /// </summary>
    public void RefundAmmo(int amount)
    {
        if (amount <= 0)
            return;


        if (activeWeapon == null)
            return;


        // -----------------------------------------------------
        // TODO:
        // Actual ammo refund gets added here after checking:
        //
        // weaponStats.cs
        // gunStats.cs
        // clip.cs
        //
        // Example final behavior:
        //
        // currentAmmo =
        //     Mathf.Min(
        //         currentAmmo + amount,
        //         magazineSize
        //     );
        // -----------------------------------------------------

        Debug.Log(
            "Packet Leech requested +" +
            amount +
            " ammo for " +
            activeWeapon.name
        );
    }


    public void attack()
    {
        if (activeWeapon == null)
            return;


        if (attackTimer < activeWeapon.attackRate)
            return;


        attackTimer = 0f;


        // Player shooting contributes heartbeat stress.
        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.playerShot();
        }


        activeWeapon.Attack();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}