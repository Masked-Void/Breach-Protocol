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

    
    

    [Header("Audio")]
    public AudioClip shootSound;
    [Range(0, 1)] public float shootSoundVol;

    public override void Attack(weaponManager manager)
    {
        Transform gunBarrel = manager.gunBarrel;
        if (gunBarrel == null) return;

            audioManager.instance.playSFX(shootSound, shootSoundVol);
     
        int shotsToFire = (gunType == GunType.Shotgun || gunType == GunType.Kunai) ? pelletCount : 1;
        spreadAngle = (gunType == GunType.Shotgun) ? spreadAngle : 0;

        //Upgrade Check
        if (gunType == GunType.Kunai && FindAnyObjectByType<playerController>().kunaiSpread)
        {
            int boostedPelletCount = 3;
            int boostedSpreadAngle = 15;
            shotsToFire += boostedPelletCount;
            spreadAngle += boostedSpreadAngle;
        }

        if (bullet != null)
        {
            for (int i = 0; i < shotsToFire; i++)
            {
                // Calculate random deviation within the spread angle cone
                float randomSpreadX = Random.Range(-spreadAngle, spreadAngle);
                float randomSpreadY = Random.Range(-spreadAngle, spreadAngle);

                // Combine the barrel's base rotation with our random offset angles
                Quaternion spreadRotation = gunBarrel.rotation * Quaternion.Euler(randomSpreadX, randomSpreadY, 0);
                

                // Spawn the bullet projectile flying out into its offset trajectory
                Transform spawnedBullet = MonoBehaviour.Instantiate(bullet, gunBarrel.position, spreadRotation);
                
                //Upgrade Check
                if (FindAnyObjectByType<playerController>().explodingBullets)
                {
                    damage dmg = spawnedBullet.GetComponent<damage>();
                    dmg.isExplosive = true;
                }
                
            }
        }
    }

 
}
