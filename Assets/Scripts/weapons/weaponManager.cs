using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    public GameObject spawnedWeaponModel;

    [Header("Inventory")]
    public weaponStats[] weapons = new weaponStats[3];
    public int[] weaponAmmo = new int[3];
    public Sprite emptySlot;

    Transform gunBarrel;
    private float attackTimer;

    [Header("Challenge")]
    public bool currentWeaponFromGround = false;
    int activeSlot = 0;

    int currentAmmo;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        // Initialize weapons array
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weaponAmmo[i] = ((gunStats)weapons[i]).startingBullets;
            }
        }

        activeSlot = 0;
        activeWeapon = weapons[0];
        currentWeaponFromGround = false;
    }

    void Update()
    {

        attackTimer += Time.unscaledDeltaTime;
    }


    public void switchToNextWeapon()
    {
        if (weapons.Length == 0) return;

        int startSlot = activeSlot;
        int nextSlot = (activeSlot + 1) % weapons.Length;

        while (nextSlot != startSlot)
        {
            if (weapons[nextSlot] != null)
            {
                if (spawnedWeaponModel != null)
                {
                    Destroy(spawnedWeaponModel);
                }

                activeSlot = nextSlot;
                activeWeapon = weapons[activeSlot];
                currentWeaponFromGround = false;

                if (gameManager.instance != null && gameManager.instance.playerScript != null)
                {
                    showActiveweapon(gameManager.instance.playerScript.weaponHoldPos.transform);
                }

                updateHUD();
                return;
            }
            nextSlot = (nextSlot + 1) % weapons.Length;
        }
    }

    public void equipWeapon(weaponStats newWeapon)
    {
        if (newWeapon == null) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == newWeapon)
                return;
        }

        int slot = -1;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                slot = i;
                break;
            }
        }

        if (slot == -1) return;

        weapons[slot] = newWeapon;
    }

    public Transform getBarrel() => gunBarrel;
    public int getActiveSlot() => activeSlot;

    public void showActiveweapon(Transform weaponHolder)
    {
        activeWeapon = weapons[activeSlot];
        if (activeWeapon == null) return;

        spawnedWeaponModel = Instantiate(activeWeapon.weaponModel, weaponHolder, false);

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;

        if (spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = true;

        // Locate the barrel or hitpoint
        string targetName = (activeWeapon is gunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = FindDeepChild(spawnedWeaponModel.transform, targetName);
    }

    public void throwWeapon()
    {
        if (spawnedWeaponModel == null) return;
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = false;

        Rigidbody projectileRb;
        if (!spawnedWeaponModel.TryGetComponent<Rigidbody>(out projectileRb))
        {
            projectileRb = spawnedWeaponModel.AddComponent<Rigidbody>();
        }

        projectileRb.isKinematic = false;
        projectileRb.useGravity = true;

        // Calculate directional trajectory
        Vector3 forceDirection = Camera.main.transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(gameManager.instance.playerScript.weaponHoldPos.transform.position,
                            gameManager.instance.playerScript.weaponHoldPos.transform.forward,
                            out hit, 500f))
        {
            forceDirection = (hit.point - gameManager.instance.playerScript.weaponHoldPos.transform.position).normalized;
        }

        // Apply forward and upward force
        Vector3 forceToAdd = forceDirection * gameManager.instance.playerScript.throwForce
                           + gameManager.instance.player.transform.up * gameManager.instance.playerScript.throwUpwardForce;

        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);

        // Add subtle spin for realistic throwing physics
        projectileRb.AddTorque(Camera.main.transform.right * 10f, ForceMode.Impulse);


        // Ensure Colliders are active
        if (spawnedWeaponModel.TryGetComponent<Collider>(out Collider weaponCollider))
        {
            weaponCollider.enabled = true;
        }

        weapons[activeSlot] = null;
        activeWeapon = null;
        spawnedWeaponModel = null;
        gunBarrel = null;

        updateHUD();
    }

    // find nested children
    public Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void attack()
    {
        if (activeWeapon == null || attackTimer < activeWeapon.attackRate)
            return;

        attackTimer = 0f;

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.playerShot();
        }

        activeWeapon.Attack();

    }

    void updateHUD()
    {
        if (gameManager.instance == null) return;
        if (activeWeapon != null)
        {
            gameManager.instance.magAmmoUI.text = weaponAmmo[activeSlot].ToString();
            gameManager.instance.totalAmmoUI.text = "0";
        }

        updateWeaponIcons();
    }

    void updateWeaponIcons()
    {
        if (gameManager.instance == null) return;

        int nextSlot = (activeSlot + 1) % weapons.Length;
        if (weapons[nextSlot] != null && gameManager.instance.inActiveWeapon1 != null)
        {
            gameManager.instance.inActiveWeapon1.sprite = weapons[nextSlot].sprite;
        }
        else
        {
            gameManager.instance.inActiveWeapon1.sprite = emptySlot;
        }

        int secondNextSlot = (activeSlot + 2) % weapons.Length;
        if (weapons[secondNextSlot] != null && gameManager.instance.inActiveWeapon2 != null)
        {
            gameManager.instance.inActiveWeapon2.sprite = weapons[secondNextSlot].sprite;
        }
        else
        {
            gameManager.instance.inActiveWeapon2.sprite = emptySlot;
        }
    }
}