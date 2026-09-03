using UnityEngine;

/*
 * Script: GlassShatter
 *
 * Description:
 * Breakable glass. Swaps the whole pane for a pre-broken set of shards and
 * pushes them apart from the hit point, so the break reads as coming from
 * where the bullet went through.
 *
 * Interacts With:
 * - Damage (bullets call Shatter and pass their direction and force)
 *
 * Notes:
 * - The explosion origin is pulled back slightly along the bullet direction, so
 *   shards blow away from the shooter rather than straight sideways.
 * - Shards get an extra directional push on top of the explosion, otherwise the
 *   break looks like a bomb rather than a bullet.
 */
public class GlassShatter : MonoBehaviour
{
    [Tooltip("the intact pane, hidden the moment it breaks")]
    public GameObject wholeGlass;

    [Tooltip("the pre-broken pieces, disabled and kinematic until it breaks")]
    public Rigidbody[] shards;

    [Tooltip("how far from the hit point the blast reaches, in metres")]
    public float explosionRadius = 2f;

    [Tooltip("extra upward lift on the shards, 0 blows them flat")]
    public float YForce = 0.4f;

    // only breaks once no matter how many bullets hit it
    bool hasShattered = false;


    // called by a bullet. bulletShatterForce of -1 falls back to the default 200.
    public void Shatter(Vector3 hitPoint, Vector3 bulletDirection = default, float bulletShatterForce = -1f)
    {
        if (hasShattered) return;
        hasShattered = true;

        if (wholeGlass != null)
        {
            wholeGlass.SetActive(false);
        }

        float forceToApply = (bulletShatterForce > 0) ? bulletShatterForce : 200f;

        Vector3 explosionOrigin = hitPoint;
        if (bulletDirection != Vector3.zero)
        {
            explosionOrigin -= bulletDirection.normalized * 0.2f;
        }

        foreach (Rigidbody r in shards)
        {
            r.gameObject.SetActive(true);
            r.isKinematic = false;

            r.AddExplosionForce(forceToApply, explosionOrigin, explosionRadius, YForce, ForceMode.Impulse);

            if (bulletDirection != Vector3.zero)
            {
                r.AddForce(bulletDirection.normalized * (forceToApply * 0.4f), ForceMode.Impulse);
            }

        }
        Destroy(gameObject, 3f);
    }
}