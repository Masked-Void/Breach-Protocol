using UnityEngine;

public class RoamPoint
{
    public Transform point;
    public GameObject claimedBy;

    public bool isFree
    {
        get { return claimedBy == null; }
    }
}