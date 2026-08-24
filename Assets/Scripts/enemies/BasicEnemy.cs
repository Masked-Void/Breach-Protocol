using System.Collections;
using UnityEngine;

public class basicEnemy : enemyBase
{
    [Header("Melee")]
    [SerializeField] GameObject weapon;
    [SerializeField] Transform handPos;

    GameObject spawnedWeapon;

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

            if(spawnedWeapon.TryGetComponent<pickWeapon>(out pickWeapon picker)) picker.enabled = false;

            katanaTransform = spawnedWeapon.transform;
            katanaOrigRot = katanaTransform.localRotation;
        }
    }

    protected override void attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);
        if (katanaTransform != null && attackRange > distToPlayer)
        {
            if (tryMeleeHit())
            {
                StartCoroutine(katanaSwing());

            }
        }
    }

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

    public override void die()
    {
        throwWeapon(spawnedWeapon, handPos);
        katanaTransform = null; 
        base.die();
    }
}