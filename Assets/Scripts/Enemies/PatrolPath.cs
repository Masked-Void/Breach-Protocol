using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    public enum PathType
    {
        Loop,
        ReverseWhenDone
    }

    [SerializeField] Transform[] patrolPoints;

    public PathType pathType = PathType.Loop;

    int direction = 1;
    int idx;

    public int NextWaypointIndex()
    {
        idx += direction;

        if (pathType == PathType.Loop) idx = ((idx % patrolPoints.Length) + patrolPoints.Length) % patrolPoints.Length;
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

    public Vector3 CurrentWaypointPosition => patrolPoints[idx].position;

    public Vector3 NextWaypointPosition()
    {
        if (patrolPoints.Length == 0) return transform.position;
        idx = NextWaypointIndex();
        Vector3 nextWayPoint = patrolPoints[idx].position;

        return nextWayPoint;
    }

    void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

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
