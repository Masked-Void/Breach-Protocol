using UnityEngine;
using System.Collections;

public class damage : MonoBehaviour
{
    enum damageType { bullet, stationary, DOT, shard, throwable }
    [SerializeField] damageType type;
    [SerializeField] Rigidbody rb;

    [Range(1, 10)][SerializeField] int damageAmount;
    [Range(.1f, 10)][SerializeField] float damageRate;

    [Header("Bullet")]
    [Range(1, 80)][SerializeField] int bulletSpeed;
    [Range(.1f, 20)][SerializeField] int bulletDestroyTime;
    [SerializeField] float shatterForce = 350f;
    [SerializeField] LayerMask deflectLayer;
    [SerializeField] ParticleSystem hitEffect;
    [SerializeField] ParticleSystem explodeEffect;

    bool isDamaging;
    private bool hasHit = false;
    public bool isExplosive;
    private float explosionRadius = 5f;
    private int explosionDamage = 50;
    

    [Header("Challenge Source")]
    public weaponStats sourceWeapon;
    public bool sourceWasGroundPickup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rb == null && !TryGetComponent<Rigidbody>(out rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
       
        if (type == damageType.bullet)
        {
           
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * bulletSpeed;
            Destroy(gameObject, bulletDestroyTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (type != damageType.throwable || hasHit) return;

        Collider other = collision.collider;
        if (other.isTrigger || other.CompareTag("Player")) return;

        hasHit = true;

        // Glass shatter check
        glassShatter glass = other.GetComponent<glassShatter>() ?? other.GetComponentInParent<glassShatter>();
        if (glass != null)
        {
            glass.Shatter(collision.contacts[0].point, transform.forward, shatterForce);
            if (audioManager.instance != null)
                audioManager.instance.playSpatialSFX(audioManager.instance.glass, transform.position, audioManager.instance.glassVol);
        }

        // Register damage source for challenge progression
        enemyBase eb = other.GetComponent<enemyBase>();
        if (eb != null && sourceWeapon != null)
        {
            eb.RegisterDamageSource(sourceWeapon, sourceWasGroundPickup);
        }

        // Deal damage
        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null)
        {
            dmg.takeDamage(damageAmount);
        }

        // Play SFX
        if (audioManager.instance != null)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                audioManager.instance.playSpatialSFX(audioManager.instance.enemyHit, transform.position, audioManager.instance.enemyHitVol);
            else
                audioManager.instance.playSpatialSFX(audioManager.instance.wallHit, transform.position, audioManager.instance.wallHitVol);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        if (type == damageType.bullet && ((1 << other.gameObject.layer) & deflectLayer) != 0)
        {
            DeflectBullet(other);
            return;
        }

        glassShatter glass = other.GetComponent<glassShatter>();
        if (glass == null)
        {
            glass = other.GetComponentInParent<glassShatter>();
        }

        if (glass != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            glass.Shatter(hitPoint, transform.forward, shatterForce);
            audioManager.instance.playSpatialSFX(audioManager.instance.glass, transform.position, audioManager.instance.glassVol);
        }
        // REGISTER SOURCE ON ENEMY BEFORE DAMAGE
         enemyBase eb = other.GetComponent<enemyBase>();
         if (eb != null && sourceWeapon != null)
         {
             eb.RegisterDamageSource(sourceWeapon, sourceWasGroundPickup);
         }

        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null && type != damageType.DOT)
        {
            dmg.takeDamage(damageAmount);
        }

        if (type == damageType.bullet)
        {

            if (isExplosive)
            {
                hitEffect = explodeEffect;
                explode();
                
            }
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            if(other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                audioManager.instance.playSpatialSFX(audioManager.instance.enemyHit, transform.position, audioManager.instance.enemyHitVol);
            else
                audioManager.instance.playSpatialSFX(audioManager.instance.wallHit, transform.position, audioManager.instance.wallHitVol);

            Destroy(gameObject);
        }
    }

    // DOT damage, we do not use it right now
    private void OnTriggerStay(Collider other)
    {
        if (other.isTrigger)
            return;

        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null && type == damageType.DOT && !isDamaging)
        {
            StartCoroutine(damageOther(dmg));
        }
    }

    // Coroutine to handle damage over time, we do not use it right now
    IEnumerator damageOther(IDamage d)
    {
        isDamaging = true;
        d.takeDamage(damageAmount);
        yield return new WaitForSeconds(damageRate);
        isDamaging = false;
    }

    private void DeflectBullet(Collider other)
    {
        Ray ray = new Ray(transform.position - rb.linearVelocity.normalized, rb.linearVelocity.normalized);
        if (other.Raycast(ray, out RaycastHit hit, 2f))
        {
            // Calculate the reflection vector based on current velocity and surface normal
            Vector3 reflectedVelocity = Vector3.Reflect(rb.linearVelocity, hit.normal);
            audioManager.instance.playSpatialSFX(audioManager.instance.bulletRicochet, transform.position, audioManager.instance.bulletRicochetVol);

            transform.forward = reflectedVelocity.normalized;

            // Apply the new velocity
            rb.linearVelocity = reflectedVelocity;
        }
    }

    public void setExplosive()
    {
        isExplosive = FindAnyObjectByType<playerController>().explodingBullets;
    }
    private void explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                dmg.takeDamage(explosionDamage);
            }
        }
    }
}