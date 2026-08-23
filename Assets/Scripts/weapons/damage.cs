using UnityEngine;
using System.Collections;

public class damage : MonoBehaviour
{
    enum DamageType { bullet, stationary, DOT, shard, throwable }
    [SerializeField] DamageType type;
    [SerializeField] Rigidbody rb;

    [Range(1, 10)][SerializeField] int damageAmount;
    [Range(.1f, 10)][SerializeField] float damageRate;

    [Header("Bullet")]
    [Range(1, 80)][SerializeField] int bulletSpeed;
    [Range(.1f, 20)][SerializeField] int bulletDestroyTime;
    [SerializeField] float shatterForce = 350f;
    [SerializeField] LayerMask deflectLayer;
    [SerializeField] ParticleSystem hitEffect;

    [Header("Explosion")]
    [SerializeField] ParticleSystem explosionEffect;
    [SerializeField] float explosionForce = 1000f;
    [SerializeField] float explosionRadius = 5f;
    [SerializeField] int explosionDamage = 50;

    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip sfx;

    public bool isExplosive;


    bool isDamaging;
    bool hasHit = false;
    int enemyLayer;
    bool hasAudioManager;

    // [Header("Challenge Source")]
    // public weaponStats sourceWeapon;
    // public bool sourceWasGroundPickup;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        hasAudioManager = audioManager.instance != null;

        if (type == DamageType.bullet)
            if (rb == null && !TryGetComponent<Rigidbody>(out rb))
                rb = gameObject.AddComponent<Rigidbody>();

        if (type == DamageType.DOT && hasAudioManager)
        {
            if (sfx != null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.clip = sfx;
                sfxSource.volume = .4f * audioManager.instance.masterVolume;
                sfxSource.loop = true;
                sfxSource.spatialBlend = 1f;
                sfxSource.minDistance = 1f;
                sfxSource.maxDistance = 25f;
                sfxSource.Play();
            }
        }
    }

    void FixedUpdate()
    {
        if (type == DamageType.bullet)
        {
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * bulletSpeed;
            Destroy(gameObject, bulletDestroyTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (type != DamageType.throwable || hasHit) return;

        Collider other = collision.collider;
        if (other.isTrigger || other.CompareTag("Player")) return;

        hasHit = true;
        Vector3 hitPoint = collision.contacts[0].point;
        handleGlassShatter(other, hitPoint);
        handleDamageAndEffects(other);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        if (type == DamageType.bullet && ((1 << other.gameObject.layer) & deflectLayer) != 0)
        {
            DeflectBullet(other);
            return;
        }

        if (type == DamageType.bullet)
        {
            if (isExplosive) explode();
            else if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        handleGlassShatter(other, hitPoint);
        handleDamageAndEffects(other);
        if (type != DamageType.DOT && type != DamageType.stationary)
            Destroy(gameObject);
    }

    void handleGlassShatter(Collider other, Vector3 hitPoint)
    {
        glassShatter glass = other.GetComponent<glassShatter>() ?? other.GetComponentInParent<glassShatter>();
        if (glass != null)
        {
            glass.Shatter(hitPoint, transform.forward, shatterForce);
            if (hasAudioManager)
                audioManager.instance.playSpatialSFX(audioManager.instance.pickRandomAudio(audioManager.instance.glass), transform.position, audioManager.instance.glassVol);
        }
    }

    void handleDamageAndEffects(Collider other)
    {
        // Deal damage
        if (type != DamageType.DOT)
        {
            IDamage dmg = other.GetComponent<IDamage>();
            if (dmg != null)
                dmg.takeDamage(damageAmount);
        }

        // Play SFX
        if (hasAudioManager)
        {
            bool isEnemy = other.gameObject.layer == enemyLayer;
            if (isEnemy)
                audioManager.instance.playSpatialSFX(audioManager.instance.pickRandomAudio(audioManager.instance.enemyHit), transform.position, audioManager.instance.enemyHitVol);
            else
                audioManager.instance.playSpatialSFX(audioManager.instance.pickRandomAudio(audioManager.instance.wallHit), transform.position, audioManager.instance.wallHitVol);
        }
    }

    // DOT damage, we do not use it right now
    void OnTriggerStay(Collider other)
    {
        if (other.isTrigger) return;

        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null && type == DamageType.DOT && !isDamaging)
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

    void DeflectBullet(Collider other)
    {
        Vector3 direction = rb.linearVelocity.normalized;
        Ray ray = new Ray(transform.position - direction, direction);
        if (other.Raycast(ray, out RaycastHit hit, 2f))
        {
            // Calculate the reflection vector based on current velocity and surface normal
            Vector3 reflectedVelocity = Vector3.Reflect(rb.linearVelocity, hit.normal);
            if (hasAudioManager)
                audioManager.instance.playSpatialSFX(audioManager.instance.pickRandomAudio(audioManager.instance.bulletRicochet), transform.position, audioManager.instance.bulletRicochetVol);

            transform.forward = reflectedVelocity.normalized;

            // Apply the new velocity
            rb.linearVelocity = reflectedVelocity;
        }
    }

    void explode()
    {
        if (hasAudioManager)
            audioManager.instance.playSpatialSFX(audioManager.instance.pickRandomAudio(audioManager.instance.explosion), transform.position, audioManager.instance.explosionVol);

        // Spawn explosion particle effect
        if (explosionEffect != null)
        {
            ParticleSystem explodeFx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explodeFx.gameObject, 1.9f);
        }

        // Query nearby colliders within explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            //Apply physics knockback force
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null)
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }
    }
}