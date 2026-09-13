using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地块上下文菜单：
/// - 点击空地：在上方显示炮塔图标（无文字），点击建造；
/// - 点击已有塔的地块：在上方显示升级（上箭头）与拆除（叉号）；
/// - 建造/选择后或点击其他地方自动关闭。
/// </summary>
public sealed class TowerContextMenu : MonoBehaviour
{
    private const int ButtonSize = 112;
    private const float MenuOffsetAboveTile = 125f;
    private const float CostIconSize = 84f;
    private const float CostDigitHeight = 67.5f;
    private const float OptionSpacing = 4f;

    public static TowerContextMenu Instance { get; private set; }

    private RectTransform menuRect;
    private HorizontalLayoutGroup layoutGroup;
    private BuildManager buildManager;
    private BuildSlot activeSlot;

    private static Sprite whiteSprite;
    private static Sprite arrowSprite;
    private static Sprite crossSprite;
    private static Sprite gunBuildIcon;
    private static Sprite frostBuildIcon;
    private static Sprite sniperBuildIcon;
    private static Sprite upgradeActionIcon;
    private static Sprite maxLevelActionIcon;
    private static Sprite demolishActionIcon;
    private static Sprite costGoldIcon;

    private static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
                whiteSprite = CreateSolidWhiteSprite();
            return whiteSprite;
        }
    }

    private static Sprite ArrowSprite
    {
        get
        {
            if (arrowSprite == null)
                arrowSprite = CreateArrowSprite();
            return arrowSprite;
        }
    }

    private static Sprite CrossSprite
    {
        get
        {
            if (crossSprite == null)
                crossSprite = CreateCrossSprite();
            return crossSprite;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        buildManager = FindObjectOfType<BuildManager>();

        CreatePanel();
        Hide();
    }

    private void Update()
    {
        if (menuRect == null || !menuRect.gameObject.activeSelf)
            return;

        // 点击“其他地方”关闭菜单。
        // 点击其他建造地块时交给该地块的 OnMouseDown 重新打开，这里不抢着关闭。
        if (!Input.GetMouseButtonDown(0))
            return;

        // 鼠标按在菜单本身的图标/背景上时不要关闭，
        // 否则会在按钮 onClick（鼠标抬起）之前把按钮销毁掉。
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (TryGetMouseBuildSlot() != null)
            return;

        Hide();
    }

    public void ShowForSlot(BuildSlot slot)
    {
        if (slot == null)
        {
            Hide();
            return;
        }

        activeSlot = slot;
        ClearChildren();

        if (slot.IsOccupied && slot.HasTower)
            AddTowerActions(slot);
        else if (!slot.IsOccupied)
            AddTowerBuildOptions(slot);
        else
        {
            Hide();
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
        PositionAboveTile(slot);
        menuRect.gameObject.SetActive(true);
    }

    public void Hide()
    {
        activeSlot = null;
        ClearChildren();

        if (menuRect != null)
            menuRect.gameObject.SetActive(false);
    }

    private void AddTowerBuildOptions(BuildSlot slot)
    {
        BuildManager.TowerTypeConfig[] types = buildManager.AvailableTowers;
        if (types == null || types.Length == 0)
            return;

        for (int i = 0; i < types.Length; i++)
        {
            BuildManager.TowerTypeConfig config = types[i];
            if (config == null || config.TowerPrefab == null)
                continue;

            Sprite buildIcon = GetBuildIcon(config.TowerPrefab);
            Sprite iconSprite = buildIcon != null ? buildIcon : VFXFactory.GlowSprite;
            Color iconColor = buildIcon != null ? Color.white : GetTowerColor(config.TowerPrefab);
            Button button = CreateActionOption(
                iconSprite,
                iconColor,
                buildIcon != null,
                config.Cost);

            int index = i;
            button.onClick.AddListener(() =>
            {
                buildManager.TryBuildByType(slot, index);
                Hide();
            });
        }
    }

    private void AddTowerActions(BuildSlot slot)
    {
        TowerBase tower = slot.PlacedTower;
        if (tower == null)
        {
            Hide();
            return;
        }

        bool isMaxLevel = tower.CurrentLevel >= tower.MaxLevel;

        // 升级：普通时黄色上箭头，满级时灰色上箭头且不可点击
        Sprite upgradeSprite = isMaxLevel
            ? LoadActionIcon(ref maxLevelActionIcon, "max_level")
            : LoadActionIcon(ref upgradeActionIcon, "upgrade");

        Button upgradeButton = CreateActionOption(
            upgradeSprite != null ? upgradeSprite : ArrowSprite,
            upgradeSprite != null
                ? Color.white
                : isMaxLevel
                    ? new Color(0.6f, 0.6f, 0.6f, 1f)
                    : new Color(0.95f, 0.85f, 0.3f, 1f),
            upgradeSprite != null,
            isMaxLevel ? -1 : tower.NextUpgradeCost);

        if (isMaxLevel)
        {
            upgradeButton.interactable = false;
        }
        else
        {
            upgradeButton.onClick.AddListener(() =>
            {
                tower.TryUpgrade();
                Hide();
            });
        }

        // 拆除：红色叉号
        Sprite demolishSprite = LoadActionIcon(ref demolishActionIcon, "demolish");
        Button demolishButton = CreateActionOption(
            demolishSprite != null ? demolishSprite : CrossSprite,
            demolishSprite != null ? Color.white : new Color(1f, 0.4f, 0.35f, 1f),
            demolishSprite != null,
            buildManager.GetDemolishRefund(slot));

        demolishButton.onClick.AddListener(() =>
        {
            buildManager.DemolishSlot(slot);
            Hide();
        });
    }

    private static Sprite LoadActionIcon(ref Sprite cache, string iconName)
    {
        if (cache == null)
            cache = Resources.Load<Sprite>("UiIcons/" + iconName);
        return cache;
    }

    private static Sprite GetBuildIcon(TowerBase towerPrefab)
    {
        if (towerPrefab is GunTower)
        {
            if (gunBuildIcon == null)
                gunBuildIcon = Resources.Load<Sprite>("TowerIcons/gun_lv1");
            return gunBuildIcon;
        }

        if (towerPrefab is FrostTower)
        {
            if (frostBuildIcon == null)
                frostBuildIcon = Resources.Load<Sprite>("TowerIcons/frost_lv1");
            return frostBuildIcon;
        }

        if (towerPrefab is SniperTower)
        {
            if (sniperBuildIcon == null)
                sniperBuildIcon = Resources.Load<Sprite>("TowerIcons/sniper_lv1");
            return sniperBuildIcon;
        }

        return null;
    }

    private Button CreateActionOption(
        Sprite iconSprite,
        Color iconColor,
        bool preserveAspect,
        int cost)
    {
        bool showCost = cost >= 0;
        float costRowHeight = Mathf.Max(CostIconSize, CostDigitHeight);
        float optionHeight = ButtonSize + (showCost ? OptionSpacing + costRowHeight : 0f);

        GameObject optionObject = new GameObject("ContextOption", typeof(RectTransform));
        optionObject.transform.SetParent(menuRect, false);

        RectTransform optionRect = (RectTransform)optionObject.transform;
        optionRect.sizeDelta = new Vector2(ButtonSize, optionHeight);

        LayoutElement optionLayout = optionObject.AddComponent<LayoutElement>();
        optionLayout.preferredWidth = ButtonSize;
        optionLayout.preferredHeight = optionHeight;
        optionLayout.minHeight = optionHeight;

        VerticalLayoutGroup vertical = optionObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = OptionSpacing;
        vertical.childAlignment = TextAnchor.MiddleCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;

        // 透明背景负责接收点击，图标和金额只是显示
        Image hitArea = optionObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;

        Button button = optionObject.AddComponent<Button>();
        button.targetGraphic = hitArea;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(optionObject.transform, false);

        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = iconSprite;
        icon.color = iconColor;
        icon.type = Image.Type.Simple;
        icon.preserveAspect = preserveAspect;
        icon.raycastTarget = false;

        LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = ButtonSize;
        iconLayout.preferredHeight = ButtonSize;
        iconLayout.minHeight = ButtonSize;

        if (showCost)
            CreateCostRow(optionObject.transform, cost);

        return button;
    }

    private void CreateCostRow(Transform parent, int cost)
    {
        GameObject rowObject = new GameObject("Cost", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);

        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        float costRowHeight = Mathf.Max(CostIconSize, CostDigitHeight);
        rowLayout.preferredHeight = costRowHeight;
        rowLayout.minHeight = costRowHeight;

        HorizontalLayoutGroup row = rowObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = -4f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        GameObject goldObject = new GameObject("GoldIcon", typeof(RectTransform));
        goldObject.transform.SetParent(rowObject.transform, false);

        Image gold = goldObject.AddComponent<Image>();
        gold.sprite = GetCostGoldIcon();
        gold.preserveAspect = true;
        gold.raycastTarget = false;

        LayoutElement goldLayout = goldObject.AddComponent<LayoutElement>();
        goldLayout.preferredWidth = CostIconSize;
        goldLayout.preferredHeight = CostIconSize;
        goldLayout.minHeight = CostIconSize;

        GameObject numberObject = new GameObject("CostNumber", typeof(RectTransform));
        numberObject.transform.SetParent(rowObject.transform, false);

        LayoutElement numberLayout = numberObject.AddComponent<LayoutElement>();
        numberLayout.preferredHeight = CostDigitHeight;
        numberLayout.minHeight = CostDigitHeight;

        HudSpriteNumber number = numberObject.AddComponent<HudSpriteNumber>();
        number.digitHeight = CostDigitHeight;
        // 方块贴图有透明边距：宽度放开到 1 倍高度，数字才会按高度完整显示；
        // 再用负间距抵消贴图自身的左右留白。
        number.spacing = -18f;
        number.digitWidthRatio = 1f;
        number.SetNumber(cost);
    }

    private static Sprite GetCostGoldIcon()
    {
        if (costGoldIcon == null)
            costGoldIcon = Resources.Load<Sprite>("HudIcons/gold");
        return costGoldIcon;
    }

    private void CreatePanel()
    {
        GameObject panelObject = new GameObject("TowerContextMenu", typeof(RectTransform));
        panelObject.transform.SetParent(transform, false);

        menuRect = (RectTransform)panelObject.transform;
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(100f, 100f);

        Image background = panelObject.AddComponent<Image>();
        background.sprite = WhiteSprite;
        background.color = new Color(0.06f, 0.1f, 0.15f, 0.88f);

        layoutGroup = panelObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 12f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // 让菜单面板根据图标数量自动撑大，避免图标被压缩
        ContentSizeFitter sizeFitter = panelObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ClearChildren()
    {
        if (menuRect == null)
            return;

        for (int i = menuRect.childCount - 1; i >= 0; i--)
            Destroy(menuRect.GetChild(i).gameObject);
    }

    private void PositionAboveTile(BuildSlot slot)
    {
        if (Camera.main == null)
            return;

        RectTransform canvasRect = (RectTransform)transform;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(slot.transform.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfMenu = menuRect.rect.size * 0.5f;

        float x = Mathf.Clamp(localPoint.x, -halfCanvas.x + halfMenu.x + 12f, halfCanvas.x - halfMenu.x - 12f);
        float y = localPoint.y + MenuOffsetAboveTile;

        // 靠近屏幕顶部时改为显示在下方，避免菜单被裁掉
        if (y + halfMenu.y > halfCanvas.y - 10f)
            y = localPoint.y - MenuOffsetAboveTile;

        menuRect.anchoredPosition = new Vector2(x, y);
    }

    private BuildSlot TryGetMouseBuildSlot()
    {
        if (Camera.main == null)
            return null;

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint);
        return hit != null ? hit.GetComponentInParent<BuildSlot>() : null;
    }

    private static Color GetTowerColor(TowerBase tower)
    {
        SpriteRenderer renderer = tower != null ? tower.GetComponent<SpriteRenderer>() : null;
        return renderer != null ? renderer.color : Color.white;
    }

    // ---------- 程序化图标 ----------

    private static Sprite CreateSolidWhiteSprite()
    {
        const int size = 4;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private static Sprite CreateArrowSprite()
    {
        const int size = 32;
        Texture2D texture = CreateEmptyIconTexture(size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centerX = x + 0.5f - size * 0.5f;
                float rowT = y / (float)(size - 1); // 0=顶部, 1=底部
                float maxHalf = (size * 0.5f) * (1f - rowT) * 0.9f;

                bool inTriangle = Mathf.Abs(centerX) <= maxHalf && y < size - 3;
                bool inStem = y >= size * 0.7f && Mathf.Abs(centerX) <= 3f;

                pixels[y * size + x] = inTriangle || inStem
                    ? Color.white
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private static Sprite CreateCrossSprite()
    {
        const int size = 32;
        const int thickness = 3;
        Texture2D texture = CreateEmptyIconTexture(size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onFirstDiagonal = Mathf.Abs(x - y) <= thickness;
                bool onSecondDiagonal = Mathf.Abs((size - 1 - x) - y) <= thickness;
                pixels[y * size + x] = onFirstDiagonal || onSecondDiagonal
                    ? Color.white
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private static Texture2D CreateEmptyIconTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
