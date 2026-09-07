using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 第四天配置工具：
    /// 生成 3 种敌人（Basic/Fast/Tank）、3 种炮塔（Gun/Frost/Sniper）与对应子弹，
    /// 并写入 BuildManager 建造列表和 WaveManager 波次配置。
    /// 用法：菜单 Tools > Tower Defense > Setup 3 Towers & 3 Enemies
    /// </summary>
    public static class TDAdvancedSetup
    {
        private const string PrefabsFolder = "Assets/Prefabs";

        // 敌人
        private const string BasicEnemyPath = "Assets/Prefabs/Enemy.prefab";
        private const string FastEnemyPath = "Assets/Prefabs/FastEnemy.prefab";
        private const string TankEnemyPath = "Assets/Prefabs/TankEnemy.prefab";

        // 子弹
        private const string GunProjectilePath = "Assets/Prefabs/Projectile.prefab";
        private const string FrostProjectilePath = "Assets/Prefabs/FrostProjectile.prefab";
        private const string SniperProjectilePath = "Assets/Prefabs/SniperProjectile.prefab";

        // 炮塔
        private const string GunTowerPath = "Assets/Prefabs/GunTower.prefab";
        private const string FrostTowerPath = "Assets/Prefabs/FrostTower.prefab";
        private const string SniperTowerPath = "Assets/Prefabs/SniperTower.prefab";

        private const string DiamondSpritePath = "Assets/Sprites/Map/spawn_marker.asset";
        private const string CircleSpritePath = "Assets/Sprites/Map/core_marker.asset";

        [MenuItem("Tools/Tower Defense/Setup 3 Towers & 3 Enemies", priority = 5)]
        public static void SetupAdvanced()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GameManager gameManager = Object.FindObjectOfType<GameManager>();
            WaveManager waveManager = Object.FindObjectOfType<WaveManager>();
            PathManager pathManager = Object.FindObjectOfType<PathManager>();

            if (gameManager == null || waveManager == null || pathManager == null)
            {
                ShowError(
                    "缺少基础配置",
                    "请先依次运行 Setup Wave & Life Test 和 Setup Build & Gun Tower Test。");
                return;
            }

            Sprite diamondSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondSpritePath);
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            if (diamondSprite == null || circleSprite == null)
            {
                ShowError("缺少占位贴图", "请先运行地图生成菜单。");
                return;
            }

            EnsurePrefabFolderExists();

            // ---------- 敌人 ----------
            Enemy basicEnemy = CreateEnemyPrefab(
                BasicEnemyPath, "Enemy", circleSprite,
                new Color(0.95f, 0.25f, 0.25f, 1f),
                0.75f, 2f, 10, 5);

            Enemy fastEnemy = CreateEnemyPrefab(
                FastEnemyPath, "FastEnemy", circleSprite,
                new Color(1f, 0.65f, 0.15f, 1f),
                0.6f, 3.5f, 4, 8);

            Enemy tankEnemy = CreateEnemyPrefab(
                TankEnemyPath, "TankEnemy", circleSprite,
                new Color(0.25f, 0.5f, 1f, 1f),
                1.05f, 1.2f, 45, 15);

            // ---------- 子弹 ----------
            Projectile gunProjectile = CreateProjectilePrefab(
                GunProjectilePath, "Projectile", circleSprite,
                new Color(1f, 0.85f, 0.2f, 1f),
                0.18f, 9f, typeof(Projectile));

            Projectile frostProjectile = CreateProjectilePrefab(
                FrostProjectilePath, "FrostProjectile", circleSprite,
                new Color(0.4f, 0.85f, 1f, 1f),
                0.2f, 10f, typeof(FrostProjectile));

            Projectile sniperProjectile = CreateProjectilePrefab(
                SniperProjectilePath, "SniperProjectile", circleSprite,
                new Color(1f, 0.35f, 0.1f, 1f),
                0.24f, 14f, typeof(Projectile));

            // ---------- 炮塔 ----------
            TowerBase gunTower = CreateTowerPrefab(
                GunTowerPath, "GunTower",
                new Color(0.35f, 0.72f, 1f, 1f),
                new Color(0.9f, 0.96f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                3.2f, 5, 2f, gunProjectile,
                typeof(GunTower));

            TowerBase frostTower = CreateTowerPrefab(
                FrostTowerPath, "FrostTower",
                new Color(0.2f, 0.85f, 0.95f, 1f),
                new Color(0.75f, 1f, 1f, 1f),
                new Vector2(0.45f, 0.45f),
                2.6f, 2, 1.5f, frostProjectile,
                typeof(FrostTower));

            TowerBase sniperTower = CreateTowerPrefab(
                SniperTowerPath, "SniperTower",
                new Color(0.75f, 0.25f, 0.85f, 1f),
                new Color(1f, 0.9f, 1f, 1f),
                new Vector2(0.9f, 0.18f),
                5f, 25, 0.5f, sniperProjectile,
                typeof(SniperTower));

            // ---------- 管理器配置 ----------
            BuildManager buildManager = GetOrCreateBuildManager();
            buildManager.SetAvailableTowers(new[]
            {
                new BuildManager.TowerTypeConfig("Gun Tower", gunTower, 60),
                new BuildManager.TowerTypeConfig("Frost Tower", frostTower, 75),
                new BuildManager.TowerTypeConfig("Sniper Tower", sniperTower, 110),
            });

            waveManager.Setup(pathManager, new[]
            {
                new WaveSettings(
                    new[]
                    {
                        new EnemyGroup(basicEnemy, 4, 1.4f),
                        new EnemyGroup(fastEnemy, 2, 1.1f),
                    },
                    1f, 2.5f),
                new WaveSettings(
                    new[]
                    {
                        new EnemyGroup(basicEnemy, 5, 1.3f),
                        new EnemyGroup(fastEnemy, 3, 1f),
                        new EnemyGroup(tankEnemy, 1, 2.5f),
                    },
                    2f, 3f),
                new WaveSettings(
                    new[]
                    {
                        new EnemyGroup(basicEnemy, 4, 1.2f),
                        new EnemyGroup(fastEnemy, 4, 0.9f),
                        new EnemyGroup(tankEnemy, 2, 2.2f),
                    },
                    2f, 3f),
            });

            gameManager.SetStartingLives(20);
            gameManager.SetStartingGold(200);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "TD 3 Towers & 3 Enemies",
                "第四天配置完成。\n\n" +
                "敌人：红色 Basic / 橙色 Fast / 蓝色 Tank\n" +
                "炮塔：1 Gun / 2 Frost / 3 Sniper\n\n" +
                "Frost 命中会把敌人染成蓝色并减速；\n" +
                "Sniper 射程最远、单发伤害最高；\n" +
                "击杀不同敌人会获得不同金币。",
                "OK");
        }

        private static Enemy CreateEnemyPrefab(
            string prefabPath,
            string objectName,
            Sprite sprite,
            Color color,
            float scale,
            float moveSpeed,
            int health,
            int goldReward)
        {
            DeleteAssetIfExists(prefabPath);

            GameObject temp = new GameObject(objectName);
            temp.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 10;

            Enemy enemy = temp.AddComponent<Enemy>();
            enemy.SetMoveSpeed(moveSpeed);
            enemy.SetMaxHealth(health);
            enemy.SetGoldReward(goldReward);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return saved != null ? saved.GetComponent<Enemy>() : null;
        }

        private static Projectile CreateProjectilePrefab(
            string prefabPath,
            string objectName,
            Sprite sprite,
            Color color,
            float scale,
            float speed,
            System.Type projectileType)
        {
            DeleteAssetIfExists(prefabPath);

            GameObject temp = new GameObject(objectName);
            temp.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = temp.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 11;

            Projectile projectile = temp.AddComponent(projectileType) as Projectile;
            if (projectile == null)
            {
                Object.DestroyImmediate(temp);
                return null;
            }

            projectile.SetFlightSpeed(speed);

            if (projectile is FrostProjectile frost)
                frost.SetSlowEffect(0.6f, 2f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return saved != null ? saved.GetComponent<Projectile>() : null;
        }

        private static TowerBase CreateTowerPrefab(
            string prefabPath,
            string objectName,
            Color baseColor,
            Color barrelColor,
            Vector2 barrelScale,
            float range,
            int damage,
            float fireRate,
            Projectile projectile,
            System.Type towerType)
        {
            DeleteAssetIfExists(prefabPath);

            GameObject temp = new GameObject(objectName);
            temp.transform.localScale = Vector3.one * 0.72f;

            SpriteRenderer baseRenderer = temp.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondSpritePath);
            baseRenderer.color = baseColor;
            baseRenderer.sortingOrder = 1;

            GameObject barrel = new GameObject("Barrel");
            barrel.transform.SetParent(temp.transform, false);
            barrel.transform.localPosition = new Vector3(0.14f, 0.1f, 0f);
            barrel.transform.localScale = new Vector3(barrelScale.x, barrelScale.y, 1f);

            SpriteRenderer barrelRenderer = barrel.AddComponent<SpriteRenderer>();
            barrelRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            barrelRenderer.color = barrelColor;
            barrelRenderer.sortingOrder = 2;

            TowerBase tower = temp.AddComponent(towerType) as TowerBase;
            if (tower == null)
            {
                Object.DestroyImmediate(temp);
                return null;
            }

            tower.Configure(range, damage, fireRate, projectile);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return saved != null ? saved.GetComponent<TowerBase>() : null;
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

        private static void DeleteAssetIfExists(string assetPath)
        {
            string absolute = Path.GetFullPath(Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                assetPath));

            if (File.Exists(absolute))
                AssetDatabase.DeleteAsset(assetPath);
        }

        private static void EnsurePrefabFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD Advanced] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
