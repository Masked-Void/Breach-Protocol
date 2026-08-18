using System.Collections;
using UnityEngine;

// Moves a lava object between a low and a high marker.
// Runs on unscaled time so the lava keeps rising even while time is frozen.
public class lavaManager : MonoBehaviour {

    [Header("Drag in Lava")]
    [SerializeField] Transform lavaObject;

    [Header("Marker names (children of the lava object)")]
    [SerializeField] string lowMarkerName = "Low";
    [SerializeField] string highMarkerName = "High";

    [Header("Motion (Uses unscaledDeltaTime)")]
    [Tooltip("Seconds for a full drained-to-full rise.")]
    [SerializeField] float riseTime = 25f;
    [Tooltip("Seconds for a full drain.")]
    [SerializeField] float drainTime = 4f;

    // The drained (0) and full (1) points the lava lerps between
    private Transform lowPos;
    private Transform highPos;

    // How far along the low -> high trip the lava currently is, 0 to 1
    private float currentProgress = 0f;

    // The active move routine, null when the lava isn't moving
    private Coroutine lavaRoutine;

    // Sends the lava all the way up, uses riseTime
    [ContextMenu("Rise")]
    public void rise() {
        moveTo(1f);
    }

    // Sends the lava all the way down, uses drainTime
    [ContextMenu("Drain")]
    public void drain() {
        moveTo(0f);
    }

    // Snaps the lava back to the low marker with no animation, used for resets
    [ContextMenu("Reset To Drained")]
    public void resetToDrained() {
        setNow(0f);
    }

    // Read only 0 to 1 progress for anything that needs to know how full the arena is
    public float getCurrentLevel {
        get {
            return currentProgress;
        }
    }



    // Read only world height of the lava surface, for damage/height checks
    public float getCurrentSurfaceY {
        get {
            return lavaObject.position.y;
        }
    }




    void Awake() {

        // Makes sure the lava object exists
        if (lavaObject == null) {
            Debug.LogError("lavaManager: lavaObject isn't assigned." , this);
            enabled = false;
            return;
        }

        // Assigns the transform positions with "findMark"
        lowPos = findMark(lowMarkerName);
        highPos = findMark(highMarkerName);

        // Makes sure they exist
        if (lowPos == null || highPos == null) {
            Debug.LogError("lavaManager: '" + lavaObject.name + "' needs two children named '"
                + lowMarkerName + "' and '" + highMarkerName + "'." , lavaObject);
            enabled = false;
            return;
        }

        // Reparents the markers onto this object so they stay put when the lava moves
        lowPos.SetParent(transform , true);
        highPos.SetParent(transform , true);

        // Puts the lava at whatever currentProgress starts at
        placeLava();
    }



    // Looks through the lava object's children for one whose name contains 'wanted'
    Transform findMark(string wanted) {
        // empty variable for 1 return call
        Transform found = null;

        // Goes through each element in the lava object
        foreach (Transform child in lavaObject) {
            // If the element has that wanted name it assigns it to the called object
            if (child.name.IndexOf(wanted , System.StringComparison.OrdinalIgnoreCase) >= 0) {
                // Warning for if more than one child matches, it keeps the first one and stops looking
                if (found != null) {
                    Debug.LogWarning("lavaManager: '" + lavaObject.name + "' has more than one child matching '"
                        + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first." , lavaObject);
                    break;
                }

                found = child;
            }
        }

        // Returns null if nothing matched, Awake handles that error
        return found;
    }



    // method call for gradual lava rise/fall
    public void moveTo(float amt) {
        // error check
        if (!enabled)
            return;

        // Makes sure that: amt >= 0 && amt is <= 1
        amt = Mathf.Clamp01(amt);

        // stops the routine for if it needs to be restarted
        if (lavaRoutine != null) {
            StopCoroutine(lavaRoutine);
        }

        // starts the new move from wherever the lava currently sits
        lavaRoutine = StartCoroutine(moveLava(amt));

    }



    // method call for instant lava rise/fall
    public void setNow(float amt) {

        // error check
        if (!enabled)
            return;

        // Stops routine incase its running
        if (lavaRoutine != null) {
            StopCoroutine(lavaRoutine);
            lavaRoutine = null;
        }

        // Makes sure that: amt >= 0 && amt is <= 1
        currentProgress = Mathf.Clamp01(amt);
        // sets lava to amt
        placeLava();

    }



    // Lerps currentProgress over to the target over time, then keeps the lava updated each frame
    IEnumerator moveLava(float target) {
        // Gets the current progress for if a routine is started while another routine is already running
        float currentStartPos = currentProgress;

        // gets the distance from current pos
        float distanceToTravel = Mathf.Abs(target - currentStartPos);

        // checks to see if it should use rise or drain time
        float fullTripTime = (target > currentStartPos) ? riseTime : drainTime;

        // gets the time needed in relation to the current distance traveled
        float duration = fullTripTime * distanceToTravel;

        float timePassed = 0f;

        while (timePassed < duration) {
            // Unscaled so the lava ignores the time freeze, capped at 0.05 so a lag spike can't teleport it
            float timePerStep = Mathf.Min(Time.unscaledDeltaTime , 0.05f);
            timePassed += timePerStep;

            // 0 to 1 of how much of the trip is done
            float howFar = Mathf.Clamp01(timePassed / duration);

            currentProgress = Mathf.Lerp(currentStartPos , target , howFar);
            placeLava();

            yield return null;
        }

        // Snaps to the exact target so float drift doesn't leave it slightly off
        currentProgress = target;
        placeLava();

        // clears routine
        lavaRoutine = null;
    }



    // Updates lava position
    void placeLava() {
        lavaObject.position = Vector3.Lerp(lowPos.position , highPos.position , currentProgress);
    }

}