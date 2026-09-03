using UnityEngine;

// one roaming destination in a level. enemies claim a point so two of them
// don't walk to the same spot, and release it when they move on or die.
public class RoamPoint
{
    // where the enemy walks to
    public Transform point;

    // the enemy that has claimed this point, null when nobody has it
    public GameObject claimedBy;

    public bool isFree
    {
        get { return claimedBy == null; }
    }
}