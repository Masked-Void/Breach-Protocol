using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    public GameObject spawnedWeaponModel;

    [Header("Inventory")]
    public weaponStats[] weapons = new weaponStats[4];

    Transform gunBarrel;
    private float attackTimer;

    [Header("Challenge")]
    public bool currentWeaponFromGround = false;
    int activeSlot = 0;

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

    void Update()
    {
    
        attackTimer += Time.unscaledDeltaTime;
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

    public Transform getBarrel()
    {
        return gunBarrel;
    }

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

    
}