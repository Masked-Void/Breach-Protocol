using UnityEngine;
using UnityEngine.AI;

public class rangedEnemy : enemyBase
{
    [Header("Weapon")]
    [SerializeField] Transform gunPivot;
    [Range(1, 30)][SerializeField] int gunRotateSpeed;

    public GameObject gunModel;
    public weaponStats[] gunPrefabs;

    gunStats activeGun;
    private GameObject spawnedWeaponModel;


    public Transform gunBarrel;


    protected override void Start()
    {
        base.Start();
        SetWeaponPrefab();
    }

    protected override void attack()
    {
        agent.stoppingDistance = stoppingDistOrig;

        if (gunPivot != null) rotateGun();
        if (attackTimer > attackRate) shoot();
    }

    void shoot()
    {
        attackTimer = 0f;
        if (audioManager.instance != null)
            audioManager.instance.playSpatialSFX(audioManager.instance.enemyShoot, gunBarrel.position, audioManager.instance.enemyShootVol);

        if (activeGun.bullet != null && gunPivot != null)
            Instantiate(activeGun.bullet, gunBarrel.position, gunPivot.rotation);
    }

    void rotateGun()
    {
        Quaternion rot = Quaternion.LookRotation(playerDir);
        gunPivot.rotation = Quaternion.Lerp(gunPivot.rotation, rot, gunRotateSpeed * Time.deltaTime);
    }

    public void SetWeaponPrefab()
    {
        weaponStats selectedGun = gunPrefabs[Random.Range(0, gunPrefabs.Length)];
        spawnedWeaponModel = Instantiate(selectedGun.weaponModel, gunModel.transform, false);

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;
        spawnedWeaponModel.TryGetComponent<clip>(out clip clip);
        spawnedWeaponModel.TryGetComponent<pickWeapon>(out pickWeapon picker);
        if(clip != null) clip.enabled = true;
        if(clip != null) picker.enabled = false;

        // Locate the barrel or hitpoint
        string targetName = (selectedGun is gunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = weaponManager.instance.FindDeepChild(spawnedWeaponModel.transform, targetName);
        activeGun = (gunStats)selectedGun;
    }
}
