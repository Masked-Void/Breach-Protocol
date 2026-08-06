using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class heavyEnemy : EnemyBase
{
    [SerializeField] float pushbackForce = 2f;

    protected override void attack()
    {
        if (tryMeleeHit())
        {
            playerController pc = gameManager.instance.player.GetComponent<playerController>();
            if (pc != null)
            {
                Vector3 pushDir = (gameManager.instance.player.transform.position - transform.position).normalized;
                pc.PushBack(pushDir, pushbackForce);
            }
        }
    }
}