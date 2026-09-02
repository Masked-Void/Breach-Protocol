using System.Collections;
using UnityEngine;

// Spawn room door that slides between a closed and an open marker.
// Opens when an enemy that hasn't left the spawn room walks into the trigger,
// then closes again once the trigger is empty.
public class DoorController : MonoBehaviour
{
    [Header("Drag door Obj in")]
    [SerializeField] Transform doorObject;

    [Header("Marker names (children of the door object)")]
    [SerializeField] string closedMarkerName = "Closed";
    [SerializeField] string openMarkerName = "Open";

    [Header("Speed")]
    [Tooltip("Seconds for a full closed-to-open move.")]
    [SerializeField] float movementTime = 1;

    [Header("Auto close")]
    [Tooltip("Closes the door on its own once nothing is left in the trigger.")]
    [SerializeField] bool closeWhenEmpty = true;
    [Tooltip("Seconds to wait before auto closing.")]
    public float closeDelay = 1f;

    // Sends the door all the way open
    [ContextMenu("Open")]
    public void open()
    {
        moveTo(1f);
    }

    // Sends the door all the way closed
    [ContextMenu("Close")]
    public void close()
    {
        moveTo(0f);
    }

    // The closed (0) and open (1) points the door lerps between
    Transform closedPos;
    Transform openPos;

    // How far along the closed -> open trip the door currently is, 0 to 1
    float currentProgress;

    // The active move routine, null when the door isn't moving
    Coroutine doorRoutine;

    // How many enemies are sitting in the trigger right now
    int insideCount = 0;



    // Read only so other scripts can check the door without moving it
    public bool getIsOpen
    {
        get
        {
            return currentProgress >= 1f;
        }
    }



    // Sets up the markers before anything tries to open the door
    void Awake()
    {
        // Makes sure the door object exists
        if (doorObject == null)
        {
            //Debug.LogError("DoorController: doorObject isn't assigned", this);
            enabled = false;
            return;
        }

        // Assigns the transform positions with "findMark"
        closedPos = findMark(closedMarkerName);
        openPos = findMark(openMarkerName);

        // Makes sure they exist
        if (closedPos == null || openPos == null)
        {
            //Debug.LogError("DoorController: '" + doorObject.name + "' needs two children named '"
                //+ closedMarkerName + "' and '" + openMarkerName + "'.", doorObject);
            enabled = false;
            return;
        }

        enabled = true;

        // Reparents the markers onto this object so they stay put when the door moves
        closedPos.SetParent(transform, true);
        openPos.SetParent(transform, false);

        // Puts the door at whatever currentProgress starts at
        placeDoor();
    }



    // Looks through the door object's children for a marker with an exact name match
    Transform findMark(string wanted)
    {
        // Goes through each child of the door object
        foreach (Transform child in doorObject)
        {
            if (child.name.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child;
            }
        }

        // Returns null if nothing matched, Awake handles that error
        return null;
    }



    void OnTriggerEnter(Collider other)
    {

        // Ignores the player, bullets and anything else that isn't a spawning enemy
        if (!isEnemy(other))
        {
            return;
        }

        insideCount += 1;
        open();

    }



    void OnTriggerExit(Collider other)
    {
        // Same filter as the enter check so the count stays even
        if (!isEnemy(other))
        {
            return;
        }

        insideCount -= 1;

        // Safety net for if an enemy gets destroyed inside the trigger and never fires an exit
        if (insideCount < 0)
        {
            insideCount = 0;
        }

        // Only starts the close timer once the last enemy is out
        if (closeWhenEmpty && insideCount == 0)
        {
            StartCoroutine(closeAfterDelay());
        }

    }



    // Only counts enemies that are still on their way out of the spawn room
    bool isEnemy(Collider other)
    {
        // tag check first since it's the cheapest
        if (!other.CompareTag("Enemy"))
        {
            return false;
        }

        // Makes sure it actually has the enemy script and isn't just tagged
        EnemyBase enemy;
        if (!other.TryGetComponent(out enemy))
        {
            return false;
        }

        // Enemies already out in the level shouldn't reopen the spawn door
        return !enemy.hasLeftSpawnRoom;
    }



    // Waits out the delay then closes, unless something walked back in
    IEnumerator closeAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);

        // rechecks because an enemy could have entered while this was waiting
        if (insideCount == 0)
        {
            close();
        }
    }



    // method call for gradual door open/close
    public void moveTo(float amt)
    {
        // error check
        if (!enabled)
        {
            return;
        }

        // Makes sure that: amt >= 0 && amt is <= 1
        amt = Mathf.Clamp01(amt);

        // stops the routine for if it needs to be restarted
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

        // starts the new move from wherever the door currently sits
        doorRoutine = StartCoroutine(moveDoor(amt));
    }



    // Lerps currentProgress over to the target over time, then keeps the door updated each frame
    IEnumerator moveDoor(float target)
    {
        // Gets the current progress for if a routine is started while another routine is already running
        float currentStartPos = currentProgress;

        // gets the distance from current pos
        float distanceToTravel = Mathf.Abs(target - currentStartPos);

        // gets the time needed in relation to the current distance traveled
        float duration = movementTime * distanceToTravel;

        float timePassed = 0f;

        // waits a frame so a bunch of triggers firing at once only ends up starting one move
        yield return null;

        while (timePassed < duration)
        {
            // Scaled so the door freezes with everything else, capped at 0.05 so a lag spike can't teleport it
            timePassed += Mathf.Min(Time.deltaTime, 0.05f);

            // 0 to 1 of how much of the trip is done
            float howFar = Mathf.Clamp01(timePassed / duration);
            currentProgress = Mathf.Lerp(currentStartPos, target, howFar);

            placeDoor();

            yield return null;
        }

        // Snaps to the exact target so float drift doesn't leave it slightly off
        currentProgress = target;
        placeDoor();

        // clears routine
        doorRoutine = null;
    }



    // Updates door position
    void placeDoor()
    {
        doorObject.position = Vector3.Lerp(closedPos.position, openPos.position, currentProgress);
    }
}