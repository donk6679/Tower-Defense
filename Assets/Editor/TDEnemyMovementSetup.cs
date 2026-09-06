using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 敌人移动测试搭建工具：
    /// 在 Main 场景中创建一个红色圆形敌人 Prefab 和 EnemySpawner，
    /// 运行游戏后即可看到敌人沿路径移动。
    /// 用法：菜单 Tools > Tower Defense > Add Enemy Movement Test
    /// </summary>
    public static class TDEnemyMovementSetup
    {
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string PlaceholderSpritePath = "Assets/Sprites/Map/core_marker.asset";

        [MenuItem("Tools/Tower Defense/Add Enemy Movement Test", priority = 2)]
        public static void SetupEnemyMovementTest()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PathManager pathManager = Object.FindObjectOfType<PathManager>();
            if (pathManager == null)
            {
                ShowError("找不到 PathManager", "请先运行 Tools > Tower Defense > Generate Map & Main Scene。");
                return;
            }

            Sprite placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (placeholderSprite == null)
            {
                ShowError("缺少占位贴图", "没有找到 core_marker 精灵，请先运行地图生成菜单。");
                return;
            }

            EnsureFolderExists("Assets/Prefabs");
            CreateEnemyPrefab(placeholderSprite);

            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Enemy enemyPrefab = prefabObject != null ? prefabObject.GetComponent<Enemy>() : null;
            if (enemyPrefab == null)
            {
                ShowError("Prefab 生成失败", "Enemy.prefab 没有创建成功，请查看 Console 报错。");
                return;
            }

            EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
            if (spawner == null)
            {
                GameObject spawnerObject = new GameObject("EnemySpawner");
                GameObject map = GameObject.Find("Map");
                if (map != null)
                    spawnerObject.transform.SetParent(map.transform, false);
                spawner = spawnerObject.AddComponent<EnemySpawner>();
            }

            spawner.Setup(pathManager, enemyPrefab);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "TD Enemy Movement",
                "测试敌人已配置完成。\n\n" +
                "按 Play 后，每 1.5 秒会从入口生成一个红色圆形敌人，\n" +
                "沿黄色路径点路线走向核心，到达后自动销毁。\n\n" +
                "生成数量与间隔可在 EnemySpawner 上调整。",
                "OK");
        }

        private static void CreateEnemyPrefab(Sprite placeholderSprite)
        {
            if (File.Exists(ToAbsolutePath(EnemyPrefabPath)))
                AssetDatabase.DeleteAsset(EnemyPrefabPath);

            GameObject temp = new GameObject("Enemy");
            temp.transform.localScale = Vector3.one * 0.75f;

            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = new Color(0.92f, 0.25f, 0.25f, 1f);
            renderer.sortingOrder = 10;

            temp.AddComponent<Enemy>();

            PrefabUtility.SaveAsPrefabAsset(temp, EnemyPrefabPath);
            Object.DestroyImmediate(temp);
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD Enemy Movement] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
