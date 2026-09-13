using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 怪物外观工具：
    /// 把三种敌人的占位圆形替换为两张行走帧，
    /// 并配置朝向规则（狐狸怪素材默认朝左）。
    /// 用法：Tools > Tower Defense > Enemies > Setup In-Game Enemy Visuals
    /// </summary>
    public static class TDEnemyVisualSetup
    {
        private const string MonsterFolder = "Assets/Resources/Monsters";

        [MenuItem("Tools/Tower Defense/Enemies/Setup In-Game Enemy Visuals", priority = 27)]
        public static void SetupEnemyVisuals()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool ok = true;

            ok &= ConfigureEnemy(
                "Assets/Prefabs/Enemy.prefab",
                "basic", 0.22f, false, 1.5f);

            ok &= ConfigureEnemy(
                "Assets/Prefabs/FastEnemy.prefab",
                "fast", 0.15f, true, 2.4f);

            ok &= ConfigureEnemy(
                "Assets/Prefabs/TankEnemy.prefab",
                "tank", 0.30f, false, 1.575f);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                ok ? "TD Enemy Visuals" : "TD Enemy Visuals - Error",
                ok
                    ? "三种怪物的行走帧与朝向已写入 Prefab。\n\n" +
                      "运行时会交替播放两帧，水平移动时朝向移动方向。"
                    : "部分 Prefab 或素材缺失，请检查 Console。",
                "OK");
        }

        private static bool ConfigureEnemy(
            string prefabPath,
            string filePrefix,
            float frameInterval,
            bool facesLeftByDefault,
            float visualScale)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError("[TD Enemy Visuals] 找不到 Prefab: " + prefabPath);
                return false;
            }

            try
            {
                root.transform.localScale = Vector3.one * Mathf.Max(0.01f, visualScale);

                Sprite frame0 = LoadSprite(filePrefix + "_0");
                Sprite frame1 = LoadSprite(filePrefix + "_1");
                if (frame0 == null || frame1 == null)
                {
                    Debug.LogError("[TD Enemy Visuals] 缺少素材: " + filePrefix);
                    return false;
                }

                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = root.AddComponent<SpriteRenderer>();

                renderer.sprite = frame0;
                renderer.color = Color.white;

                Enemy enemy = root.GetComponent<Enemy>();
                if (enemy == null)
                {
                    Debug.LogError("[TD Enemy Visuals] " + prefabPath + " 上没有 Enemy 组件");
                    return false;
                }

                enemy.SetWalkFrames(
                    new[] { frame0, frame1 },
                    frameInterval,
                    facesLeftByDefault);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[TD Enemy Visuals] " + prefabPath + " 外观已更新");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            string path = MonsterFolder + "/" + fileName + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                return null;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                return sprite;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            float targetPixelsPerUnit = Mathf.Max(texture.width, texture.height);

            if (importer != null &&
                (!Mathf.Approximately(importer.spritePixelsPerUnit, targetPixelsPerUnit) ||
                 importer.textureType != TextureImporterType.Sprite))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = targetPixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return sprite;
        }
    }
}
