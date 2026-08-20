using UnityEngine;
using System.Collections;
[CreateAssetMenu(menuName = "Weapons/Melee", order = 2)]
public class meleeStats : weaponStats
{
    [Header("Damage")]
    [Range(1, 10)][SerializeField] public int attackDamage;
    [Range(5, 10)][SerializeField] public int attackDist;
    Quaternion katanaOrigRot;
    Transform katanaTransform;
    [Header("Audio")]
    public AudioClip swingSound;
    [Range(0, 1)] public float swingSoundVol = 1f;
    public AudioClip hitFleshSound;
    [Range(0, 1)] public float hitFleshVol = 1f;
    public AudioClip hitWallSound;
    [Range(0, 1)] public float hitWallVol = 1f;
    private IEnumerator katanaSwing()
    {
        float duration = 0.1f;
        float t = 0f;

        Quaternion startRot = katanaOrigRot;
        Quaternion endRot = katanaOrigRot * Quaternion.Euler(28.9087696f, 148.389023f, 97.1623077f);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            katanaTransform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            katanaTransform.localRotation = Quaternion.Lerp(endRot, startRot, t);
            yield return null;
        }
    }
    public override void Attack()
    {

        if (weaponManager.instance.activeWeapon.weaponModel.CompareTag("Katana" ))
        {
        }
        Transform gunBarrel = weaponManager.instance.getBarrel();
        if (gunBarrel == null) return;


        audioManager.instance.playSFX(swingSound, swingSoundVol);

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, attackDist))
        {
            //register source for challenge manager
            enemyBase eb = hit.transform.GetComponent<enemyBase>();
            if (eb != null)
            {
                eb.RegisterDamageSource(weaponManager.instance.activeWeapon, weaponManager.instance.currentWeaponFromGround);
            }
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