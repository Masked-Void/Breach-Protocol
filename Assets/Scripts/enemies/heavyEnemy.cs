using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class heavyEnemy : enemyBase
{
    [SerializeField] float pushbackForce = 2f;

    protected override void attack()
    {
        agent.stoppingDistance = Mathf.Max(0.5f, attackRange - 0.5f);
        float dist = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);
        if (dist > attackRange) return;

        if (attackTimer > attackRate)
        {
            attackTimer = 0;

            IDamage damageable = gameManager.instance.player.gameObject.GetComponent<IDamage>();
            damageable?.takeDamage(attackDamage);

            playerController pc = gameManager.instance.player.gameObject.GetComponent<playerController>();
            if (pc != null)
            {
                Vector3 pushDir = (gameManager.instance.player.transform.position - transform.position).normalized;
                pc.PushBack(pushDir, pushbackForce);
            }
        }
    }
}