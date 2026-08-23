using System.Collections;
using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    public weaponStats starterWeapon;
    [SerializeField] private weaponStats[] allWeapons;
    public Sprite emptySlot;


    // [Header("Challenge")]
    // public bool currentWeaponFromGround = false;

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
        if (gameManager.instance == null || gameManager.instance.playerScript == null) return;

        weaponHolder = gameManager.instance.playerScript.weaponHoldPos.transform;

        // Load saved weapon
        string savedWeaponName = PlayerPrefs.GetString("EquippedWeapon", "");
        if (!string.IsNullOrEmpty(savedWeaponName) && allWeapons != null)
        {
            weaponStats loadedWeapon = System.Array.Find(allWeapons, w => w != null && w.Name == savedWeaponName);
            if (loadedWeapon != null)
            {
                activeWeapon = loadedWeapon;
            }
        }
        else
        {
            if (starterWeapon != null) activeWeapon = starterWeapon;
        }

        if (activeWeapon != null) spawnWeapon(activeWeapon);
    }

    void Update()
    {

        attackTimer += Time.unscaledDeltaTime;
    }

    void OnDestroy()
    {
        activeWeapon.isFromGround = false;
        if (instance == this) instance = null;
    }

    public void equipWeapon(weaponStats newWeapon)
    {
        StartCoroutine(equip(newWeapon));
    }

    IEnumerator equip(weaponStats newWeapon)
    {
        if (newWeapon == null) yield return null;
        if (spawnedWeaponModel != null) throwWeapon();
        yield return new WaitForSeconds(1.5f);
        audioManager.instance.playEquip();
        spawnWeapon(newWeapon);
    }

    private void spawnWeapon(weaponStats newWeapon)
    {
        activeWeapon = newWeapon;

        if (activeWeapon is gunStats gun) currentAmmo = gun.startingBullets;
        if (activeWeapon is meleeStats melee) currentAmmo = 10_000;

        spawnedWeaponModel = Instantiate(activeWeapon.weaponModel, weaponHolder, false);
        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;

        if (spawnedWeaponModel.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (spawnedWeaponModel.TryGetComponent<pickWeapon>(out pickWeapon picker)) picker.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = true;
        if (spawnedWeaponModel.TryGetComponent<damage>(out damage thrownDamage)) thrownDamage.enabled = false;

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

        activeWeapon.isFromGround = false;

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
        if (spawnedWeaponModel.TryGetComponent<damage>(out damage thrownDamage)) thrownDamage.enabled = true;

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

    public float getUpgradeFireRate()
    {
        if (activeWeapon == null) return 0f;
        float rate = activeWeapon.attackRate;

        // Check if fire rate upgrade is active
        if (upgradeManager.instance != null && upgradeManager.instance.IsUpgradeActive("fire_rate"))
            rate /= 1.5f;

        return rate;
    }

    public void attack()
    {
        if (activeWeapon == null || attackTimer < getUpgradeFireRate()) return;
        if (currentAmmo <= 0) { audioManager.instance.playEmptyMag(); return; }
        if (heartbeatManager.instance != null) heartbeatManager.instance.playerShot();

        attackTimer = 0f;
        currentAmmo--;
        activeWeapon.Attack();
    }

    void updateHUD()
    {
        if (gameManager.instance == null) return;

        if (activeWeapon != null && activeWeapon is gunStats)
        {
            gameManager.instance.ammoPanel.SetActive(true);
            gameManager.instance.magAmmoUI.text = currentAmmo.ToString();
        }
        else
            gameManager.instance.ammoPanel.SetActive(activeWeapon is gunStats);

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
            gameManager.instance.magAmmoUI.text = "0";
            gameManager.instance.activeWeapon.sprite = emptySlot;
        }
    }

    [ContextMenu("Reset Saved Weapon")]
    public void ResetWeapon()
    {
        PlayerPrefs.DeleteKey("EquippedWeapon");
    }
}