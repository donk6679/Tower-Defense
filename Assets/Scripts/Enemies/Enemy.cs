using System;
using UnityEngine;

/// <summary>
/// 敌人：沿着 PathManager 提供的路径点按顺序移动。
/// 由生成器实例化后调用 Initialize(pathManager) 即可运行。
/// </summary>
public sealed class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    private PathManager pathManager;
    private int nextWaypointIndex = 1;

    /// <summary>敌人到达核心时触发（后续生命系统会在这里扣生命）。</summary>
    public event Action<Enemy> ReachedBase;

    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// 绑定路径。出生位置为路径的第一个点，因此从第二个点开始寻找目标。
    /// </summary>
    public void Initialize(PathManager path)
    {
        pathManager = path;
        nextWaypointIndex = 1;
    }

    private void Update()
    {
        if (pathManager == null || pathManager.WaypointCount == 0)
            return;

        // 已经走完所有路径点：到达核心
        if (nextWaypointIndex >= pathManager.WaypointCount)
        {
            ReachBase();
            return;
        }

        Transform target = pathManager.GetWaypoint(nextWaypointIndex);
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        float step = moveSpeed * Time.deltaTime;

        // 一步之内到达：直接吸附到路径点，避免来回抖动
        if (direction.magnitude <= step)
        {
            transform.position = target.position;
            nextWaypointIndex++;
            return;
        }

        transform.position += direction.normalized * step;
    }

    private void ReachBase()
    {
        Debug.Log("[Enemy] 到达核心，本次测试敌人已销毁", this);
        ReachedBase?.Invoke(this);
        Destroy(gameObject);
    }
}
