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
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 0.5f);
        float dist = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);
        if (dist > attackRange) return;

        if (attackTimer > attackRate)
        {
            attackTimer = 0;
            if (katanaTransform != null) StartCoroutine(katanaSwing());

            IDamage damageable = gameManager.instance.player.gameObject.GetComponent<IDamage>();
            damageable?.takeDamage(attackDamage);
        }
    }

    private IEnumerator katanaSwing()
    {
        float duration = 0.1f;
        float t = 0f;

        Quaternion startRot = katanaOrigRot;
        Quaternion endRot = katanaOrigRot * Quaternion.Euler(60f, 0f, 0f);

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