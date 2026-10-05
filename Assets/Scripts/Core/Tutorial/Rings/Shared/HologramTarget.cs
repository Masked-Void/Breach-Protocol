using UnityEngine;
using UnityEngine.Events;

// a one hit target for rings 2 and 3. real bullets and thrown guns hit it through IDamage,
// and it never counts as a kill, so no bytes, score or challenge progress
[RequireComponent(typeof(Collider))]
public class HologramTarget : MonoBehaviour,IDamage
{
    [Header("Parts")]
    [SerializeField] private GameObject visual;

    [SerializeField] private ParticleSystem burstEffect;

    public UnityEvent<HologramTarget> Killed = new UnityEvent<HologramTarget>();

    private Collider hitCollider;

    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        isDead = true;
        setShown(false);

        if (burstEffect != null)
        {
            burstEffect.Play();
        }

        Killed.Invoke(this);
    }

    public void ResetTarget()
    {
        isDead = false;
        setShown(true);
    }

    public void Hide()
    {
        isDead = true;
        setShown(false);
    }

    private void setShown(bool isShown)
    {
        if (visual != null)
        {
            visual.SetActive(isShown);
        }

        if (hitCollider != null)
        {
            hitCollider.enabled = isShown;
        }
    }
}
