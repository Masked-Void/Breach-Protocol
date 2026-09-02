using UnityEngine;
using System.Collections;
[CreateAssetMenu(menuName = "Weapons/Melee", order = 2)]
public class MeleeStats : WeaponStats
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
    public AudioClip hitShieldSound;
    [Range(0,1)] public float hitShieldVol = 1f;
    /*private IEnumerator katanaSwing()
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
    } cant call this coroutine from non monobehaviour script
    */
    public override void Attack()
    {
        Transform gunBarrel = WeaponManager.instance.getBarrel();
        if (gunBarrel == null) return;

        WeaponManager.instance.PlayMeleeSwing();
        AudioManager.instance.playSFX(swingSound, swingSoundVol);

        AudioManager.instance.playSFX(swingSound, swingSoundVol);
        //StartCoroutine(katanaSwing());

        RaycastHit hit;
        if (Physics.Raycast(gunBarrel.position, gunBarrel.forward, out hit, attackDist))
        {
            // //register source for challenge manager
            EnemyBase eb = hit.transform.GetComponent<EnemyBase>();
            if (eb == null) eb = hit.transform.GetComponentInParent<EnemyBase>();
            if (eb != null) eb.RegisterDamageSource(this, isFromGround);

            IDamage dmg = hit.transform.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(attackDamage);
                AudioManager.instance.playSFX(hitFleshSound, hitFleshVol);
            } else if (hit.collider.CompareTag("Shield"))
            {
                AudioManager.instance.playSFX(hitShieldSound, hitShieldVol);
            }
            else
            {
                AudioManager.instance.playSFX(hitWallSound, hitWallVol);
            }
            
        }
    }
}