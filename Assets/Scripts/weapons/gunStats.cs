using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Gun")]

public class gunStats : weaponStats
{
    public enum GunType { Pistol, AR, Shotgun, Kunai }

    [Header("Gun Settings")]
    public GunType gunType;

    [Header("Projectile")]
    [SerializeField] public Transform bullet;

    [Header("Spawn Position")]
    public Vector3 Position;
    public Vector3 Rotation;

    [Header("Ammo")]
    [Range(1, 20)] public int pelletCount;
    [Range(.2f, 20f)] public float spreadAngle;
    [Range(3, 30)] public int startingBullets;

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0, 1)] public float shootSoundVol;

    public override void Attack()
    {

        Transform gunBarrel = weaponManager.instance.getBarrel();
        if (gunBarrel == null) return;

        audioManager.instance.playSFX(shootSound, shootSoundVol);

        bool hasKunaiSpread = upgradeManager.instance != null &&
                      upgradeManager.instance.IsUpgradeActive("kunai_spread");

        int shotsToFire = (gunType == GunType.Shotgun) ? pelletCount : 1;
        float spreadToUse = (gunType == GunType.Shotgun) ? spreadAngle : 0f;

        if (gunType == GunType.Kunai && hasKunaiSpread)
        {
            shotsToFire = 3;
            spreadToUse = 15f;
        }

        if (bullet != null)
        {
            for (int i = 0; i < shotsToFire; i++)
            {
                // Calculate random deviation within the spread angle cone
                float randomSpreadX = Random.Range(-spreadToUse, spreadToUse);
                float randomSpreadY = Random.Range(-spreadToUse, spreadToUse);

                // Combine the barrel's base rotation with our random offset angles
                Quaternion spreadRotation = gunBarrel.rotation * Quaternion.Euler(randomSpreadX, randomSpreadY, 0);

                // Spawn the bullet projectile flying out into its offset trajectory
                Transform spawnedBullet = Instantiate(bullet, gunBarrel.position, spreadRotation);

                if (spawnedBullet.TryGetComponent<damage>(out damage dmg))
                {
                    dmg.sourceWeapon = this;

                    if (upgradeManager.instance != null && upgradeManager.instance.IsUpgradeActive("exploding_bullets"))
                        dmg.isExplosive = true;
                }
            }
        }
    }


}
