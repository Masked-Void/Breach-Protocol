using UnityEngine;

/*
 * Script: HeavyEnemy
 *
 * Description:
 * Melee enemy that shoves the player instead of dealing damage. Used to break
 * up the player's positioning rather than to kill them directly.
 *
 * Interacts With:
 * - EnemyBase (everything else comes from the base)
 * - PlayerController (PushBack)
 */
public class HeavyEnemy : EnemyBase
{
    [Tooltip("how hard the shove is, applied along the direction from us to the player")]
    [SerializeField] float pushbackForce = 2f;

    // shoves rather than damages, the knock is the whole point of this enemy
    protected override void attack()
    {
        float distToPlayer = Vector3.Distance(transform.position, GameManager.instance.player.transform.position);
        if (AttackRange > distToPlayer)
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