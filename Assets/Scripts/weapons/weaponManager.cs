using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    private GameObject spawnedWeaponModel;


    public Transform gunBarrel;
    private float attackTimer;

    [Header("Challenge")]
    public bool currentWeaponFromGround = false;
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

    public void equipWeapon(weaponStats newWeapon, Transform weaponHolder)
    {
        if (newWeapon == null) return;

        // Drop and destroy current weapon
        if (spawnedWeaponModel != null)
        {
            Vector3 dropPos = transform.position + transform.forward * 1.5f + transform.up * 0.5f;
            Instantiate(activeWeapon.weaponModel, dropPos, Quaternion.identity);
            Destroy(spawnedWeaponModel);
        }

        activeWeapon = newWeapon;
        // Spawn model
        spawnedWeaponModel = Instantiate(newWeapon.weaponModel, weaponHolder, false);

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;
        spawnedWeaponModel.TryGetComponent<clip>(out clip clip);
        if(clip != null) clip.enabled = true;

        // Locate the barrel or hitpoint
        string targetName = (newWeapon is gunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = FindDeepChild(spawnedWeaponModel.transform, targetName);
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

        activeWeapon.Attack(this);

    }

    
}