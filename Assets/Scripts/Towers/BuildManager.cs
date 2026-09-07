using System;
using UnityEngine;

/// <summary>
/// 建造管理器：管理可选塔类型、选中状态、扣金币与放置炮塔。
/// 数字键与底部 UI 按钮都可以选塔，随后点击浅绿色地块建造。
/// </summary>
public sealed class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [SerializeField] private TowerTypeConfig[] towerTypes = new TowerTypeConfig[0];

    private int selectedIndex = -1;
    private Transform towerParent;

    public bool HasSelection => selectedIndex >= 0 && selectedIndex < towerTypes.Length;
    public int SelectedIndex => HasSelection ? selectedIndex : -1;
    public TowerTypeConfig[] AvailableTowers => towerTypes;
    public TowerTypeConfig SelectedTower => HasSelection ? towerTypes[selectedIndex] : null;

    /// <summary>选中变化：-1 表示取消选择，否则为塔列表下标。</summary>
    public event Action<int> SelectionChanged;

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
        if (towerTypes == null || towerTypes.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
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
        Debug.Log("[Build] 选择 " + towerTypes[index].DisplayName + "，点击浅绿色地块建造", this);
        SelectionChanged?.Invoke(index);
    }

    public void ClearSelection()
    {
        if (selectedIndex == -1)
            return;

        selectedIndex = -1;
        SelectionChanged?.Invoke(-1);
    }

    public void TryBuild(BuildSlot slot)
    {
        if (slot == null || !slot.isBuildable)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
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

        slot.SetOccupied(true);
        Debug.Log("[Build] 建造了 " + config.DisplayName + "，剩余金币 " + GameManager.Instance.Gold, tower);
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
