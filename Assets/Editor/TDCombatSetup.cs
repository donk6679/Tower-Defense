using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 第三天战斗搭建工具：
    /// 生成机炮塔/子弹占位 Prefab，并在场景中配置 BuildManager。
    /// 用法：菜单 Tools > Tower Defense > Setup Build & Gun Tower Test
    /// </summary>
    public static class TDCombatSetup
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string GunTowerPrefabPath = "Assets/Prefabs/GunTower.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectile.prefab";
        private const string DiamondSpritePath = "Assets/Sprites/Map/spawn_marker.asset";
        private const string CircleSpritePath = "Assets/Sprites/Map/core_marker.asset";

        [MenuItem("Tools/Tower Defense/Setup Build & Gun Tower Test", priority = 4)]
        public static void SetupCombat()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GameManager gameManager = Object.FindObjectOfType<GameManager>();
            WaveManager waveManager = Object.FindObjectOfType<WaveManager>();
            if (gameManager == null || waveManager == null)
            {
                ShowError(
                    "缺少第二天配置",
                    "请先运行 Tools > Tower Defense > Setup Wave & Life Test，再执行本菜单。");
                return;
            }

            GameObject enemyPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Enemy enemyPrefab = enemyPrefabObject != null
                ? enemyPrefabObject.GetComponent<Enemy>()
                : null;
            if (enemyPrefab == null)
            {
                ShowError("找不到敌人 Prefab", "请先运行 Tools > Tower Defense > Add Enemy Movement Test。");
                return;
            }

            // 旧 Prefab 可能缺少“金币奖励”字段，补上默认值并重新保存
            enemyPrefab.SetGoldReward(5);
            PrefabUtility.SavePrefabAsset(enemyPrefabObject);

            Sprite diamondSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondSpritePath);
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            if (diamondSprite == null || circleSprite == null)
            {
                ShowError("缺少占位贴图", "请先运行地图生成菜单。");
                return;
            }

            EnsurePrefabFolderExists();

            Projectile projectilePrefab = CreateProjectilePrefab(circleSprite);
            TowerBase gunTowerPrefab = CreateGunTowerPrefab(diamondSprite, circleSprite, projectilePrefab);

            BuildManager buildManager = GetOrCreateBuildManager();
            buildManager.SetAvailableTowers(new[]
            {
                new BuildManager.TowerTypeConfig("Gun Tower", gunTowerPrefab, 60),
            });

            gameManager.SetStartingLives(20);
            gameManager.SetStartingGold(200);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "TD Build & Gun Tower",
                "第三天玩法已配置完成。\n\n" +
                "按 Play 后：\n" +
                "1. 按 1 选择 Gun Tower\n" +
                "2. 左键点击浅绿色地块建造（花费 60 金币）\n" +
                "3. 机炮塔会自动射击射程内的敌人\n" +
                "4. 敌人被击杀后 +5 金币\n\n" +
                "左上角显示 Gold / Lives。",
                "OK");
        }

        private static Projectile CreateProjectilePrefab(Sprite circleSprite)
        {
            if (File.Exists(ToAbsolutePath(ProjectilePrefabPath)))
                AssetDatabase.DeleteAsset(ProjectilePrefabPath);

            GameObject temp = new GameObject("Projectile");
            temp.transform.localScale = Vector3.one * 0.36f;

            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = new Color(1f, 0.85f, 0.25f, 1f);
            renderer.sortingOrder = 11;

            temp.AddComponent<Projectile>();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(temp, ProjectilePrefabPath);
            Object.DestroyImmediate(temp);

            return savedPrefab != null ? savedPrefab.GetComponent<Projectile>() : null;
        }

        private static TowerBase CreateGunTowerPrefab(
            Sprite diamondSprite,
            Sprite circleSprite,
            Projectile projectilePrefab)
        {
            if (File.Exists(ToAbsolutePath(GunTowerPrefabPath)))
                AssetDatabase.DeleteAsset(GunTowerPrefabPath);

            GameObject temp = new GameObject("GunTower");
            temp.transform.localScale = Vector3.one * 0.72f;

            // 底座：菱形
            SpriteRenderer baseRenderer = temp.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = diamondSprite;
            baseRenderer.color = new Color(0.35f, 0.72f, 1f, 1f);
            baseRenderer.sortingOrder = 1;

            // 炮口装饰：小圆点（只是占位造型，之后换成美术）
            GameObject barrel = new GameObject("Barrel");
            barrel.transform.SetParent(temp.transform, false);
            barrel.transform.localPosition = new Vector3(0.1f, 0.18f, 0f);
            barrel.transform.localScale = Vector3.one * 0.5f;

            SpriteRenderer barrelRenderer = barrel.AddComponent<SpriteRenderer>();
            barrelRenderer.sprite = circleSprite;
            barrelRenderer.color = new Color(0.9f, 0.96f, 1f, 1f);
            barrelRenderer.sortingOrder = 2;

            GunTower tower = temp.AddComponent<GunTower>();
            tower.Configure(3.2f, 5, 2f, projectilePrefab);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(temp, GunTowerPrefabPath);
            Object.DestroyImmediate(temp);

            return savedPrefab != null ? savedPrefab.GetComponent<TowerBase>() : null;
        }

        private static BuildManager GetOrCreateBuildManager()
        {
            BuildManager buildManager = Object.FindObjectOfType<BuildManager>();
            if (buildManager != null)
                return buildManager;

            GameObject map = GameObject.Find("Map");
            GameObject managerObject = new GameObject("BuildManager");
            if (map != null)
                managerObject.transform.SetParent(map.transform, false);

            return managerObject.AddComponent<BuildManager>();
        }

        private static void EnsurePrefabFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD Combat] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
