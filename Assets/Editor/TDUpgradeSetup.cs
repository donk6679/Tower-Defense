using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 炮塔升级配置工具：按优化文档给现有 Gun/Frost/Sniper Prefab
    /// 写入 1~3 级数值（机炮升伤害、冰霜升减速、狙击升伤害+射程）。
    /// 用法：菜单 Tools > Tower Defense > Setup Tower Upgrades
    /// </summary>
    public static class TDUpgradeSetup
    {
        private const string GunTowerPath = "Assets/Prefabs/GunTower.prefab";
        private const string FrostTowerPath = "Assets/Prefabs/FrostTower.prefab";
        private const string SniperTowerPath = "Assets/Prefabs/SniperTower.prefab";

        [MenuItem("Tools/Tower Defense/Setup Tower Upgrades", priority = 8)]
        public static void SetupUpgrades()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool ok = true;
            ok &= ConfigureTower(GunTowerPath, "Gun Tower", BuildGunLevels());
            ok &= ConfigureTower(FrostTowerPath, "Frost Tower", BuildFrostLevels());
            ok &= ConfigureTower(SniperTowerPath, "Sniper Tower", BuildSniperLevels());

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                ok ? "TD Tower Upgrades" : "TD Tower Upgrades - Error",
                ok
                    ? "三座炮塔的 1~3 级数据已写入 Prefab。\n\n" +
                      "运行 Setup UI & 8 Manual Waves 刷新升级面板后，\n" +
                      "点击场上已有炮塔即可升级。"
                    : "部分 Prefab 未找到，请先运行 Setup 3 Towers & 3 Enemies。",
                "OK");
        }

        private static bool ConfigureTower(string prefabPath, string displayName, TowerLevelStats[] levels)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[TD Upgrade] 找不到 " + prefabPath);
                return false;
            }

            TowerBase tower = prefab.GetComponent<TowerBase>();
            if (tower == null)
            {
                Debug.LogError("[TD Upgrade] " + prefabPath + " 上没有 TowerBase");
                return false;
            }

            tower.SetUpgradeProfile(displayName, levels);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log("[TD Upgrade] " + displayName + " 已配置 " + levels.Length + " 级数据");
            return true;
        }

        private static TowerLevelStats[] BuildGunLevels()
        {
            return new[]
            {
                new TowerLevelStats(3f, 5, 2f, 1f, 0f, 0),
                new TowerLevelStats(3f, 8, 2f, 1f, 0f, 70),
                new TowerLevelStats(3f, 12, 2f, 1f, 0f, 80),
            };
        }

        private static TowerLevelStats[] BuildFrostLevels()
        {
            return new[]
            {
                // 减速倍率：0.6 = 减速 40%，0.45 = 减速 55%，0.3 = 减速 70%
                new TowerLevelStats(2.5f, 3, 1.5f, 0.6f, 2f, 0),
                new TowerLevelStats(2.5f, 3, 1.5f, 0.45f, 3f, 90),
                new TowerLevelStats(2.5f, 3, 1.5f, 0.3f, 4f, 105),
            };
        }

        private static TowerLevelStats[] BuildSniperLevels()
        {
            return new[]
            {
                new TowerLevelStats(5f, 20, 0.5f, 1f, 0f, 0),
                new TowerLevelStats(6.2f, 35, 0.5f, 1f, 0f, 130),
                new TowerLevelStats(7.5f, 55, 0.5f, 1f, 0f, 150),
            };
        }
    }
}
