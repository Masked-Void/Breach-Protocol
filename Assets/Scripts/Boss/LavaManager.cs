using System.Collections;
using UnityEngine;

/*
 * Script: LavaManager
 *
 * Description:
 * Moves a lava object between a low and a high marker, and damages the player
 * while they're standing in it. Runs on unscaled time so the lava keeps rising
 * even while the player has frozen time.
 *
 * Responsibilities:
 * - Rise, drain, or move to any 0 to 1 level over a set duration
 * - Tick damage on the player while they're below the surface
 * - Expose the current level and surface height for other systems
 *
 * Interacts With:
 * - TrapManager (sets the level per boss phase)
 * - IDamage (damages the player)
 * - MarkerUtility (finds the low and high markers by name)
 */
public class LavaManager : MonoBehaviour {

    [Header("Drag in Lava")]
    [Tooltip("the lava surface object, moved between the two markers below")]
    [SerializeField] Transform lavaObject;

    [Header("Marker names (children of the lava object)")]
    [Tooltip("child marking the drained position, found by name so it can be moved in the scene")]
    [SerializeField] string lowMarkerName = "Low";

    [Tooltip("child marking the full position")]
    [SerializeField] string highMarkerName = "High";

    [Header("Motion (Uses unscaledDeltaTime)")]
    [Tooltip("Seconds for a full drained-to-full rise.")]
    [SerializeField] float riseTime = 25f;
    [Tooltip("Seconds for a full drain.")]
    [SerializeField] float drainTime = 4f;

    [Header("Damage")]
    [Tooltip("seconds between damage ticks while the player is standing in it")]
    [SerializeField] private float damageRate = 2f;

    [Tooltip("how far below the surface counts as being in the lava, stops ankle deep hurting")]
    [SerializeField] private float damageDepth = 0.2f;
    [Tooltip("tag used to find the player on Awake")]
    [SerializeField] private string playerTag = "Player";

    private Transform player;
    private IDamage playerDamage;
    private float nextTick;
    private Renderer lavaSurface;

    // The drained (0) and full (1) points the lava lerps between
    private Transform lowPos;
    private Transform highPos;

    // How far along the low -> high trip the lava currently is, 0 to 1
    private float currentProgress = 0f;

    // The active move routine, null when the lava isn't moving
    private Coroutine lavaRoutine;

    // Sends the lava all the way up, uses riseTime
    [ContextMenu("Rise")]
    public void Rise() {
        MoveTo(1f);
    }

    // Sends the lava all the way down, uses drainTime
    [ContextMenu("Drain")]
    public void Drain() {
        MoveTo(0f);
    }

    // Snaps the lava back to the low marker with no animation, used for resets
    [ContextMenu("Reset To Drained")]
    public void ResetToDrained() {
        SetNow(0f);
    }

    // Read only 0 to 1 progress for anything that needs to know how full the arena is
    public float CurrentLevel => currentProgress;



    // Read only world height of the lava surface, for damage/height checks
    public float CurrentSurfaceY => lavaObject.position.y;

    void Awake() {

        // Makes sure the lava object exists
        if (lavaObject == null) {
            Debug.LogError("LavaManager: lavaObject isn't assigned." , this);
            enabled = false;
            return;
        }

        // Assigns the transform positions with "findMark"
        lowPos = findMark(lowMarkerName);
        highPos = findMark(highMarkerName);

        // Makes sure they exist
        if (lowPos == null || highPos == null) {
            Debug.LogError("LavaManager: '" + lavaObject.name + "' needs two children named '"
                + lowMarkerName + "' and '" + highMarkerName + "'." , lavaObject);
            enabled = false;
            return;
        }

        // Reparents the markers onto this object so they stay put when the lava moves
        lowPos.SetParent(transform , true);
        highPos.SetParent(transform , true);

        lavaSurface = lavaObject.GetComponentInChildren<Renderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj!= null) {
            player = playerObj.transform;
            playerDamage = playerObj.GetComponent<IDamage>();
        }

        if (playerDamage == null) {
            Debug.LogError("LavaManager: nothing tagged '" + playerTag + "' with an IDamage on it" , this);
        }

        // Puts the lava at whatever currentProgress starts at
        placeLava();
    }


    void Update() {
        if (playerDamage == null) {
            return;
        }

        if (!checkInLava()) {
            nextTick = 0f;
            return;
        }

        if (Time.unscaledTime < nextTick) {
            return;
        }

        nextTick = Time.unscaledTime + (1f / Mathf.Max(0.01f , damageRate));
        playerDamage.TakeDamage(1);
    }

    // depth check against the surface height rather than a trigger, so the lava
    // can be any shape and the check still works
    bool checkInLava() {
        if (player.position.y > CurrentSurfaceY - damageDepth) {
            return false;
        }

        if (lavaSurface != null) {
            Bounds bound = lavaSurface.bounds;

            if (player.position.x < bound.min.x || player.position.x > bound.max.x) {
                return false;
            }

            if (player.position.z < bound.min.z||player.position.z> bound.max.z) {
                return false;
            }
        }

        return true;
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
                    Debug.LogWarning("LavaManager: '" + lavaObject.name + "' has more than one child matching '"
                        + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first." , lavaObject);
                    break;
                }

                found = child;
            }
        }

        // Returns null if nothing matched, Awake handles that error
        return found;
    }



    // starts a move to a 0 to 1 target, cancelling whatever move was running.
    // duration comes from riseTime or drainTime depending on direction.

    public void MoveTo(float amt) {
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
    public void SetNow(float amt) {

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



    // writes the lava to a height with no animation, used on reset and by SetNow
    void placeLava() {
        lavaObject.position = Vector3.Lerp(lowPos.position , highPos.position , currentProgress);
    }

}