using UnityEngine;

// Shakes the mask, will do ontop of whatever the base boss is looking at
public class maskShake : MonoBehaviour {

    [Header("Shake")]
    [Tooltip("Lowest random offset")]
    [SerializeField] private Vector3 shakeMin = new Vector3(-8f , -10f , -4f);
    [Tooltip("Highest random offset")]
    [SerializeField] private Vector3 shakeMax = new Vector3(8f , 10f , 4f);

    [Tooltip("How wild it goes")]
    [SerializeField] private float shakeIntensity = 1f;

    [Tooltip("How often it picks a new rotation")]
    [SerializeField] private float shakeInterval = 0.1f;

    [Tooltip("How fast it goes to new rotation")]
    [SerializeField] private float shakeSpeed = 25f;


    [Header("Settle")]
    [SerializeField] private float settleSpeed = 2f;

    [Header("State")]
    public bool doShake = false;

    private Quaternion restRot;
    private Quaternion shakeOffset = Quaternion.identity;
    private float shakeTimer;

    void Awake() {
        // Gets 0,0,0 as of now but the base rotation if we change it
        restRot = transform.localRotation;
    }

    void Update() {
        if (doShake) {
            // Shake based on time
            shakeTimer -= Time.unscaledDeltaTime;

            if (shakeTimer <= 0) {
                // if timer runs out, select a random shake rotation
                shakeTimer = shakeInterval;
                shakeOffset = Quaternion.Euler(randomShake());
            }

            moveTo(restRot * shakeOffset , shakeSpeed);

        } else if (transform.localRotation != restRot) {
            // If not shaking, goes back to rest rotation
            shakeOffset = Quaternion.identity;
            shakeTimer = 0f;
            moveTo(restRot , settleSpeed);
        }
    }

    // Handles the rotation
    private void moveTo(Quaternion target , float speed) {
        float lerpAmount = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation , target , lerpAmount);
    }

    // Gets a random shake rotation
    private Vector3 randomShake() {
        return new Vector3(
            Random.Range(shakeMin.x , shakeMax.x) ,
            Random.Range(shakeMin.y , shakeMax.y) ,
            Random.Range(shakeMin.z , shakeMax.z)
            ) * shakeIntensity;
    }
}
