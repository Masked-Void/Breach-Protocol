using UnityEngine;

public class HeavyEnemy : EnemyBase
{
    [SerializeField] float pushbackForce = 2f;

    protected override void attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);
        if (attackRange > distToPlayer)
        {
            PlayerController pc = GameManager.instance.player.GetComponent<PlayerController>();
            if (pc != null)
            {
                Vector3 pushDir = (GameManager.instance.player.transform.position - transform.position).normalized;
                pc.PushBack(pushDir, pushbackForce);
            }
        }
    }
}