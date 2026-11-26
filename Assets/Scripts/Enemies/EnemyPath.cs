using UnityEngine;

// The path the enemies walk. Every child of this object is a waypoint,
// and the enemies visit them in the order they appear in the Hierarchy.
public class EnemyPath : MonoBehaviour
{
    Transform[] waypoints;

    public int Count => waypoints != null ? waypoints.Length : 0;

    void Awake()
    {
        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            waypoints[i] = transform.GetChild(i);
    }

    public Transform GetWaypoint(int index)
    {
        return waypoints[index];
    }

    // Draws the path in the Scene view so it is easier to move the waypoints around.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 point = transform.GetChild(i).position;
            Gizmos.DrawSphere(point, 0.25f);
            if (i > 0)
                Gizmos.DrawLine(transform.GetChild(i - 1).position, point);
        }
    }
}
