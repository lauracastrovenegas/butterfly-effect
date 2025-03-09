using UnityEngine;

public class WaypointSystem : MonoBehaviour
{
    [Header("Waypoint Configuration")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Color waypointColor = Color.yellow;
    [SerializeField] private float waypointSize = 0.3f;
    [SerializeField] private bool showDirectionIndicator = true;
    [SerializeField] private float directionIndicatorLength = 0.5f;
    
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        
        // Draw spheres at each waypoint
        Gizmos.color = waypointColor;
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null) continue;
            
            Gizmos.DrawSphere(waypoint.position, waypointSize);
            
            // Draw direction indicator
            if (showDirectionIndicator)
            {
                Gizmos.color = Color.blue;
                Vector3 direction = waypoint.forward * directionIndicatorLength;
                Gizmos.DrawLine(waypoint.position, waypoint.position + direction);
                Gizmos.color = waypointColor;
            }
        }
        
        // Draw lines connecting the waypoints (optional)
        Gizmos.color = new Color(waypointColor.r, waypointColor.g, waypointColor.b, 0.5f);
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i+1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
            }
        }
        
        // Connect last waypoint to first to form a loop (optional)
        if (waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length-1] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length-1].position, waypoints[0].position);
        }
    }
    
    // Get a random waypoint
    public Transform GetRandomWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return null;
        return waypoints[Random.Range(0, waypoints.Length)];
    }
    
    // Get all waypoints
    public Transform[] GetAllWaypoints()
    {
        return waypoints;
    }
    
    // Get waypoint by index
    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length) return null;
        return waypoints[index];
    }
    
    // Get waypoint count
    public int GetWaypointCount()
    {
        if (waypoints == null) return 0;
        return waypoints.Length;
    }
}