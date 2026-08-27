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
    bool isEquipping;
    [Header("Dropped Weapons")]
    [SerializeField] LayerMask groundLayers = ~0;
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
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (gameManager.instance == null || gameManager.instance.playerScript == null) return;

        weaponHolder = gameManager.instance.playerScript.weaponHoldPos.transform;

        if (gameManager.instance!=null && gameManager.instance.ammoPanel == gameObject) {
            Debug.LogError("weaponManager: ammoPanel is wired to the weapon manager, fix reference on gameManager" , this);
            gameManager.instance.ammoPanel = null;
        }

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
        if (activeWeapon != null) activeWeapon.isFromGround = false;
        if (instance == this) instance = null;
    }

    // logs when something turns this off and what kind of off it is
    void OnDisable() {
        Debug.Log($"weaponManager disabled | activeSelf {gameObject.activeSelf} | enabled {enabled}" , gameObject);
    }

    public void equipWeapon(weaponStats newWeapon, int ammoOverride = -1)
    {
        StartCoroutine(equip(newWeapon, ammoOverride));
    }

    IEnumerator equip(weaponStats newWeapon, int ammoOverride)
    {
        if (newWeapon == null || isEquipping) yield break;

        isEquipping = true;
        if (spawnedWeaponModel != null) throwWeapon();
        yield return new WaitForSecondsRealtime(.01f);
        if (audioManager.instance != null) audioManager.instance.playEquip();
        spawnWeapon(newWeapon, ammoOverride);
        isEquipping = false;
    }
    private void spawnWeapon(weaponStats newWeapon, int ammoOverride = -1)
    {
        activeWeapon = newWeapon;

        if (activeWeapon is gunStats gun)
            currentAmmo = ammoOverride >= 0 ? ammoOverride : gun.startingBullets;
        else if (activeWeapon is meleeStats)
            currentAmmo = 10_000;

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
        if (spawnedWeaponModel == null) return;
        spawnedWeaponModel.transform.SetParent(null);
        if (spawnedWeaponModel.TryGetComponent<clip>(out clip clip)) clip.enabled = false;
        if (spawnedWeaponModel.TryGetComponent<pickWeapon>(out pickWeapon picker))
        {
            picker.weapon = activeWeapon;
            picker.remainingAmmo = (activeWeapon is gunStats) ? currentAmmo : -1;
            picker.enabled = true;
        }
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

        if (!spawnedWeaponModel.TryGetComponent<droppedWeapon>(out droppedWeapon dropped))
            dropped = spawnedWeaponModel.AddComponent<droppedWeapon>();

        dropped.groundLayers = groundLayers;
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

        bool isGun = activeWeapon is gunStats;
        if (gameManager.instance.ammoPanel != null) {
            gameManager.instance.ammoPanel.SetActive(isGun);
        }

        if (isGun && gameManager.instance.magAmmoUI!=null){
            gameManager.instance.magAmmoUI.text = currentAmmo.ToString();
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