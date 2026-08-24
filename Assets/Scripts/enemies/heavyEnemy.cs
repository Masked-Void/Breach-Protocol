using UnityEngine;

public class heavyEnemy : enemyBase
{
    [SerializeField] float pushbackForce = 2f;

    protected override void attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);
        if (attackRange > distToPlayer)
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