using UnityEngine;
using System.Collections;

public class Damage : MonoBehaviour
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

    [Header("Explosion Shake")]
    [SerializeField] float maxShakeDistance = 20f;
    [SerializeField] float shakeDuration = 0.3f;
    [SerializeField] Vector3 maxShakeStrength = new Vector3(0.25f, 0.25f, 0.25f);

    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip sfx;

    [HideInInspector] public WeaponStats sourceWeapon;

    public bool isExplosive;


    bool isDamaging;
    bool hasHit = false;
    int enemyLayer;
    bool hasAudioManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        hasAudioManager = AudioManager.instance != null;

        if (type == DamageType.bullet)
            if (rb == null && !TryGetComponent<Rigidbody>(out rb))
                rb = gameObject.AddComponent<Rigidbody>();

        if (type == DamageType.DOT && hasAudioManager)
        {
            if (sfx != null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.clip = sfx;
                sfxSource.volume = .4f * AudioManager.instance.masterVolume;
                sfxSource.loop = true;
                sfxSource.spatialBlend = 1f;
                sfxSource.minDistance = 1f;
                sfxSource.maxDistance = 25f;
                sfxSource.Play();
            }
        }
    }

    void Update()
    {
        if (sfxSource != null && type == DamageType.DOT)
        {
            if (GameManager.instance != null && GameManager.instance.isPaused)
            {
                if (sfxSource.isPlaying)
                    sfxSource.Pause();
            }
            else
            {
                if (!sfxSource.isPlaying && sfxSource.time > 0)
                    sfxSource.UnPause();
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
            deflectBullet(other);
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
        GlassShatter glass = other.GetComponent<GlassShatter>() ?? other.GetComponentInParent<GlassShatter>();
        if (glass != null)
        {
            glass.Shatter(hitPoint, transform.forward, shatterForce);
            if (hasAudioManager)
                AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.glass), transform.position, AudioManager.instance.glassVol);
        }
    }

    void handleDamageAndEffects(Collider other)
    {
        // Deal damage
        if (type != DamageType.DOT)
        {
            if (sourceWeapon != null)
            {
                EnemyBase eb = other.GetComponent<EnemyBase>();
                if (eb == null) eb = other.GetComponentInParent<EnemyBase>();
                if (eb != null) eb.RegisterDamageSource(sourceWeapon, sourceWeapon.isFromGround);
            }

            IDamage dmg = other.GetComponent<IDamage>();
            if (dmg != null)
                dmg.TakeDamage(damageAmount);
        }

        // Play SFX
        if (hasAudioManager)
        {
            bool isEnemy = other.gameObject.layer == enemyLayer;
            if (isEnemy)
                AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.enemyHit), transform.position, AudioManager.instance.enemyHitVol);
            else
                AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.wallHit), transform.position, AudioManager.instance.wallHitVol);
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
        d.TakeDamage(damageAmount);
        yield return new WaitForSeconds(damageRate);
        isDamaging = false;
    }

    void deflectBullet(Collider other)
    {
        Vector3 direction = rb.linearVelocity.normalized;
        Ray ray = new Ray(transform.position - direction, direction);
        if (other.Raycast(ray, out RaycastHit hit, 2f))
        {
            // Calculate the reflection vector based on current velocity and surface normal
            Vector3 reflectedVelocity = Vector3.Reflect(rb.linearVelocity, hit.normal);
            if (hasAudioManager)
                AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.bulletRicochet), transform.position, AudioManager.instance.bulletRicochetVol);

            transform.forward = reflectedVelocity.normalized;

            // Apply the new velocity
            rb.linearVelocity = reflectedVelocity;
        }
    }

    void explode()
    {
        if (hasAudioManager)
            AudioManager.instance.PlaySpatialSFX(AudioManager.instance.PickRandomAudio(AudioManager.instance.explosion), transform.position, AudioManager.instance.explosionVol);

        // Spawn explosion particle effect
        if (explosionEffect != null)
        {
            ParticleSystem explodeFx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explodeFx.gameObject, 1.9f);
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            float distance = Vector3.Distance(transform.position, mainCam.transform.position);
            if (distance < maxShakeDistance)
            {
                // Intensity drops off linearly as distance increases
                float intensity = 1f - (distance / maxShakeDistance);
                StartCoroutine(cameraShake(mainCam, maxShakeStrength * intensity, shakeDuration));
            }
        }

        // Query nearby colliders within explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            //Apply physics knockback force
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null)
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null) StartCoroutine(delayDamage(dmg));
        }
    }

    IEnumerator delayDamage(IDamage dmg)
    {
        yield return new WaitForFixedUpdate();
        dmg.TakeDamage(explosionDamage);
    }

    IEnumerator cameraShake(Camera cam, Vector3 strength, float duration)
    {
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp01(percentComplete); // Smooth fade out

            Vector3 randomOffset = Vector3.Scale(Random.insideUnitSphere, strength) * damper;
            cam.transform.localPosition = originalPos + randomOffset;

            yield return null;
        }

        cam.transform.localPosition = originalPos;
    }
}