using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 一次性清理：移除场景里的开发期调试组件
    /// （DebugEnemyKiller 按 K 杀敌、旧版 EnemySpawner 等）。
    /// 用法：Tools > Tower Defense > Debug > Remove Debug Components (One Time)
    /// </summary>
    public static class TDRemoveDebugComponents
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Tower Defense/Debug/Remove Debug Components (One Time)", priority = 31)]
        public static void RemoveDebugComponents()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string originalScenePath = SceneManager.GetActiveScene().path;

            int removed = 0;
            removed += CleanScene(MainScenePath);
            removed += CleanScene(MainMenuScenePath);

            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Debug Cleanup",
                removed > 0
                    ? "已移除 " + removed + " 个调试组件。"
                    : "场景里没有找到需要移除的调试组件。",
                "OK");
        }

        private static int CleanScene(string scenePath)
        {
            if (!File.Exists(ToAbsolutePath(scenePath)))
                return 0;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int removed = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                DebugEnemyKiller[] killers =
                    root.GetComponentsInChildren<DebugEnemyKiller>(true);
                foreach (DebugEnemyKiller killer in killers)
                {
                    Object.DestroyImmediate(killer);
                    removed++;
                }

                EnemySpawner[] spawners =
                    root.GetComponentsInChildren<EnemySpawner>(true);
                foreach (EnemySpawner spawner in spawners)
                {
                    Object.DestroyImmediate(spawner.gameObject);
                    removed++;
                }
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return removed;
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }
    }
}
