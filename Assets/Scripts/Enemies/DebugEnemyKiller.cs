using UnityEngine;

/// <summary>
/// 开发期调试组件（已停用）。
/// 保留空脚本是为了让场景里已有的组件引用不丢失，
/// 可以在 Unity 里取消勾选，或用
/// Tools > Tower Defense > Debug > Remove Debug Components 从场景中彻底移除。
/// </summary>
public sealed class DebugEnemyKiller : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
    }
}
