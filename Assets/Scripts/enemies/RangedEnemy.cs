using UnityEngine;
using UnityEngine.AI;

public class rangedEnemy : enemyBase
{
    [Header("Weapon")]
    [SerializeField] Transform gunPivot;
    [Range(1, 30)][SerializeField] int gunRotateSpeed;

    public Transform shootPos;
    public Transform bullet;
    public GameObject gunModel;


    private void Awake()
    {
        
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
        if(audioManager.instance != null) 
            audioManager.instance.playSpatialSFX(audioManager.instance.enemyShoot, shootPos.position, audioManager.instance.enemyShootVol);
        
        if (bullet != null && shootPos != null && gunPivot != null)
            Instantiate(bullet, shootPos.position, gunPivot.rotation);
    }

    void rotateGun()
    {
        Quaternion rot = Quaternion.LookRotation(playerDir);
        gunPivot.rotation = Quaternion.Lerp(transform.rotation, rot, gunRotateSpeed * Time.deltaTime);
    }

    public override void SetWeaponPrefab(GameObject weaponPrefab)
    {
       
        gunModel = weaponPrefab;
        bullet = gunModel.GetComponent<gunStats>().bullet;
        weaponStats newWeapon = weaponPrefab.GetComponent<weaponStats>();
        string targetName = (newWeapon is gunStats) ? "Muzzle" : "HitPoint";
        shootPos = FindDeepChild(gunModel.transform, targetName);
    }
}
