using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 编辑器菜单保护：这些生成/配置工具会修改并保存场景，
    /// 因此不允许在 Play 模式中运行；若误触发则自动退出 Play 模式。
    /// </summary>
    public static class TDMenuGuard
    {
        public static bool IsInPlayMode()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            EditorUtility.DisplayDialog(
                "TD Tool",
                "该工具不能在 Play 模式中运行，将自动停止运行并返回编辑器。",
                "OK");

            EditorApplication.isPlaying = false;
            return true;
        }
    }
}
