using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Weapon")]
    [SerializeField] Transform gunPivot;
    [Range(1, 30)][SerializeField] int gunRotateSpeed;

    public GameObject gunModel;
    public WeaponStats[] gunPrefabs;

    [SerializeField] PatrolPath patrol;

    GunStats activeGun;
    private GameObject spawnedWeaponModel;

    public Transform gunBarrel;

    int currentAmmo;

    protected override void Start()
    {
        base.Start();
        SetWeaponPrefab();
        currentAmmo = activeGun.startingBullets * 3;
        if (TryGetComponent<PatrolPath>(out patrol))
            agent.destination = patrol.CurrentWaypointPosition;
    }

    protected override void attack()
    {
        agent.stoppingDistance = stoppingDistOrig;

        if (gunPivot != null) rotateGun();
        if (attackTimer > attackRate && currentAmmo >= 0) shoot();
    }

    void shoot()
    {
        currentAmmo--;
        attackTimer = 0f;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySpatialSFX(
                AudioManager.instance.PickRandomAudio(AudioManager.instance.enemyShoot),
                gunBarrel.position,
                AudioManager.instance.enemyShootVol);

        if (activeGun == null || activeGun.bullet == null || gunPivot == null || gunBarrel == null)
            return;

        bool isShotgun = activeGun.gunType == GunStats.GunType.Shotgun;

        int shotsToFire = isShotgun ? Mathf.Max(1, activeGun.pelletCount) : 1;
        float spread = isShotgun ? activeGun.spreadAngle : 0f;

        for (int i = 0; i < shotsToFire; i++)
        {
            float spreadX = Random.Range(-spread, spread);
            float spreadY = Random.Range(-spread, spread);

            Quaternion shotRotation = gunPivot.rotation * Quaternion.Euler(spreadX, spreadY, 0f);

            Instantiate(activeGun.bullet, gunBarrel.position, shotRotation);
        }
    }

    void rotateGun()
    {
        Quaternion rot = Quaternion.LookRotation(playerDir);
        gunPivot.rotation = Quaternion.Lerp(gunPivot.rotation, rot, gunRotateSpeed * Time.deltaTime);
    }

    public void SetWeaponPrefab()
    {
        WeaponStats selectedGun = gunPrefabs[Random.Range(0, gunPrefabs.Length)];
        spawnedWeaponModel = Instantiate(selectedGun.weaponModel, gunModel.transform, false);

        spawnedWeaponModel.transform.localPosition = Vector3.zero;
        spawnedWeaponModel.transform.localRotation = Quaternion.identity;
        if (spawnedWeaponModel.TryGetComponent<WeaponWallAvoidance>(out var weaponClip)) weaponClip.enabled = true;
        if (spawnedWeaponModel.TryGetComponent<PickWeapon>(out var picker)) picker.enabled = false;

        string targetName = (selectedGun is GunStats) ? "Muzzle" : "HitPoint";
        gunBarrel = WeaponManager.instance.FindDeepChild(spawnedWeaponModel.transform, targetName);
        activeGun = (GunStats)selectedGun;

        // each weapon sets its own pacing
        if (activeGun.attackRate > 0f)
            attackRate = activeGun.attackRate;
    }
    public override void Die()
    {
        ThrowWeapon(spawnedWeaponModel, gunModel.transform);
        base.Die();
    }
}
