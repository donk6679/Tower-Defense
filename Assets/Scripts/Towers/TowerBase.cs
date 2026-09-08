using System;
using UnityEngine;

/// <summary>
/// 炮塔基类：周期性在射程内寻找目标并发射追踪子弹。
/// 支持 3 级升级：每个等级独立配置射程/伤害/攻速，
/// 冰霜塔通过 FrostTower 额外使用每级的减速参数。
/// </summary>
public class TowerBase : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Tower";

    [Header("Levels")]
    [SerializeField] private TowerLevelStats[] levelStats = new TowerLevelStats[0];

    [Header("Combat (fallback when no level data)")]
    [SerializeField, Min(0.1f)] private float range = 3f;
    [SerializeField, Min(1)] private int damage = 5;
    [SerializeField, Min(0.05f)] private float fireRate = 2f;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;

    private int currentLevel = 1;
    private float attackCooldown;
    private int totalInvestedGold;

    public string DisplayName => displayName;
    public int CurrentLevel => currentLevel;
    public int TotalInvestedGold => totalInvestedGold;
    public int MaxLevel => levelStats == null || levelStats.Length == 0 ? 1 : levelStats.Length;
    public bool CanUpgrade => currentLevel < MaxLevel && !IsGameOver();
    public int NextUpgradeCost => CanUpgrade ? levelStats[currentLevel].upgradeCost : 0;

    protected TowerLevelStats CurrentLevelStats
    {
        get
        {
            if (levelStats == null || levelStats.Length == 0)
                return null;

            int index = Mathf.Clamp(currentLevel, 1, levelStats.Length) - 1;
            return levelStats[index];
        }
    }

    protected TowerLevelStats NextLevelStats
    {
        get
        {
            if (!CanUpgrade)
                return null;

            return levelStats[currentLevel];
        }
    }

    public float Range => CurrentLevelStats != null ? CurrentLevelStats.range : range;
    public int Damage => CurrentLevelStats != null ? CurrentLevelStats.damage : damage;
    public float FireRate => CurrentLevelStats != null ? CurrentLevelStats.fireRate : fireRate;

    public virtual void Configure(
        float newRange,
        int newDamage,
        float newFireRate,
        Projectile newProjectile)
    {
        range = newRange;
        damage = newDamage;
        fireRate = newFireRate;
        projectilePrefab = newProjectile;
    }

    /// <summary>编辑器配置升级档案：显示名 + 1~3 级完整数据。</summary>
    public void SetUpgradeProfile(string towerName, TowerLevelStats[] levels)
    {
        displayName = towerName;
        levelStats = levels == null ? new TowerLevelStats[0] : levels;
        currentLevel = 1;
    }

    /// <summary>建造完成时记录造价，用于拆除返还时累计已投入金币。</summary>
    public void SetInitialCost(int buildCost)
    {
        totalInvestedGold = Mathf.Max(0, buildCost);
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
        {
            Debug.Log("[Tower] " + displayName + " 已达到最高等级或游戏已结束", this);
            return false;
        }

        TowerLevelStats next = levelStats[currentLevel];
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Tower] 找不到 GameManager", this);
            return false;
        }

        if (!GameManager.Instance.TrySpendGold(next.upgradeCost))
        {
            Debug.Log("[Tower] 金币不足，升级需要 " + next.upgradeCost, this);
            return false;
        }

        totalInvestedGold += next.upgradeCost;
        currentLevel++;
        Debug.Log("[Tower] " + displayName + " 升级到 Lv" + currentLevel +
                  "，剩余金币 " + GameManager.Instance.Gold, this);
        return true;
    }

    /// <summary>供升级面板显示“下一级变强了什么”。</summary>
    public virtual string GetUpgradeDescription()
    {
        string head = displayName + "  Lv" + currentLevel + " / " + MaxLevel;

        if (!CanUpgrade)
            return head + "\nMAX LEVEL";

        TowerLevelStats current = CurrentLevelStats;
        TowerLevelStats next = levelStats[currentLevel];

        string body = head + "\n";

        if (next.damage != current.damage)
            body += "Damage: " + current.damage + " -> " + next.damage + "\n";

        if (Mathf.Abs(next.range - current.range) > 0.001f)
            body += "Range: " + current.range.ToString("0.0") +
                    " -> " + next.range.ToString("0.0") + "\n";

        if (Mathf.Abs(next.fireRate - current.fireRate) > 0.001f)
            body += "Fire rate: " + current.fireRate.ToString("0.0") +
                    " -> " + next.fireRate.ToString("0.0") + "\n";

        body += "Cost: " + next.upgradeCost;
        return body;
    }

    private void Update()
    {
        if (projectilePrefab == null)
            return;

        if (IsGameOver())
            return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f)
            return;

        Enemy target = FindTarget();
        if (target == null)
            return;

        attackCooldown = 1f / Mathf.Max(FireRate, 0.05f);
        FireAt(target);
    }

    /// <summary>
    /// 优先选择“路径进度最大”（最接近终点）的敌人；
    /// 进度相同时选择距离更近的。
    /// </summary>
    private Enemy FindTarget()
    {
        Enemy bestTarget = null;
        int bestProgress = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < Enemy.ActiveEnemies.Count; i++)
        {
            Enemy candidate = Enemy.ActiveEnemies[i];
            if (candidate == null || candidate.IsDead)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance > Range)
                continue;

            int progress = candidate.WaypointProgress;
            if (progress > bestProgress ||
                (progress == bestProgress && distance < bestDistance))
            {
                bestTarget = candidate;
                bestProgress = progress;
                bestDistance = distance;
            }
        }

        return bestTarget;
    }

    private void FireAt(Enemy target)
    {
        Vector3 startPosition = firePoint != null ? firePoint.position : transform.position;
        Projectile projectile = Instantiate(projectilePrefab, startPosition, Quaternion.identity);
        ConfigureProjectile(projectile);
        projectile.Launch(target, Damage);
    }

    /// <summary>子类可在开火前调整子弹（例如冰霜塔写入当前等级的减速参数）。</summary>
    protected virtual void ConfigureProjectile(Projectile projectile)
    {
    }

    private bool IsGameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameOver;
    }
}
