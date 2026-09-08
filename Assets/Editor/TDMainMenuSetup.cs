using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 主菜单搭建工具：
    /// 生成 MainMenu 场景（开始游戏/退出），并把 MainMenu、Main
    /// 加入 Build Settings（MainMenu 在前，作为打包后的首个界面）。
    /// 用法：菜单 Tools > Tower Defense > Setup Main Menu & Build Settings
    /// </summary>
    public static class TDMainMenuSetup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string WhiteSpritePath = "Assets/Sprites/UI/white.png";

        [MenuItem("Tools/Tower Defense/Setup Main Menu & Build Settings", priority = 7)]
        public static void SetupMainMenu()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!File.Exists(ToAbsolutePath(MainScenePath)))
            {
                ShowError("找不到游戏场景", "请先运行地图生成菜单创建 Assets/Scenes/Main.unity。");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite whiteSprite = EnsureWhiteSprite();
            CreateMenuUI(whiteSprite);
            CreateMainCamera();
            EnsureEventSystemExists();

            if (File.Exists(ToAbsolutePath(MainMenuScenePath)))
                AssetDatabase.DeleteAsset(MainMenuScenePath);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainMenuScenePath);
            AssetDatabase.SaveAssets();

            UpdateBuildSettings();

            EditorUtility.DisplayDialog(
                "TD Main Menu",
                "主菜单已创建并保存到 Assets/Scenes/MainMenu.unity。\n\n" +
                "Build Settings 已把 MainMenu 设为第 1 个场景，Main 为第 2 个。\n\n" +
                "在编辑器里打开 MainMenu 场景并按 Play 即可测试主菜单。",
                "OK");
        }

        private static void CreateMenuUI(Sprite whiteSprite)
        {
            GameObject uiRoot = new GameObject("UI", typeof(RectTransform));
            RectTransform canvasRect = (RectTransform)uiRoot.transform;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            Canvas canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            uiRoot.AddComponent<GraphicRaycaster>();

            // 深色背景
            RectTransform background = CreateRect(uiRoot.transform, "Background");
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = whiteSprite;
            backgroundImage.color = new Color(0.04f, 0.08f, 0.13f, 1f);

            // 标题与副标题
            CreateScreenText(
                uiRoot.transform, "Title", "TOWER DEFENSE",
                104, new Color(1f, 0.85f, 0.35f),
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1400f, 160f), TextAnchor.MiddleCenter);

            CreateScreenText(
                uiRoot.transform, "Subtitle", "Protect the core. Survive all 8 waves.",
                38, new Color(0.75f, 0.85f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1200f, 60f), TextAnchor.MiddleCenter);

            // 按钮
            uiRoot.AddComponent<MainMenuUI>();

            Button startButton = CreateButton(
                uiRoot.transform, "StartButton", "START GAME",
                whiteSprite, 46, new Color(0.12f, 0.55f, 0.3f, 1f),
                new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 110f));

            Button exitButton = CreateButton(
                uiRoot.transform, "ExitButton", "EXIT",
                whiteSprite, 40, new Color(0.45f, 0.2f, 0.2f, 1f),
                new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(320f, 90f));

        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.08f, 0.13f, 1f);

            cameraObject.AddComponent<AudioListener>();
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Sprite sprite,
            int fontSize,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform textRect = CreateRect(rect, "Text");
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return button;
        }

        private static Text CreateScreenText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.text = content;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void EnsureEventSystemExists()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void UpdateBuildSettings()
        {
            var sceneList = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true),
            };

            EditorBuildSettings.scenes = sceneList.ToArray();
            Debug.Log("[TD Main Menu] Build Settings 已更新：MainMenu 在前，Main 在后");
        }

        private static Sprite EnsureWhiteSprite()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites/UI"))
                AssetDatabase.CreateFolder("Assets/Sprites", "UI");

            if (!File.Exists(ToAbsolutePath(WhiteSpritePath)))
            {
                const int size = 4;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.white;

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(ToAbsolutePath(WhiteSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(WhiteSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(WhiteSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 4;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD Main Menu] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
