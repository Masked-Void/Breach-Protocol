using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    public Sprite emptySlot;


    [Header("Challenge")]
    public bool currentWeaponFromGround = false;

    GameObject spawnedWeaponModel;
    Transform gunBarrel;
    float attackTimer;

    int currentAmmo;
    Transform weaponHolder;

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
        weaponHolder = gameManager.instance.playerScript.weaponHoldPos.transform;
        if (activeWeapon != null) spawnWeapon(activeWeapon);
    }

    void Update()
    {

        attackTimer += Time.unscaledDeltaTime;
    }

    public void equipWeapon(weaponStats newWeapon)
    {
        if (newWeapon == null) return;
        if (spawnedWeaponModel != null) throwWeapon();
        spawnWeapon(newWeapon);
    }

    private void spawnWeapon(weaponStats newWeapon)
    {
        activeWeapon = newWeapon;

        if (activeWeapon is gunStats gun) currentAmmo = gun.startingBullets;

        spawnedWeaponModel = Instantiate(activeWeapon.weaponModel, weaponHolder, false);
        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;

        if (spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = true;

        string targetName = (activeWeapon is gunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = FindDeepChild(spawnedWeaponModel.transform, targetName);

        updateHUD();
    }

    public Transform getBarrel() => gunBarrel;
    public int getCurrentAmmo() => currentAmmo;

    public void throwWeapon()
    {
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<pickWeapon>(out pickWeapon picker)) picker.enabled = false;
        if (!spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody projectileRb))
            projectileRb = spawnedWeaponModel.AddComponent<Rigidbody>();

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

        if (spawnedWeaponModel.TryGetComponent<Collider>(out Collider weaponCollider)) weaponCollider.enabled = true;

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
        if (currentAmmo <= 0) { audioManager.instance.playEmptyMag(); return; }

        attackTimer = 0f;
        currentAmmo--;

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
            gameManager.instance.magAmmoUI.text = currentAmmo.ToString();
        }
        else
        {
            gameManager.instance.magAmmoUI.text = "0";
        }

        updateWeaponIcons();
    }

    void updateWeaponIcons()
    {
        if (gameManager.instance == null) return;

        if (activeWeapon != null && gameManager.instance.activeWeapon != null)
        {
            gameManager.instance.activeWeapon.sprite = activeWeapon.sprite;
        }
        else
        {
            gameManager.instance.activeWeapon.sprite = emptySlot;
        }
    }
}