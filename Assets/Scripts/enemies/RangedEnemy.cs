using UnityEngine;

[RequireComponent(typeof(Patrol))]
public class rangedEnemy : enemyBase
{
    [Header("Weapon")]
    [SerializeField] Transform gunPivot;
    [Range(1, 30)][SerializeField] int gunRotateSpeed;

    [Header("Roam")]
    [SerializeField] float waitTimeOnWayPoint = 2f;
    public Patrol patrol;

    public GameObject gunModel;
    public weaponStats[] gunPrefabs;

    gunStats activeGun;
    private GameObject spawnedWeaponModel;
    Transform gunBarrel;

    int currentAmmo;
    float timer;

    protected override void Start()
    {
        base.Start();
        SetWeaponPrefab();
        currentAmmo = activeGun.startingBullets;
        if (TryGetComponent<Patrol>(out Patrol patrol))
            agent.destination = patrol.getCurrentWayPointPos();
    }

    protected override void attack()
    {
        agent.stoppingDistance = stoppingDistOrig;

        if (gunPivot != null) rotateGun();
        if (attackTimer > attackRate) shoot();
    }

    public override bool canSeePlayer()
    {
        if (gameManager.instance?.player == null) return false;
        
        playerDir = gameManager.instance.player.transform.position - transform.position;
        angleToPlayer = Vector3.Angle(playerDir, transform.forward);

        if (Physics.Raycast(transform.position, playerDir, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer <= FOV)
            {
                faceTarget();
                attack();
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return true;
    }

    public override void checkRoam()
    {
        if(agent.remainingDistance <= 0.1f)
        {
            timer += Time.deltaTime;
            if(timer >= waitTimeOnWayPoint)
            {
                timer = 0f;
                agent.destination = patrol.getNextWayPointPos();
            }
        }
    }

    void shoot()
    {
        if(currentAmmo <= 0) return;

        currentAmmo--;
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
        if (spawnedWeaponModel.TryGetComponent<clip>(out var weaponClip)) weaponClip.enabled = true;
        if (spawnedWeaponModel.TryGetComponent<pickWeapon>(out var picker)) picker.enabled = false;

        // Locate the barrel or hitpoint
        string targetName = (selectedGun is gunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = weaponManager.instance.FindDeepChild(spawnedWeaponModel.transform, targetName);
        activeGun = (gunStats)selectedGun;
    }

    public override void die()
    {
        throwWeapon(spawnedWeaponModel, gunModel.transform);
        gunBarrel = null;
        base.die();
    }
}
