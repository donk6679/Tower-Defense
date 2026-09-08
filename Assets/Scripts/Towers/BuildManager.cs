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
    }

    public void ClearSelection()
    {
        if (selectedIndex != -1)
        {
            selectedIndex = -1;
            SelectionChanged?.Invoke(-1);
        }

        ClearUpgradeTowerSelection();
    }

    public void ClearUpgradeTowerSelection()
    {
        if (selectedUpgradeTower == null)
            return;

        selectedUpgradeTower = null;
        UpgradeSelectionChanged?.Invoke(null);
    }

    /// <summary>
    /// 地块点击统一入口：
    /// 拆除模式 → 拆塔；已占用地块 → 查看/升级该塔；空地 → 尝试建造。
    /// </summary>
    public void OnBuildSlotClicked(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return;

        if (demolishMode)
        {
            TryDemolish(slot);
            return;
        }

        if (slot.IsOccupied)
        {
            SelectUpgradeTower(slot.HasTower ? slot.PlacedTower : null);
            return;
        }

        TryBuild(slot);
    }

    private void SelectUpgradeTower(TowerBase tower)
    {
        if (selectedUpgradeTower == tower)
            return;

        selectedUpgradeTower = tower;
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

    public void TryBuild(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (demolishMode)
            return;

        if (!HasSelection)
        {
            Debug.Log("[Build] 请先选择炮塔（UI 按钮或数字键 1/2/3）");
            return;
        }

        if (slot.IsOccupied)
        {
            Debug.Log("[Build] 该地块已经被占用");
            return;
        }

        TowerTypeConfig config = towerTypes[selectedIndex];
        if (config == null || config.TowerPrefab == null)
        {
            Debug.LogWarning("[Build] 当前塔类型缺少 Prefab 配置");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Build] 找不到 GameManager");
            return;
        }

        if (!GameManager.Instance.TrySpendGold(config.Cost))
        {
            Debug.Log("[Build] 金币不足，需要 " + config.Cost + "，当前 " + GameManager.Instance.Gold);
            return;
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
        slot.PlaceTower(tower, config.Cost);
        Debug.Log("[Build] 建造了 " + config.DisplayName + "，剩余金币 " + GameManager.Instance.Gold, tower);
    }

    public void TryDemolish(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return;

        if (!demolishMode)
        {
            Debug.Log("[Build] 当前不在拆除模式");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Build] 找不到 GameManager");
            return;
        }

        if (!slot.IsOccupied)
        {
            Debug.Log("[Build] 这个地块上没有塔");
            return;
        }

        TowerBase tower = slot.PlacedTower;
        int investedGold = tower != null ? tower.TotalInvestedGold : slot.TowerCost;
        int refund = Mathf.RoundToInt(investedGold * demolishRefundRate);
        TowerBase removedTower = slot.RemoveTower();

        if (removedTower != null)
            Destroy(removedTower.gameObject);

        if (selectedUpgradeTower == removedTower)
            ClearUpgradeTowerSelection();

        if (refund > 0)
            GameManager.Instance.AddGold(refund);

        Debug.Log("[Build] 已拆除炮塔（累计投入 " + investedGold +
                  "），返还 " + refund + " 金币，当前 " + GameManager.Instance.Gold);
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
