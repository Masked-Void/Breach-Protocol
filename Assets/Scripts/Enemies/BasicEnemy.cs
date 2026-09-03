using System.Collections;
using UnityEngine;

/*
 * Script: BasicEnemy
 *
 * Description:
 * Melee enemy carrying a katana. Spawns the weapon into its hand on start,
 * swings on attack, and throws the weapon on death so the player can pick it up.
 *
 * Interacts With:
 * - EnemyBase (sight, roaming, death path)
 * - PickWeapon (disabled while the enemy holds it, re-enabled when thrown)
 *
 * Notes:
 * - Weapons dropping on death is how the player rearms. The GDD says weapons
 *   only come from enemy drops.
 */
public class BasicEnemy : EnemyBase
{
    [Header("Melee")]
    [Tooltip("weapon prefab spawned into the hand on start, and thrown on death")]
    [SerializeField] GameObject weapon;

    [Tooltip("empty transform on the model the weapon parents to")]
    [SerializeField] Transform handPos;

    // the spawned instance, thrown on death
    GameObject spawnedWeapon;

    // resting rotation, the swing lerps away from this and back
    Quaternion katanaOrigRot;
    Transform katanaTransform;

    protected override void Start()
    {
        base.Start();
        if (weapon != null && handPos != null)
        {
            spawnedWeapon = Instantiate(weapon, handPos);
            spawnedWeapon.transform.localPosition = Vector3.zero;
            spawnedWeapon.transform.localRotation = Quaternion.identity;

            if (spawnedWeapon.TryGetComponent<PickWeapon>(out PickWeapon picker))
                picker.enabled = false;

            katanaTransform = spawnedWeapon.transform;
            katanaOrigRot = katanaTransform.localRotation;
        }
    }

    // swings only if the hit actually connects, so the animation never plays on a miss
    protected override void attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);
        if (katanaTransform != null && AttackRange > distToPlayer)
        {
            if (tryMeleeHit())
            {
                StartCoroutine(katanaSwing());

            }
        }
    }

    // rotates out and back over 0.1s each way. the euler values are a pose
    // captured from the model, not a meaningful angle.
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

    public override void Die()
    {
        ThrowWeapon(spawnedWeapon, handPos);
        katanaTransform = null;
        base.Die();
    }
}