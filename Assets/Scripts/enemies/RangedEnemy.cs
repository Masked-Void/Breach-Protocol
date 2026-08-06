using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    [Header("Weapon")]
    [SerializeField] GameObject bullet;
    [SerializeField] Transform gunPivot;
    [SerializeField] Transform shootPos;
    [Range(1, 30)][SerializeField] int gunRotateSpeed;

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
        gunPivot.rotation = Quaternion.Lerp(gunPivot.rotation, rot, gunRotateSpeed * Time.deltaTime);
    }
}
