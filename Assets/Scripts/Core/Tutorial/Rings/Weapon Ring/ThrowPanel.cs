using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

// the security panel in front of ring 2's next gun. bullets can't break it and early throws bounce off,
// only a thrown gun after both holograms are down gets through
[RequireComponent(typeof(Collider))]
public class ThrowPanel : MonoBehaviour
{
    [Header("Breaking")]

    [Tooltip("Slowest impack that breaks it, meters per second")]
    [SerializeField] private float minImpactSpeedMetersPerSecond = 3f;


    [Header("Look")]

    [Tooltip("The whole panel, hidden when it breaks")]
    [SerializeField] private GameObject intactVisual;

    [Tooltip("Shatter effect played where the gun hit")]
    [SerializeField] private ParticleSystem shatterEffect;


    [Header("Events")]
    [Tooltip("fires once when a thrown gun breaks it")]
    public UnityEvent Broken = new UnityEvent();

    private Collider panelCollider;
    private bool isArmed;
    private bool isBroken;

    public bool IsArmed => isArmed;
    public bool IsBroken => isBroken;

    private void Awake()
    {
        panelCollider = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || isBroken)
        {
            return;
        }

        Rigidbody thrown = collision.rigidbody;
        if (thrown == null || !thrown.TryGetComponent(out DroppedWeapon wep))
        {
            return;
        }

        if (collision.relativeVelocity.magnitude <  minImpactSpeedMetersPerSecond)
        {
            return;
        }

        breakPanel(collision.GetContact(0).point);
    }

    public void ArmPanel()
    {
        isArmed = true;
    }

    public void ResetPanel()
    {
        isArmed = false;
        isBroken = false;

        if (intactVisual != null)
        {
            intactVisual.SetActive(true);
        }

        if (panelCollider!= null)
        {
            panelCollider.enabled = true;
        }
    }

    private void breakPanel(Vector3 hitPoint)
    {
        isBroken = true;

        if (intactVisual != null)
        {
            intactVisual.SetActive(false);
        }

        panelCollider.enabled = false;

        if (shatterEffect != null)
        {
            shatterEffect.transform.position = hitPoint;
            shatterEffect.Play();
        }

        AudioManager audioManager = AudioManager.instance;

        if (audioManager != null && audioManager.glass != null && audioManager.glass.Length>0)
        {
            audioManager.PlaySpatialSFX(audioManager.PickRandomAudio(audioManager.glass), hitPoint, audioManager.glassVol);
        }

        Broken.Invoke();
    }
}
