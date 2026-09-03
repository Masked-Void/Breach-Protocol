using UnityEngine;

/*
 * Script: PatrolPath
 *
 * Description:
 * A fixed route of waypoints an enemy walks. Either loops back to the start or
 * bounces back along itself. Draws the route as gizmos so it can be laid out
 * in the scene view.
 *
 * Interacts With:
 * - RangedEnemy (the only enemy that patrols)
 *
 * Notes:
 * - NextWaypointIndex advances state, so it is a method not a property. Calling
 *   it twice moves two waypoints along.
 */
public class PatrolPath : MonoBehaviour
{
    // loop wraps back to the first point, reverse walks the route backwards
    public enum PathType
    {
        Loop,
        ReverseWhenDone
    }

    [Tooltip("the route in order, drop empty objects in the scene and drag them here")]
    [SerializeField] Transform[] patrolPoints;

    [Tooltip("what happens at the end of the route")]
    public PathType pathType = PathType.Loop;

    // 1 walking forward, -1 after a reverse path turns around
    int direction = 1;

    // which waypoint we're currently at
    int idx;

    // steps to the next waypoint and returns its index. advances state, so
    // calling this twice skips one.
    public int NextWaypointIndex()
    {
        idx += direction;

        if (pathType == PathType.Loop)
            idx = ((idx % patrolPoints.Length) + patrolPoints.Length) % patrolPoints.Length;
        else if (pathType == PathType.ReverseWhenDone)
        {
            if (idx >= patrolPoints.Length || idx < 0)
            {
                direction *= -1;
                idx += direction * 2;
            }
        }

        return idx;
    }

    // where the enemy is headed right now, no state change
    public Vector3 CurrentWaypointPosition => patrolPoints[idx].position;

    // advances to the next waypoint and returns where it is
    public Vector3 NextWaypointPosition()
    {
        if (patrolPoints.Length == 0)
            return transform.position;
        idx = NextWaypointIndex();
        Vector3 nextWayPoint = patrolPoints[idx].position;

        return nextWayPoint;
    }

    // draws the route in the scene view so it can be laid out visually
    void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.white;

        for (int i = 0; i < patrolPoints.Length - 1; i++)
            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);

        if (pathType == PathType.Loop)
            Gizmos.DrawLine(patrolPoints[^1].position, patrolPoints[0].position);

        Gizmos.color = Color.red;

        foreach (Transform patrolPoint in patrolPoints)
            Gizmos.DrawSphere(patrolPoint.position, .3f);
    }
}
