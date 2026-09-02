using UnityEngine;

// Aims the whole boss at the player, MaskShake will do the shaking for the mask only
public class FaceFollowPlayer : MonoBehaviour {
    [Header("Target")]
    [Tooltip("This is what the boss looks at, if left empty it will grab the object tagged 'Player' on start.")]
    [SerializeField] private Transform followedObject;

    [Header("Follow")]
    [Tooltip("How fast the boss springs to the player.")]
    [SerializeField] private float turnSpeed = 10f;

    //Dont know if this will be used, but thought it would be good to think ahead.
    [Tooltip("Boss tracks the player while on.")]
    public bool follow = true;

    void Awake() {
        // Fall back to player tag for error avoidance
        if (followedObject == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                followedObject = player.transform;
            }

        }
    }



    private void Update() {
        if (!follow || followedObject == null) { return; }

        Vector3 objDir = followedObject.position - transform.position;

        // LookRotation errors on a zero vector. this justs avoids that error
        if (objDir.sqrMagnitude < 0.0001f) { return; }

        Quaternion target = Quaternion.LookRotation(objDir);

        // Damping to avoid drift
        float lerpAmount = 1f - Mathf.Exp(-turnSpeed * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation , target , lerpAmount);
    }

    // Change for follow, still incase we need it.
    public void setFollow(bool on) {
        follow = on;
    }
}
