using UnityEngine;

/// <summary>
/// 路径管理器：持有敌人按顺序行走的路径点。
/// 路径点在 Inspector 中以数组形式配置，顺序即敌人移动顺序。
/// </summary>
public sealed class PathManager : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints = System.Array.Empty<Transform>();

    public Transform[] Waypoints => waypoints;
    public int WaypointCount => waypoints == null ? 0 : waypoints.Length;

    public Vector3 SpawnPosition => WaypointCount > 0 ? waypoints[0].position : Vector3.zero;
    public Vector3 BasePosition => WaypointCount > 0 ? waypoints[waypoints.Length - 1].position : Vector3.zero;

    public Transform GetWaypoint(int index)
    {
        return waypoints[index];
    }

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = new Color(1f, 0.82f, 0.25f, 1f);

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawWireSphere(waypoints[i].position, 0.24f);

            if (i > 0 && waypoints[i - 1] != null)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
}
