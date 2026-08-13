using System.Collections;
using UnityEngine;

// Handles a group of lasers that slide from a hidden spot out into the arena.
// Each laser is a child of this object and moves between its own two markers.
// Beams stay off while moving and only switch on once a laser is fully out.
public class laserArray : MonoBehaviour
{

    [Header("Marker Names: (Children of each laser)")]
    [SerializeField] string laserInMarkerName = "laserIn";
    [SerializeField] string laserOutMarkerName = "laserOut";

    [Header("Deploy Motion")]
    [Tooltip("Seconds for one laser to go from fully in to fully out.")]
    [SerializeField] float deployTime = 1f;
    [Tooltip("Seconds between each laser starting its move, 0 makes them all move at once.")]
    [SerializeField] float stagger = 0.5f;

    // Sends every laser out, staggered
    [ContextMenu("Deploy")]
    public void deploy()
    {
        moveAll(true);
    }

    // Pulls every laser back in, staggered
    [ContextMenu("Retract")]
    public void retract()
    {
        moveAll(false);
    }

    // Everything one laser needs to move on its own, built once in build()
    class laserUnit
    {
        public Transform laser;
        public Transform laserInPos;
        public Transform laserOutPos;
        public Collider[] beams;
        public float currentProgress;
        public Coroutine moveRoutine;
    }

    // Every laser under this object, filled in by build()
    laserUnit[] lasers;

    // The routine that walks down the array with the stagger delay
    Coroutine groupRoutine;

    // Which way the array was last told to go, not whether it finished moving
    bool isOut = false;

    // Read only so other scripts can check the array state without changing it
    public bool getIsOut
    {
        get
        {
            return isOut;
        }
    }

