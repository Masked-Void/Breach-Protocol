using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Melee", order = 2)]
public class meleeStats : weaponStats
{
    [Header("Damage")]
    [Range(1, 10)][SerializeField] public int attackDamage;
    [Range(5, 10)][SerializeField] public int attackDist;

    [Header("Audio")]
    public AudioClip swingSound;
    [Range(0, 1)] public float swingSoundVol = 1f;
    public AudioClip hitFleshSound;
    [Range(0, 1)] public float hitFleshVol = 1f;
    public AudioClip hitWallSound;
    [Range(0, 1)] public float hitWallVol = 1f;

    public override void Attack()
    {
        Transform gunBarrel = weaponManager.instance.getBarrel();
        if (gunBarrel == null) return;

        audioManager.instance.playSFX(swingSound, swingSoundVol);

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, attackDist))
        {
            IDamage dmg = hit.transform.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(attackDamage);
                audioManager.instance.playSFX(hitFleshSound, hitFleshVol);
            }
            else
            {
                audioManager.instance.playSFX(hitWallSound, hitWallVol);
            }
        }
    }
}