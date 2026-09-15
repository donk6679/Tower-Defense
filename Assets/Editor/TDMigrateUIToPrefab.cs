using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 一次性迁移工具：把 Main / MainMenu 场景里已经生成好的 UI
    /// 固化成 Prefab，并用 Prefab 实例替换场景里的 UI 根节点。
    ///
    /// 迁移之后：
    /// - 布局、位置、大小、贴图都直接在 Prefab 里编辑并保存；
    /// - 不再需要反复运行 UI 生成菜单；
    /// - 新功能需要新 UI 元素时，改 Prefab 或写增量脚本即可。
    ///
    /// 用法：Tools > Tower Defense > UI > Freeze UI To Prefabs (One Time)
    /// </summary>
    public static class TDMigrateUIToPrefab
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameUIPrefabPath = "Assets/Prefabs/GameUI.prefab";
        private const string MainMenuUIPrefabPath = "Assets/Prefabs/MainMenuUI.prefab";

        [MenuItem("Tools/Tower Defense/UI/Freeze UI To Prefabs (One Time)", priority = 30)]
        public static void FreezeUI()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string originalScenePath = SceneManager.GetActiveScene().path;

            bool ok = true;
            ok &= FreezeScene(MainScenePath, GameUIPrefabPath);
            ok &= FreezeScene(MainMenuScenePath, MainMenuUIPrefabPath);

            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                ok ? "UI Freeze" : "UI Freeze - Error",
                ok
                    ? "UI 已固化为 Prefab：\n\n" +
                      GameUIPrefabPath + "\n" +
                      MainMenuUIPrefabPath + "\n\n" +
                      "以后直接编辑这两个 Prefab，不需要再运行 UI 生成菜单。"
                    : "部分场景处理失败，请查看 Console。",
                "OK");
        }

        private static bool FreezeScene(string scenePath, string prefabPath)
        {
            if (!File.Exists(ToAbsolutePath(scenePath)))
            {
                Debug.LogWarning("[TD UI Freeze] 找不到场景: " + scenePath);
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject uiRoot = FindRoot(scene, "UI");
            if (uiRoot == null)
            {
                Debug.LogWarning("[TD UI Freeze] " + scenePath + " 里找不到 UI 根节点");
                return false;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(uiRoot))
            {
                Debug.Log("[TD UI Freeze] " + scenePath + " 的 UI 已经是 Prefab 实例，跳过");
                return true;
            }

            if (File.Exists(ToAbsolutePath(prefabPath)))
                AssetDatabase.DeleteAsset(prefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(uiRoot, prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[TD UI Freeze] 保存 Prefab 失败: " + prefabPath);
                return false;
            }

            Object.DestroyImmediate(uiRoot);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "UI";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TD UI Freeze] " + scenePath + " 的 UI 已保存为 " + prefabPath);
            return true;
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return root;
            }

            return null;
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }
    }
}
