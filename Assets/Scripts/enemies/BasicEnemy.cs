using System.Collections;
using UnityEngine;

public class basicEnemy : enemyBase
{
    [Header("Melee")]
    [SerializeField] GameObject weapon;
    [SerializeField] Transform handPos;
    GameObject spawnedWeaponModel;

    Quaternion katanaOrigRot;
    Transform katanaTransform;

    protected override void Start()
    {
        base.Start();
        if (weapon != null && handPos != null)
        {
            spawnedWeaponModel = Instantiate(weapon, handPos);
            spawnedWeaponModel.transform.localPosition = Vector3.zero;
            spawnedWeaponModel.transform.localRotation = Quaternion.identity;

            katanaTransform = spawnedWeaponModel.transform;
            katanaOrigRot = katanaTransform.localRotation;
        }
    }

    protected override void attack()
    {
        if (tryMeleeHit() && katanaTransform != null)
        {
            StartCoroutine(katanaSwing());
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
        throwWeapon(spawnedWeaponModel, handPos.transform);
        katanaTransform = null; 
        base.die();
    }
}