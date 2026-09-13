using System;
using UnityEngine;

/// <summary>
/// 建造管理器：管理可选塔类型、选中状态、扣金币与放置炮塔。
/// 数字键与底部 UI 按钮都可以选塔，随后点击浅绿色地块建造；
/// X 或底部按钮可切换“拆除模式”，点击已有塔的地块将其拆除并返还金币。
/// </summary>
public sealed class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Demolish")]
    [SerializeField, Range(0f, 1f)] private float demolishRefundRate = 0.5f;

    [SerializeField] private TowerTypeConfig[] towerTypes = new TowerTypeConfig[0];

    private int selectedIndex = -1;
    private Transform towerParent;
    private bool demolishMode;
    private TowerBase selectedUpgradeTower;
    private BuildSlot hoveredBuildSlot;
    private GameObject buildRangePreview;
    private GameObject selectedRangePreview;

    private static readonly Color BuildRangeColor = new Color(0.35f, 1f, 0.7f, 0.6f);
    private static readonly Color SelectedRangeColor = new Color(0.45f, 0.85f, 1f, 0.7f);

    public bool HasSelection => selectedIndex >= 0 && selectedIndex < towerTypes.Length;
    public int SelectedIndex => HasSelection ? selectedIndex : -1;
    public TowerTypeConfig[] AvailableTowers => towerTypes;
    public TowerTypeConfig SelectedTower => HasSelection ? towerTypes[selectedIndex] : null;
    public bool IsDemolishMode => demolishMode;
    public TowerBase SelectedUpgradeTower => selectedUpgradeTower;

    /// <summary>选中变化：-1 表示取消选择，否则为塔列表下标。</summary>
    public event Action<int> SelectionChanged;

    /// <summary>拆除模式开关变化。</summary>
    public event Action<bool> DemolishModeChanged;

    /// <summary>被选中的已有炮塔变化（null 表示取消选中）。</summary>
    public event Action<TowerBase> UpgradeSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            ToggleDemolishMode();
            return;
        }

        if (towerTypes == null || towerTypes.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
            if (demolishMode)
                SetDemolishMode(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectTower(0);
        if (towerTypes.Length > 1 && Input.GetKeyDown(KeyCode.Alpha2))
            SelectTower(1);
        if (towerTypes.Length > 2 && Input.GetKeyDown(KeyCode.Alpha3))
            SelectTower(2);
    }

    public void SetAvailableTowers(TowerTypeConfig[] types)
    {
        towerTypes = types == null ? new TowerTypeConfig[0] : types;
        ClearSelection();
    }

    public void SelectTower(int index)
    {
        if (demolishMode)
            SetDemolishMode(false);

        if (towerTypes == null || index < 0 || index >= towerTypes.Length)
        {
            ClearSelection();
            return;
        }

        if (selectedIndex == index)
        {
            ClearSelection();
            return;
        }

        selectedIndex = index;
        ClearUpgradeTowerSelection();
        Debug.Log("[Build] 选择 " + towerTypes[index].DisplayName + "，点击浅绿色地块建造", this);
        SelectionChanged?.Invoke(index);
        RefreshBuildRangePreview();
    }

    public void ClearSelection()
    {
        if (selectedIndex != -1)
        {
            selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }

        ClearUpgradeTowerSelection();
        HideBuildRangePreview();
    }

    public void ClearUpgradeTowerSelection()
    {
        HideSelectedRangePreview();

        if (selectedUpgradeTower == null)
            return;

        selectedUpgradeTower = null;
        UpgradeSelectionChanged?.Invoke(null);
    }

    /// <summary>鼠标进入/离开建造地块时调用，用于显示建造射程提示。</summary>
    public void NotifyBuildSlotHover(BuildSlot slot, bool entered)
    {
        if (!entered)
        {
            if (hoveredBuildSlot == null || slot == null || slot == hoveredBuildSlot)
                hoveredBuildSlot = null;

            HideBuildRangePreview();
            return;
        }

        hoveredBuildSlot = slot;
        RefreshBuildRangePreview();
    }

    /// <summary>升级面板升级成功后刷新选中塔的射程圈。</summary>
    public void RefreshSelectedTowerRange()
    {
        if (selectedUpgradeTower != null)
            ShowSelectedRangePreview(selectedUpgradeTower);
    }

    private void RefreshBuildRangePreview()
    {
        bool canShow =
            HasSelection &&
            hoveredBuildSlot != null &&
            !hoveredBuildSlot.IsOccupied &&
            !demolishMode &&
            (GameManager.Instance == null || !GameManager.Instance.IsGameOver);

        if (!canShow)
        {
            HideBuildRangePreview();
            return;
        }

        TowerTypeConfig config = towerTypes[selectedIndex];
        if (config == null || config.TowerPrefab == null)
        {
            HideBuildRangePreview();
            return;
        }

        if (buildRangePreview == null)
            buildRangePreview = CreateRangePreviewObject();

        buildRangePreview.transform.SetParent(hoveredBuildSlot.transform, false);
        buildRangePreview.transform.localPosition = Vector3.zero;

        RangeIndicator indicator = buildRangePreview.GetComponent<RangeIndicator>();
        indicator.Show(config.TowerPrefab.Range, BuildRangeColor);
    }

    private void HideBuildRangePreview()
    {
        if (buildRangePreview != null)
            buildRangePreview.SetActive(false);
    }

    private void ShowSelectedRangePreview(TowerBase tower)
    {
        if (tower == null)
        {
            HideSelectedRangePreview();
            return;
        }

        if (selectedRangePreview == null)
            selectedRangePreview = CreateRangePreviewObject();

        selectedRangePreview.transform.SetParent(tower.transform, false);
        selectedRangePreview.transform.localPosition = Vector3.zero;

        RangeIndicator indicator = selectedRangePreview.GetComponent<RangeIndicator>();
        indicator.Show(tower.Range, SelectedRangeColor);
    }

    private void HideSelectedRangePreview()
    {
        if (selectedRangePreview != null)
            selectedRangePreview.SetActive(false);
    }

    private GameObject CreateRangePreviewObject()
    {
        GameObject preview = new GameObject("RangeIndicator");

        SpriteRenderer renderer = preview.AddComponent<SpriteRenderer>();
        renderer.sprite = VFXFactory.RangeSprite;
        renderer.sortingOrder = 4;

        preview.AddComponent<RangeIndicator>();
        return preview;
    }

    /// <summary>
    /// 地块点击统一入口：
    /// 拆除模式 → 拆塔；已占用地块 → 查看/升级该塔；空地 → 尝试建造。
    /// </summary>
    public void OnBuildSlotClicked(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return;

        // 新交互：由上下文菜单接管，点击地块后由玩家在菜单里选择操作
        if (TowerContextMenu.Instance != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            TowerContextMenu.Instance.ShowForSlot(slot);
            return;
        }

        if (demolishMode)
        {
            TryDemolish(slot);
            return;
        }

        if (slot.IsOccupied)
        {
            HideBuildRangePreview();
            SelectUpgradeTower(slot.HasTower ? slot.PlacedTower : null);
            return;
        }

        TryBuild(slot);
    }

    public void SelectUpgradeTower(TowerBase tower)
    {
        if (selectedUpgradeTower == tower)
            return;

        selectedUpgradeTower = tower;
        ShowSelectedRangePreview(tower);
        UpgradeSelectionChanged?.Invoke(tower);
    }

    public void ToggleDemolishMode()
    {
        SetDemolishMode(!demolishMode);
    }

    private void SetDemolishMode(bool value)
    {
        if (demolishMode == value)
            return;

        demolishMode = value;

        if (demolishMode)
            ClearSelection();

        Debug.Log(demolishMode
            ? "[Build] 拆除模式已开启：点击已有塔的地块可拆除"
            : "[Build] 拆除模式已关闭");

        DemolishModeChanged?.Invoke(demolishMode);
    }

    /// <summary>上下文菜单使用：直接按塔类型下标尝试建造。</summary>
    public bool TryBuildByType(BuildSlot slot, int towerTypeIndex)
    {
        if (slot == null || !slot.isBuildable)
            return false;

        if (towerTypes == null || towerTypeIndex < 0 || towerTypeIndex >= towerTypes.Length)
            return false;

        TowerTypeConfig config = towerTypes[towerTypeIndex];
        return PerformBuild(slot, config);
    }

    public void TryBuild(BuildSlot slot)
    {
        if (!HasSelection)
        {
            Debug.Log("[Build] 请先选择炮塔");
            return;
        }

        TryBuildByType(slot, selectedIndex);
    }

    private bool PerformBuild(BuildSlot slot, TowerTypeConfig config)
    {
        if (slot == null || !slot.isBuildable)
            return false;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return false;

        if (demolishMode)
            return false;

        if (slot.IsOccupied)
        {
            Debug.Log("[Build] 该地块已经被占用");
            return false;
        }

        if (config == null || config.TowerPrefab == null)
        {
            Debug.LogWarning("[Build] 当前塔类型缺少 Prefab 配置");
            return false;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Build] 找不到 GameManager");
            return false;
        }

        if (!GameManager.Instance.TrySpendGold(config.Cost))
        {
            Debug.Log("[Build] 金币不足，需要 " + config.Cost + "，当前 " + GameManager.Instance.Gold);
            return false;
        }

        if (towerParent == null)
            towerParent = FindOrCreateTowerParent();

        TowerBase tower = Instantiate(
            config.TowerPrefab,
            slot.transform.position,
            Quaternion.identity,
            towerParent);

        tower.SetInitialCost(config.Cost);
        ClearUpgradeTowerSelection();
        HideBuildRangePreview();
        slot.PlaceTower(tower, config.Cost);
        AudioManager.PlaySfx("build");
        Debug.Log("[Build] 建造了 " + config.DisplayName + "，剩余金币 " + GameManager.Instance.Gold, tower);
        return true;
    }

    public void TryDemolish(BuildSlot slot)
    {
        if (!demolishMode)
        {
            Debug.Log("[Build] 当前不在拆除模式");
            return;
        }

        DemolishSlot(slot);
    }

    /// <summary>上下文菜单使用：不要求先进入拆除模式，直接拆除该地块上的塔。</summary>
    public bool DemolishSlot(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return false;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Build] 找不到 GameManager");
            return false;
        }

        if (!slot.IsOccupied)
        {
            Debug.Log("[Build] 这个地块上没有塔");
            return false;
        }

        TowerBase tower = slot.PlacedTower;
        int investedGold = tower != null ? tower.TotalInvestedGold : slot.TowerCost;
        int refund = GetDemolishRefund(slot);
        TowerBase removedTower = slot.RemoveTower();

        if (removedTower != null)
            Destroy(removedTower.gameObject);

        if (selectedUpgradeTower == removedTower)
            ClearUpgradeTowerSelection();

        if (refund > 0)
            GameManager.Instance.AddGold(refund);

        AudioManager.PlaySfx("demolish");
        Debug.Log("[Build] 已拆除炮塔（累计投入 " + investedGold +
                  "），返还 " + refund + " 金币，当前 " + GameManager.Instance.Gold);
        return true;
    }

    /// <summary>预览拆除返还金额（不实际拆除）。</summary>
    public int GetDemolishRefund(BuildSlot slot)
    {
        if (slot == null)
            return 0;

        TowerBase tower = slot.PlacedTower;
        int investedGold = tower != null ? tower.TotalInvestedGold : slot.TowerCost;
        return Mathf.RoundToInt(investedGold * demolishRefundRate);
    }

    private Transform FindOrCreateTowerParent()
    {
        GameObject towers = GameObject.Find("Towers");
        if (towers == null)
        {
            towers = new GameObject("Towers");
            GameObject map = GameObject.Find("Map");
            if (map != null)
                towers.transform.SetParent(map.transform, false);
        }

        return towers.transform;
    }

    [Serializable]
    public sealed class TowerTypeConfig
    {
        [SerializeField] private string displayName;
        [SerializeField] private TowerBase towerPrefab;
        [SerializeField, Min(0)] private int cost;

        public TowerTypeConfig()
        {
        }

        public TowerTypeConfig(string name, TowerBase prefab, int goldCost)
        {
            displayName = name;
            towerPrefab = prefab;
            cost = goldCost;
        }

        public string DisplayName => displayName;
        public TowerBase TowerPrefab => towerPrefab;
        public int Cost => cost;
    }
}
