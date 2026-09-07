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
    /// 第五天 UI 搭建工具：
    /// 创建正式 Canvas UI、8 波手动波次配置，
    /// 并绑定胜利/失败/重新开始流程。
    /// 用法：菜单 Tools > Tower Defense > Setup UI & 8 Manual Waves
    /// </summary>
    public static class TDSetupUI
    {
        private const string WhiteSpritePath = "Assets/Sprites/UI/white.png";
        private const string BasicEnemyPath = "Assets/Prefabs/Enemy.prefab";
        private const string FastEnemyPath = "Assets/Prefabs/FastEnemy.prefab";
        private const string TankEnemyPath = "Assets/Prefabs/TankEnemy.prefab";

        private static readonly Color ButtonNormal = new Color(0.15f, 0.25f, 0.4f, 1f);
        private static readonly Color PanelDim = new Color(0f, 0f, 0f, 0.72f);

        [MenuItem("Tools/Tower Defense/Setup UI & 8 Manual Waves", priority = 6)]
        public static void SetupFullUI()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GameManager gameManager = Object.FindObjectOfType<GameManager>();
            WaveManager waveManager = Object.FindObjectOfType<WaveManager>();
            BuildManager buildManager = Object.FindObjectOfType<BuildManager>();
            PathManager pathManager = Object.FindObjectOfType<PathManager>();

            if (gameManager == null || waveManager == null || buildManager == null || pathManager == null)
            {
                ShowError("缺少基础配置", "请先运行 Setup Wave & Life Test 与 Setup 3 Towers & 3 Enemies。");
                return;
            }

            if (buildManager.AvailableTowers == null || buildManager.AvailableTowers.Length < 3)
            {
                ShowError("炮塔列表不完整", "请先运行 Setup 3 Towers & 3 Enemies。");
                return;
            }

            Enemy basicEnemy = LoadPrefabComponent<Enemy>(BasicEnemyPath);
            Enemy fastEnemy = LoadPrefabComponent<Enemy>(FastEnemyPath);
            Enemy tankEnemy = LoadPrefabComponent<Enemy>(TankEnemyPath);
            if (basicEnemy == null || fastEnemy == null || tankEnemy == null)
            {
                ShowError("缺少敌人 Prefab", "请先运行 Setup 3 Towers & 3 Enemies。");
                return;
            }

            gameManager.SetStartingLives(20);
            gameManager.SetStartingGold(200);

            // 8 波手动波次：玩家准备好后点击 Start Wave
            waveManager.Setup(pathManager, new[]
            {
                Wave(basicEnemy, 4, 1.4f, 1.2f, 2.2f),
                Wave(basicEnemy, 5, 1.4f, fastEnemy, 2, 1.0f, 1.2f, 2.2f),
                Wave(basicEnemy, 5, 1.3f, fastEnemy, 3, 0.9f, tankEnemy, 1, 2.5f, 1.2f, 2.4f),
                Wave(basicEnemy, 6, 1.3f, fastEnemy, 3, 0.8f, tankEnemy, 1, 2.5f, 1.2f, 2.4f),
                Wave(basicEnemy, 6, 1.2f, fastEnemy, 4, 0.8f, tankEnemy, 2, 2.4f, 1.1f, 2.3f),
                Wave(basicEnemy, 7, 1.1f, fastEnemy, 4, 0.7f, tankEnemy, 2, 2.3f, 1.1f, 2.2f),
                Wave(basicEnemy, 7, 1.0f, fastEnemy, 5, 0.7f, tankEnemy, 3, 2.2f, 1.0f, 2.1f),
                Wave(basicEnemy, 8, 0.9f, fastEnemy, 6, 0.6f, tankEnemy, 4, 2.0f, 1.0f, 2.0f),
            });

            CreateOrReplaceUI(buildManager);
            EnsureEventSystemExists();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "TD Full UI",
                "UI 与 8 波手动波次已配置完成。\n\n" +
                "按 Play 后：\n" +
                "1. 点击底部炮塔按钮选择类型\n" +
                "2. 点击浅绿色地块建造\n" +
                "3. 点击 Start Wave 开始波次\n" +
                "4. 守住全部 8 波 → VICTORY\n" +
                "生命归零 → GAME OVER\n" +
                "结算界面可点击 Restart 重开。",
                "OK");
        }

        // ---------- 波次辅助 ----------

        private static WaveSettings Wave(Enemy a, int aCount, float aInterval, float before, float after)
        {
            return new WaveSettings(new[] { new EnemyGroup(a, aCount, aInterval) }, before, after);
        }

        private static WaveSettings Wave(
            Enemy a, int aCount, float aInterval,
            Enemy b, int bCount, float bInterval,
            float before, float after)
        {
            return new WaveSettings(
                new[]
                {
                    new EnemyGroup(a, aCount, aInterval),
                    new EnemyGroup(b, bCount, bInterval),
                },
                before, after);
        }

        private static WaveSettings Wave(
            Enemy a, int aCount, float aInterval,
            Enemy b, int bCount, float bInterval,
            Enemy c, int cCount, float cInterval,
            float before, float after)
        {
            return new WaveSettings(
                new[]
                {
                    new EnemyGroup(a, aCount, aInterval),
                    new EnemyGroup(b, bCount, bInterval),
                    new EnemyGroup(c, cCount, cInterval),
                },
                before, after);
        }

        // ---------- UI 创建 ----------

        private static void CreateOrReplaceUI(BuildManager buildManager)
        {
            GameObject oldUi = GameObject.Find("UI");
            if (oldUi != null)
                Object.DestroyImmediate(oldUi);

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

            UIManager ui = uiRoot.AddComponent<UIManager>();
            Sprite whiteSprite = EnsureWhiteSprite();

            BuildHUD(uiRoot.transform, ui);
            BuildTowerButtons(uiRoot.transform, ui, buildManager, whiteSprite);
            BuildStartButton(uiRoot.transform, ui, whiteSprite);
            BuildResultPanels(uiRoot.transform, ui, whiteSprite);
        }

        private static void BuildHUD(Transform canvasRoot, UIManager ui)
        {
            ui.goldText = CreateScreenText(
                canvasRoot, "GoldText", "Gold: 200",
                36, new Color(1f, 0.9f, 0.35f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(400f, 46f));

            ui.livesText = CreateScreenText(
                canvasRoot, "LivesText", "Lives: 20",
                36, new Color(1f, 0.55f, 0.45f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -86f), new Vector2(400f, 46f));

            ui.waveText = CreateScreenText(
                canvasRoot, "WaveText", "Wave: 1 / 8",
                36, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -142f), new Vector2(400f, 46f));

            ui.hintText = CreateScreenText(
                canvasRoot, "HintText", "Select a tower button, then click a green tile",
                24, new Color(0.85f, 0.9f, 1f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -200f), new Vector2(720f, 40f));
        }

        private static void BuildTowerButtons(
            Transform canvasRoot,
            UIManager ui,
            BuildManager buildManager,
            Sprite whiteSprite)
        {
            BuildManager.TowerTypeConfig[] types = buildManager.AvailableTowers;
            int count = Mathf.Min(types.Length, 3);

            RectTransform towerArea = CreateRect(canvasRoot, "TowerPanel");
            towerArea.anchorMin = new Vector2(0f, 0f);
            towerArea.anchorMax = new Vector2(0f, 0f);
            towerArea.pivot = new Vector2(0f, 0f);
            towerArea.anchoredPosition = new Vector2(20f, 20f);
            towerArea.sizeDelta = new Vector2(25f + count * 250f, 90f);

            ui.towerButtons = new Button[count];

            for (int i = 0; i < count; i++)
            {
                string label = (i + 1) + ". " + types[i].DisplayName + " - " + types[i].Cost;
                Button button = CreateButton(
                    towerArea, "TowerButton_" + i, label,
                    whiteSprite, 28, ButtonNormal,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(i * 250f, 0f), new Vector2(240f, 80f));

                ui.towerButtons[i] = button;
            }
        }

        private static void BuildStartButton(Transform canvasRoot, UIManager ui, Sprite whiteSprite)
        {
            ui.startButton = CreateButton(
                canvasRoot, "StartWaveButton", "Start Wave 1",
                whiteSprite, 34, new Color(0.1f, 0.5f, 0.25f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(340f, 90f));

            ui.startButtonText = ui.startButton.GetComponentInChildren<Text>();
        }

        private static void BuildResultPanels(Transform canvasRoot, UIManager ui, Sprite whiteSprite)
        {
            // 胜利面板
            ui.winPanel = CreateDimPanel(canvasRoot, "WinPanel", PanelDim).gameObject;

            CreateScreenText(
                ui.winPanel.transform, "WinTitle", "VICTORY!",
                86, new Color(1f, 0.9f, 0.3f),
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 120f));

            CreateScreenText(
                ui.winPanel.transform, "WinSubtitle", "All 8 waves defended!",
                40, Color.white,
                new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 60f));

            ui.winRestartButton = CreateButton(
                ui.winPanel.transform, "WinRestart", "Restart",
                whiteSprite, 38, new Color(0.15f, 0.35f, 0.7f, 1f),
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 90f));

            ui.winPanel.SetActive(false);

            // 失败面板
            ui.losePanel = CreateDimPanel(canvasRoot, "LosePanel", PanelDim).gameObject;

            CreateScreenText(
                ui.losePanel.transform, "LoseTitle", "GAME OVER",
                86, new Color(1f, 0.35f, 0.3f),
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 120f));

            CreateScreenText(
                ui.losePanel.transform, "LoseSubtitle", "The core was destroyed...",
                40, Color.white,
                new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 60f));

            ui.loseRestartButton = CreateButton(
                ui.losePanel.transform, "LoseRestart", "Restart",
                whiteSprite, 38, new Color(0.15f, 0.35f, 0.7f, 1f),
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 90f));

            ui.losePanel.SetActive(false);
        }

        private static RectTransform CreateDimPanel(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            image.color = color;
            return rect;
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

            Text text = CreateFullStretchText(rect, "Text", label, fontSize, Color.white);
            text.alignment = TextAnchor.MiddleCenter;

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
            Vector2 size)
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
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateFullStretchText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
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

        // ---------- 白色 UI 精灵 ----------

        private static Sprite EnsureWhiteSprite()
        {
            EnsureFolderExists("Assets/Sprites", "UI");

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

        private static void EnsureFolderExists(string parent, string leaf)
        {
            string path = parent + "/" + leaf;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static void ShowError(string title, string message)
        {
            Debug.LogError("[TD UI] " + title + "：" + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
