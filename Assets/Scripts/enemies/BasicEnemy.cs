using System.Collections;
using UnityEngine;

public class basicEnemy : enemyBase
{
    [Header("Melee")]
    [SerializeField] GameObject weapon;
    [SerializeField] Transform handPos;

    Quaternion katanaOrigRot;
    Transform katanaTransform;

    protected override void Start()
    {
        base.Start();
        if (weapon != null && handPos != null)
        {
            GameObject weaponInstance = Instantiate(weapon, handPos);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            katanaTransform = weaponInstance.transform;
            katanaOrigRot = katanaTransform.localRotation;
        }
    }

    protected override void attack()
    {
        if (katanaTransform != null)
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
}