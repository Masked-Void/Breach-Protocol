using UnityEngine;

public class weaponManager : MonoBehaviour
{
    public static weaponManager instance { get; private set; }

    [Header("Weapon")]
    public weaponStats activeWeapon;
    private GameObject spawnedWeaponModel;

    [Header("Inventory")]
    public weaponStats[] weapons = new weaponStats[4];

    Transform gunBarrel;
    private float attackTimer;

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
        spawnedWeaponModel = Instantiate(activeWeapon.weaponModel, weaponHolder, false);

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;
        spawnedWeaponModel.TryGetComponent<clip>(out clip clip);
        if (clip != null) clip.enabled = true;

        // Locate the barrel or hitpoint
        string targetName = (activeWeapon is gunStats) ? "Muzzle" : "HitPoint";
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
        activeWeapon.Attack(this);
    }
}