    // True only when every laser is all the way out and the beams are live
    // The manager checks this instead of getIsOut, a half deployed array shouldn't count as firing
    public bool getIsDeployed
    {
        get
        {
            if (lasers == null || lasers.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < lasers.Length; i++)
            {
                if (lasers[i].currentProgress < 1f)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // True while anything in here is still sliding, group or single
    public bool getIsMoving
    {
        get
        {
            if (groupRoutine != null)
            {
                return true;
            }

            if (lasers == null)
            {
                return false;
            }

            for (int i = 0; i < lasers.Length; i++)
            {
                if (lasers[i].moveRoutine != null)
                {
                    return true;
                }
            }

            return false;
        }
    }



    // How many lasers ended up under this object
    public int getCount
    {
        get
        {
            return lasers == null ? 0 : lasers.Length;
        }
    }


    private void Awake()
    {
        build();
    }



    // Grabs every child laser and sets up its markers, beams and starting state
    void build()
    {

        int count = transform.childCount;
        lasers = new laserUnit[count];

        // Caches the children first because reparenting markers later shifts the child order
        Transform[] children = new Transform[count];

        for (int i = 0; i < count; i++)
        {

            children[i] = transform.GetChild(i);

        }

        for (int i = 0; i < count; i++)
        {
            Transform laser = children[i];

            laserUnit newUnit = new laserUnit();

            newUnit.laser = laser;
            newUnit.laserInPos = findMark(laser, laserInMarkerName);
            newUnit.laserOutPos = findMark(laser, laserOutMarkerName);
            newUnit.currentProgress = 0f;

            // Makes sure both markers exist, one missing marker kills the whole array
            if (newUnit.laserInPos == null || newUnit.laserOutPos == null)
            {
                Debug.LogError("laserArray: '" + laser.name + "' needs two children named '"
                + laserInMarkerName + "' and '" + laserOutMarkerName + "'.", laser);

                // Blanks the array so nothing can walk into the half filled entries later
                lasers = new laserUnit[0];
                enabled = false;
                return;
            }

            // Grabs every collider on the laser, true includes ones that start disabled
            newUnit.beams = laser.GetComponentsInChildren<Collider>(true);

            // Reparents the markers onto this object so they stay put when the laser moves
            newUnit.laserInPos.SetParent(transform, true);
            newUnit.laserOutPos.SetParent(transform, true);

            lasers[i] = newUnit;

            // Starts with the beams off since the laser starts hidden
            setBeam(newUnit, false);
        }
    }



    // Looks through one laser's children for a marker whose name contains 'wanted'
    Transform findMark(Transform laser, string wanted)
    {
        // empty variable for 1 return call
        Transform found = null;

        // Goes through each child of that laser
        foreach (Transform child in laser)
        {
            // If the child has that wanted name it assigns it to the found object
            if (child.name.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Warning for if more than one child matches, it keeps the first one and stops looking
                if (found != null)
                {
                    Debug.LogWarning("laserArray: '" + laser.name + "' has more than one child matching '"
                        + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first.", laser);
                    break;
                }

                found = child;
            }
        }

        // Returns null if nothing matched, build() handles that error
        return found;
    }



    // Reapplies every laser position after everything else has moved for the frame
    private void LateUpdate()
    {
        // error check
        if (!enabled)
        {
            return;
        }

        for (int i = 0; i < lasers.Length; i++)
        {
            placeLaser(lasers[i]);
        }
    }



    // Moves a single laser by its index, for when only one needs to fire
    public void moveOne(int index, bool goOut)
    {
        // error check
        if (!enabled)
        {
            return;
        }

        // Silently skips a bad index, the manager does the same so a stale pattern step just does nothing
        if (index < 0 || index >= lasers.Length)
        {
            return;
        }

        startMove(lasers[index], goOut);
    }

    // Pulls everything in at once with no stagger, for when a phase ends
    // retract() walks down the array with a delay, so lasers it hasn't reached yet keep deploying
    public void retractNow()
    {
        // error check
        if (!enabled)
        {
            return;
        }

        isOut = false;

        // Kills the stagger walk first so it can't start lasers back up behind us
        if (groupRoutine != null)
        {
            StopCoroutine(groupRoutine);
            groupRoutine = null;
        }

        for (int i = 0; i < lasers.Length; i++)
        {
            startMove(lasers[i], false);
        }
    }

    // Moves the whole array one direction with the stagger delay between each laser
    void moveAll(bool goOut)
    {
        // error check
        if (!enabled)
        {
            return;
        }

        isOut = goOut;

        // stops the group routine for if it needs to be restarted mid deploy
        if (groupRoutine != null)
        {
            StopCoroutine(groupRoutine);
        }

        groupRoutine = StartCoroutine(moveGroup(goOut));
    }



    // Kicks off each laser one at a time so they don't all pop out together
    IEnumerator moveGroup(bool goOut)
    {
        for (int i = 0; i < lasers.Length; i++)
        {
            startMove(lasers[i], goOut);

            // Scaled time on purpose so the stagger stops while time is frozen
            if (stagger > 0)
            {
                yield return new WaitForSeconds(stagger);
            }
        }

        // clears routine
        groupRoutine = null;
    }



    // Starts one laser's move, restarting it from wherever it currently sits
    void startMove(laserUnit unit, bool goOut)
    {
        // stops that laser's routine for if it needs to be restarted
        if (unit.moveRoutine != null)
        {
            StopCoroutine(unit.moveRoutine);
        }

        float target = goOut ? 1f : 0f;
        unit.moveRoutine = StartCoroutine(moveLaser(unit, target));
    }



    // Lerps one laser between its markers, then turns the beams back on if it made it all the way out
    IEnumerator moveLaser(laserUnit unit, float target)
    {
        // Beams off while moving so the laser can't hit the player on the way out
        setBeam(unit, false);

        // Gets the current progress for if a routine is started while another routine is already running
        float currentStartPos = unit.currentProgress;

        // gets the distance from current pos
        float distanceToTravel = Mathf.Abs(target - currentStartPos);

        // gets the time needed in relation to the current distance traveled
        float duration = deployTime * distanceToTravel;

        float timePassed = 0f;

        while (timePassed < duration)
        {
            // Scaled so the lasers freeze with everything else, capped at 0.05 so a lag spike can't teleport them
            timePassed += Mathf.Min(Time.deltaTime, 0.05f);

            // 0 to 1 of how much of the trip is done
            float howFar = Mathf.Clamp01(timePassed / duration);

            unit.currentProgress = Mathf.Lerp(currentStartPos, target, howFar);
            placeLaser(unit);

            yield return null;
        }

        // Snaps to the exact target so float drift doesn't leave it slightly off
        unit.currentProgress = target;
        placeLaser(unit);

        // Only turns the beams on when fully out, a retract leaves them off
        if (target >= 1f)
        {
            setBeam(unit, true);
        }

        // clears routine
        unit.moveRoutine = null;
    }



    // Updates one laser's position and rotation
    void placeLaser(laserUnit unit)
    {
        unit.laser.position = Vector3.Lerp(unit.laserInPos.position, unit.laserOutPos.position, unit.currentProgress);
        unit.laser.rotation = Quaternion.Slerp(unit.laserInPos.rotation, unit.laserOutPos.rotation, unit.currentProgress);
    }



    // Turns every collider on one laser on or off
    void setBeam(laserUnit unit, bool on)
    {
        for (int i = 0; i < unit.beams.Length; i++)
        {
            // skips any collider that got destroyed
            if (unit.beams[i] != null)
                unit.beams[i].enabled = on;
        }
    }

}