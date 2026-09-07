using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 第二天玩法搭建工具：
    /// 配置 GameManager、WaveManager 与调试击杀组件，
    /// 覆盖旧的测试用 EnemySpawner。
    /// 用法：菜单 Tools > Tower Defense > Setup Wave & Life Test
    /// </summary>
    public static class TDGameplaySetup
    {
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";

        [MenuItem("Tools/Tower Defense/Setup Wave & Life Test", priority = 3)]
        public static void SetupDay2Gameplay()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PathManager pathManager = Object.FindObjectOfType<PathManager>();
            if (pathManager == null)
            {
                ShowError("找不到 PathManager", "请先运行地图生成菜单。");
                return;
            }

            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Enemy enemyPrefab = prefabObject != null ? prefabObject.GetComponent<Enemy>() : null;
            if (enemyPrefab == null)
            {
                ShowError("找不到敌人 Prefab", "请先运行 Tools > Tower Defense > Add Enemy Movement Test。");
                return;
            }

            // 旧的单机测试生成器由 WaveManager 取代，直接移除，避免重复刷怪
            EnemySpawner oldSpawner = Object.FindObjectOfType<EnemySpawner>();
            if (oldSpawner != null)
                Object.DestroyImmediate(oldSpawner.gameObject);

            GameManager gameManager = GetOrCreateGameManager();
            WaveManager waveManager = GetOrCreateWaveManager();

            waveManager.Setup(
                pathManager,
                new[]
                {
                    new WaveSettings(
                        new[] { new EnemyGroup(enemyPrefab, 5, 1.4f) },
                        1.0f, 2.5f),
                    new WaveSettings(
                        new[] { new EnemyGroup(enemyPrefab, 8, 1.2f) },
                        2.0f, 2.5f),
                    new WaveSettings(
                        new[] { new EnemyGroup(enemyPrefab, 10, 1.0f) },
                        2.0f, 3.0f),
                });

            if (gameManager.GetComponent<DebugEnemyKiller>() == null)
                gameManager.gameObject.AddComponent<DebugEnemyKiller>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "TD Wave & Life",
                "第二天玩法已配置完成。\n\n" +
                "按 Play 后会自动进行 3 波敌人（5/8/10 只）。\n" +
                "敌人到达核心会扣生命（左上角 Lives）。\n\n" +
                "按 K 可消灭场上任意敌人，观察死亡碎片反馈。",
                "OK");
        }

        private static GameManager GetOrCreateGameManager()
        {
            GameObject managerObject = GameObject.Find("GameManager");
            GameManager gameManager = managerObject != null
                ? managerObject.GetComponent<GameManager>()
                : null;

            if (gameManager != null)
                return gameManager;

            if (managerObject == null)
                managerObject = new GameObject("GameManager");

            gameManager = managerObject.GetComponent<GameManager>();
            if (gameManager == null)
                gameManager = managerObject.AddComponent<GameManager>();

            gameManager.SetStartingLives(20);
            return gameManager;
        }

        private static WaveManager GetOrCreateWaveManager()
        {
            WaveManager waveManager = Object.FindObjectOfType<WaveManager>();
            if (waveManager != null)
                return waveManager;

            GameObject map = GameObject.Find("Map");
            GameObject managerObject = map != null
                ? new GameObject("WaveManager")
                : new GameObject("WaveManager");

            if (map != null)
                managerObject.transform.SetParent(map.transform, false);

            return managerObject.AddComponent<WaveManager>();
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD Gameplay] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
