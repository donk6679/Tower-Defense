using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 场内炮塔外观工具：
    /// - 把三座炮塔 Prefab 的占位造型换成炮塔图标（1 级 / 2 级）；
    /// - 移除旧的 Barrel 子物体（图标的炮管已经包含在贴图里）；
    /// - 设置炮口朝向补偿与转向速度，让炮口对准目标；
    /// - 同时写入最高 2 级的升级数据。
    ///
    /// 用法：Tools > Tower Defense > Towers > Setup In-Game Tower Visuals
    /// </summary>
    public static class TDTowerVisualSetup
    {
        private const string GunTowerPath = "Assets/Prefabs/GunTower.prefab";
        private const string FrostTowerPath = "Assets/Prefabs/FrostTower.prefab";
        private const string SniperTowerPath = "Assets/Prefabs/SniperTower.prefab";
        private const string IconFolder = "Assets/Resources/TowerIcons";
        private const float TurnSpeed = 720f;

        [MenuItem("Tools/Tower Defense/Towers/Setup In-Game Tower Visuals", priority = 26)]
        public static void SetupTowerVisuals()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool ok = true;

            // Gun/Frost 的图标是朝上的（+Y），所以旋转补偿 -90°；
            // Sniper 的图标是朝右的（+X），补偿 0°。
            ok &= ConfigureTower(
                GunTowerPath, "Gun Tower", -90f, 1.44f,
                "gun_lv1", "gun_lv2",
                TDUpgradeSetup.BuildGunLevels());

            ok &= ConfigureTower(
                FrostTowerPath, "Frost Tower", -90f, 1.44f,
                "frost_lv1", "frost_lv2",
                TDUpgradeSetup.BuildFrostLevels());

            ok &= ConfigureTower(
                SniperTowerPath, "Sniper Tower", 0f, 2.16f,
                "sniper_lv1", "sniper_lv2",
                TDUpgradeSetup.BuildSniperLevels());

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                ok ? "TD Tower Visuals" : "TD Tower Visuals - Error",
                ok
                    ? "三座炮塔的场内图标与 1~2 级数据已写入 Prefab。\n\n" +
                      "运行时炮塔会持续朝向当前锁定的敌人。"
                    : "部分 Prefab 或图标缺失，请检查 Console。",
                "OK");
        }

        private static bool ConfigureTower(
            string prefabPath,
            string displayName,
            float aimAngleOffset,
            float visualScale,
            string level1IconName,
            string level2IconName,
            TowerLevelStats[] levels)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError("[TD Tower Visuals] 找不到 Prefab: " + prefabPath);
                return false;
            }

            try
            {
                root.transform.localScale = Vector3.one * Mathf.Max(0.01f, visualScale);

                Sprite level1 = LoadIcon(level1IconName);
                Sprite level2 = LoadIcon(level2IconName);
                if (level1 == null)
                {
                    Debug.LogError("[TD Tower Visuals] 缺少图标 " + level1IconName +
                                   ".png（" + prefabPath + "）");
                    return false;
                }

                // 图标本身已经包含炮管，移除旧的占位子物体
                Transform oldBarrel = root.transform.Find("Barrel");
                if (oldBarrel != null)
                    Object.DestroyImmediate(oldBarrel.gameObject);

                SpriteRenderer body = root.GetComponent<SpriteRenderer>();
                if (body == null)
                    body = root.AddComponent<SpriteRenderer>();
                body.sprite = level1;
                body.color = Color.white;

                TowerBase tower = root.GetComponent<TowerBase>();
                if (tower == null)
                {
                    Debug.LogError("[TD Tower Visuals] " + prefabPath + " 上没有 TowerBase");
                    return false;
                }

                tower.SetUpgradeProfile(displayName, levels);
                tower.SetAimSettings(aimAngleOffset, TurnSpeed);
                tower.SetLevelSprites(new[] { level1, level2 });

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[TD Tower Visuals] " + displayName + " 外观与瞄准参数已配置");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite LoadIcon(string iconName)
        {
            string path = IconFolder + "/" + iconName + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                return null;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                return sprite;

            float targetPixelsPerUnit = Mathf.Max(texture.width, texture.height);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null &&
                !Mathf.Approximately(importer.spritePixelsPerUnit, targetPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = targetPixelsPerUnit;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return sprite;
        }
    }
}
