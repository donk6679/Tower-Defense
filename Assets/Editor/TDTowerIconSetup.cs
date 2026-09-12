using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 炮塔图标导入设置工具：
    /// 把 Assets/Resources 下的炮塔图标与操作图标统一设为 Sprite、PPU 128、
    /// 透明背景且不生成 Mipmap，保证 UI 里清晰。
    /// 用法：Tools > Tower Defense > Tower Icons > Setup Icon Import Settings
    /// </summary>
    public static class TDTowerIconSetup
    {
        private static readonly string[] IconFolders =
        {
            "Assets/Resources/TowerIcons",
            "Assets/Resources/UiIcons",
            "Assets/Resources/HudIcons",
        };

        private static readonly string[] ExpectedFiles =
        {
            "TowerIcons/gun_lv1.png",
            "TowerIcons/gun_lv2.png",
            "TowerIcons/frost_lv1.png",
            "TowerIcons/frost_lv2.png",
            "TowerIcons/sniper_lv1.png",
            "TowerIcons/sniper_lv2.png",
            "UiIcons/upgrade.png",
            "UiIcons/demolish.png",
            "UiIcons/max_level.png",
            "HudIcons/gold.png",
            "HudIcons/life.png",
            "HudIcons/wave.png",
            "HudIcons/colon.png",
            "HudIcons/slash.png",
            "HudIcons/digit_0.png",
            "HudIcons/digit_1.png",
            "HudIcons/digit_2.png",
            "HudIcons/digit_3.png",
            "HudIcons/digit_4.png",
            "HudIcons/digit_5.png",
            "HudIcons/digit_6.png",
            "HudIcons/digit_7.png",
            "HudIcons/digit_8.png",
            "HudIcons/digit_9.png",
        };

        [MenuItem("Tools/Tower Defense/Tower Icons/Setup Icon Import Settings", priority = 25)]
        public static void SetupIconImportSettings()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            int configured = 0;
            foreach (string folder in IconFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png"))
                        continue;

                    ConfigureImporter(path);
                    configured++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var missing = new List<string>();
            foreach (string fileName in ExpectedFiles)
            {
                if (!File.Exists(ToAbsolutePath("Assets/Resources/" + fileName)))
                    missing.Add(fileName);
            }

            string summary = "已配置 " + configured + " 张图标。";
            if (missing.Count > 0)
                summary += "\n\n缺少：" + string.Join(", ", missing);
            else
                summary += "\n\n所有图标齐全。";

            Debug.Log("[TD Tower Icons] " + summary);
            EditorUtility.DisplayDialog("Tower Icons", summary, "OK");
        }

        private static void ConfigureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static void ShowError(string message)
        {
            Debug.LogError("[TD Tower Icons] " + message);
            EditorUtility.DisplayDialog("Tower Icons Error", message, "OK");
        }
    }
}
