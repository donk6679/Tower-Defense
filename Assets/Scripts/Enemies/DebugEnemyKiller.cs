using UnityEngine;

/// <summary>
/// 开发期临时工具：按 K 消灭场上任意一只敌人，
/// 用来在塔还没做出来之前验证“敌人死亡事件 + 死亡特效反馈”。
/// 塔系统完成后删除本组件即可。
/// </summary>
public sealed class DebugEnemyKiller : MonoBehaviour
{
    [SerializeField] private KeyCode killKey = KeyCode.K;

    private void Update()
    {
        if (!Input.GetKeyDown(killKey))
            return;

        Enemy target = FindObjectOfType<Enemy>();
        if (target == null)
        {
            Debug.Log("[Debug] 场上没有存活敌人，按 K 无效");
            return;
        }

        Debug.Log("[Debug] 按 K 消灭敌人：" + target.name);
        target.TakeDamage(99999);
    }
}
