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
        private const string VictoryResultPath = "Assets/Sprites/UI/victory_result.png";
        private const string DefeatResultPath = "Assets/Sprites/UI/defeat_result.png";
        private const string NextWaveButtonPath = "Assets/Sprites/UI/next_wave_button.png";
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
            if (waveManager.TotalWaveCount == 0)
            {
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
            }

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

            ui.towerButtons = new Button[0];
            BuildHUD(uiRoot.transform, ui);
            BuildStartButton(uiRoot.transform, ui, whiteSprite);
            BuildUpgradePanel(uiRoot.transform, ui, whiteSprite);
            EnsureUiSpriteImport(VictoryResultPath);
            EnsureUiSpriteImport(DefeatResultPath);
            BuildResultPanels(uiRoot.transform, ui, whiteSprite);

            // 新交互：点击地块后由上下文菜单接管建塔/升级/拆除
            uiRoot.AddComponent<TowerContextMenu>();
        }

        private static void BuildHUD(Transform canvasRoot, UIManager ui)
        {
            RectTransform hudRoot = CreateRect(canvasRoot, "HudPanel");
            hudRoot.anchorMin = new Vector2(0f, 1f);
            hudRoot.anchorMax = new Vector2(0f, 1f);
            hudRoot.pivot = new Vector2(0f, 1f);
            hudRoot.anchoredPosition = new Vector2(24f, -20f);
            hudRoot.sizeDelta = new Vector2(520f, 220f);

            VerticalLayoutGroup hudLayout = hudRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            hudLayout.spacing = 26f;
            hudLayout.childAlignment = TextAnchor.UpperLeft;
            hudLayout.childControlWidth = true;
            hudLayout.childControlHeight = true;
            hudLayout.childForceExpandWidth = false;
            hudLayout.childForceExpandHeight = false;

            Sprite goldSprite = LoadHudSprite("gold");
            Sprite lifeSprite = LoadHudSprite("life");
            Sprite waveSprite = LoadHudSprite("wave");
            Sprite colonSprite = LoadHudSprite("colon");
            Sprite slashSprite = LoadHudSprite("slash");

            // Gold: [icon] : [number]
            RectTransform goldRow = CreateHudRow(hudRoot, "GoldRow", 78f);
            ui.goldIcon = CreateHudIcon(goldRow, "GoldIcon", goldSprite, 104f);
            ui.goldColon = CreateHudIcon(goldRow, "GoldColon", colonSprite, 80f);
            ui.goldNumber = CreateHudNumber(goldRow, "GoldNumber", 184f);

            // Lives: [icon] : [number]
            RectTransform lifeRow = CreateHudRow(hudRoot, "LifeRow", 78f);
            ui.lifeIcon = CreateHudIcon(lifeRow, "LifeIcon", lifeSprite, 104f);
            ui.lifeColon = CreateHudIcon(lifeRow, "LifeColon", colonSprite, 80f);
            ui.lifeNumber = CreateHudNumber(lifeRow, "LifeNumber", 184f);

            // Wave: [icon] [current] / [total]
            RectTransform waveRow = CreateHudRow(hudRoot, "WaveRow", 78f);
            waveRow.GetComponent<HorizontalLayoutGroup>().spacing = -50f;
            ui.waveIcon = CreateHudIcon(waveRow, "WaveIcon", waveSprite, 104f);
            // 只加宽“波次图标 → 第一个数字”的距离，不影响 1 / 3 的其他间距
            CreateHudSpacer(waveRow, "WaveIconGap", 72f);
            ui.waveCurrentNumber = CreateHudNumber(waveRow, "WaveCurrentNumber", 184f);
            ui.waveSlash = CreateHudIcon(waveRow, "WaveSlash", slashSprite, 120f);
            ui.waveTotalNumber = CreateHudNumber(waveRow, "WaveTotalNumber", 184f);
        }

        private static RectTransform CreateHudRow(Transform parent, string name, float height)
        {
            RectTransform row = CreateRect(parent, name);

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = -16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return row;
        }

        private static Image CreateHudIcon(Transform parent, string name, Sprite sprite, float size)
        {
            RectTransform rect = CreateRect(parent, name);

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;
            element.minHeight = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static HudSpriteNumber CreateHudNumber(Transform parent, string name, float height)
        {
            RectTransform rect = CreateRect(parent, name);

            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;

            HudSpriteNumber number = rect.gameObject.AddComponent<HudSpriteNumber>();
            number.digitHeight = height;
            number.spacing = -50f;
            number.digitWidthRatio = 0.55f;
            return number;
        }

        private static void CreateHudSpacer(Transform parent, string name, float width)
        {
            RectTransform rect = CreateRect(parent, name);
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
        }

        private static Sprite LoadHudSprite(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/HudIcons/" + spriteName + ".png");
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
            towerArea.sizeDelta = new Vector2(25f + (count + 1) * 250f, 90f);

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

            ui.demolishButton = CreateButton(
                towerArea, "DemolishButton", "Remove Mode (X)",
                whiteSprite, 28, ButtonNormal,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(count * 250f, 0f), new Vector2(240f, 80f));

            ui.demolishButtonText = ui.demolishButton.GetComponentInChildren<Text>();
        }

        private static void BuildStartButton(Transform canvasRoot, UIManager ui, Sprite whiteSprite)
        {
            EnsureUiSpriteImport(NextWaveButtonPath);
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NextWaveButtonPath);

            const float buttonWidth = 210f;
            const float buttonHeight = 82.5f; // 裁剪后图片比例约 2.55:1
            const float barHeight = 6f;
            const float barGap = 4f;

            RectTransform root = CreateRect(canvasRoot, "WaveButtonRoot");
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 24f);
            root.sizeDelta = new Vector2(buttonWidth, buttonHeight + barGap + barHeight);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // 下一波按钮（图片自带文字，不显示额外金币奖励）
            RectTransform buttonRect = CreateRect(root, "StartWaveButton");
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = buttonSprite;
            buttonImage.preserveAspect = true;
            buttonImage.raycastTarget = true;

            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // 进度条背景
            RectTransform barBackground = CreateRect(root, "WaveProgressBg");
            barBackground.anchorMin = new Vector2(0.5f, 0f);
            barBackground.anchorMax = new Vector2(0.5f, 0f);
            barBackground.pivot = new Vector2(0.5f, 0f);
            barBackground.anchoredPosition = new Vector2(0f, 0f);
            barBackground.sizeDelta = new Vector2(buttonWidth, barHeight);

            Image barBackgroundImage = barBackground.gameObject.AddComponent<Image>();
            barBackgroundImage.sprite = whiteSprite;
            barBackgroundImage.color = new Color(0f, 0f, 0f, 0.35f);
            barBackgroundImage.raycastTarget = false;

            // 进度条填充：左对齐，宽度随时间从 100% 缩到 0
            RectTransform fill = CreateRect(barBackground, "WaveProgressFill");
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(buttonWidth, 0f);

            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = whiteSprite;
            fillImage.color = new Color(1f, 0.85f, 0.2f, 1f);
            fillImage.raycastTarget = false;

            ui.startButton = button;
            ui.startButtonText = null;
            ui.startButtonGroup = group;
            ui.waveProgressFill = fill;
            ui.waveProgressFullWidth = buttonWidth;
        }

        private static void BuildUpgradePanel(Transform canvasRoot, UIManager ui, Sprite whiteSprite)
        {
            RectTransform panel = CreateRect(canvasRoot, "UpgradePanel");
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-20f, 20f);
            panel.sizeDelta = new Vector2(520f, 220f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.type = Image.Type.Simple;
            background.color = new Color(0.08f, 0.12f, 0.18f, 0.94f);

            ui.upgradeInfoText = CreateScreenText(
                panel, "UpgradeInfo", "Click a tower to inspect it",
                28, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -12f), new Vector2(500f, 140f),
                TextAnchor.UpperCenter);

            ui.upgradeButton = CreateButton(
                panel, "UpgradeButton", "Upgrade",
                whiteSprite, 30, new Color(0.2f, 0.45f, 0.25f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(300f, 64f));

            ui.upgradeButtonText = ui.upgradeButton.GetComponentInChildren<Text>();

            ui.upgradePanel = panel.gameObject;
            panel.gameObject.SetActive(false);
        }

        private static void BuildResultPanels(Transform canvasRoot, UIManager ui, Sprite whiteSprite)
        {
            Sprite victorySprite = AssetDatabase.LoadAssetAtPath<Sprite>(VictoryResultPath);
            Sprite defeatSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefeatResultPath);

            // 胜利结算：重玩 / 主菜单
            ui.winPanel = CreateResultPanel(canvasRoot, "WinPanel", victorySprite);
            ui.winRestartButton = CreateInvisibleResultButton(
                ui.winPanel.transform, "WinRestart", whiteSprite,
                new Vector2(0.3459f, 0.15f), new Vector2(820f, 300f));
            ui.winMenuButton = CreateInvisibleResultButton(
                ui.winPanel.transform, "WinMenu", whiteSprite,
                new Vector2(0.6531f, 0.15f), new Vector2(780f, 280f));
            ui.winPanel.SetActive(false);

            // 失败结算：重试 / 主菜单
            ui.losePanel = CreateResultPanel(canvasRoot, "LosePanel", defeatSprite);
            ui.loseRestartButton = CreateInvisibleResultButton(
                ui.losePanel.transform, "LoseRestart", whiteSprite,
                new Vector2(0.3898f, 0.1544f), new Vector2(620f, 260f));
            ui.loseMenuButton = CreateInvisibleResultButton(
                ui.losePanel.transform, "LoseMenu", whiteSprite,
                new Vector2(0.6113f, 0.1544f), new Vector2(640f, 280f));
            ui.losePanel.SetActive(false);
        }

        private static GameObject CreateResultPanel(Transform parent, string name, Sprite sprite)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = true;
            return rect.gameObject;
        }

        private static Button CreateInvisibleResultButton(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchor,
            Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void EnsureUiSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
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
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleLeft)
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
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleLeft)
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
