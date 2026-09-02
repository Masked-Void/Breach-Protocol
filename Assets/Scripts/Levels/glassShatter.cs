using UnityEngine;

public class glassShatter : MonoBehaviour
{
    public GameObject wholeGlass;
    public Rigidbody[] shards;

    public float explosionRadius = 2f;
    public float YForce = 0.4f;

    bool hasShattered = false;

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