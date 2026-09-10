using System;
using UnityEngine;

/// <summary>
/// 炮塔基类：持续锁定射程内最接近终点的敌人，炮口朝向目标，
/// 冷却结束后发射追踪子弹。升级等级数量由 TowerLevelStats 决定。
/// </summary>
public class TowerBase : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Tower";

    [Header("Levels")]
    [SerializeField] private TowerLevelStats[] levelStats = new TowerLevelStats[0];

    [Header("Aiming & Visual")]
    [Tooltip("炮口默认朝向与 +X 轴的夹角补偿：图标朝上填 -90，朝右填 0。")]
    [SerializeField] private float aimAngleOffset = -90f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField] private Sprite[] levelSprites = new Sprite[0];

    [Header("Combat (fallback when no level data)")]
    [SerializeField, Min(0.1f)] private float range = 3f;
    [SerializeField, Min(1)] private int damage = 5;
    [SerializeField, Min(0.05f)] private float fireRate = 2f;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;

    private int currentLevel = 1;
    private float attackCooldown;
    private int totalInvestedGold;
    private Enemy currentTarget;
    private SpriteRenderer bodyRenderer;

    public string DisplayName => displayName;
    public int CurrentLevel => currentLevel;
    public int TotalInvestedGold => totalInvestedGold;
    public int MaxLevel => levelStats == null || levelStats.Length == 0 ? 1 : levelStats.Length;
    public bool CanUpgrade => currentLevel < MaxLevel && !IsGameOver();
    public int NextUpgradeCost => CanUpgrade ? levelStats[currentLevel].upgradeCost : 0;

    private void Awake()
    {
        bodyRenderer = GetComponent<SpriteRenderer>();
        ApplyLevelSprite();
    }

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

    /// <summary>编辑器配置升级档案：显示名 + 每级完整数据。</summary>
    public void SetUpgradeProfile(string towerName, TowerLevelStats[] levels)
    {
        displayName = towerName;
        levelStats = levels == null ? new TowerLevelStats[0] : levels;
        currentLevel = 1;
    }

    /// <summary>编辑器配置：炮口朝向补偿与转向速度。</summary>
    public void SetAimSettings(float angleOffset, float degreesPerSecond)
    {
        aimAngleOffset = angleOffset;
        turnSpeed = Mathf.Max(0f, degreesPerSecond);
    }

    /// <summary>编辑器配置：1 级、2 级……对应的场内外观。</summary>
    public void SetLevelSprites(Sprite[] sprites)
    {
        levelSprites = sprites == null ? new Sprite[0] : sprites;
        ApplyLevelSprite();
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
        ApplyLevelSprite();
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
        if (IsGameOver())
            return;

        AcquireTarget();
        AimAtCurrentTarget();

        if (projectilePrefab == null || currentTarget == null)
            return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f)
            return;

        attackCooldown = 1f / Mathf.Max(FireRate, 0.05f);
        FireAt(currentTarget);
    }

    /// <summary>
    /// 保持当前目标直到它死亡或离开射程，再重新寻找，
    /// 避免炮口在多个敌人之间来回摆动。
    /// </summary>
    private void AcquireTarget()
    {
        if (currentTarget != null && !currentTarget.IsDead && IsInRange(currentTarget))
            return;

        currentTarget = FindTarget();
    }

    private bool IsInRange(Enemy enemy)
    {
        if (enemy == null)
            return false;

        return Vector3.Distance(transform.position, enemy.transform.position) <= Range;
    }

    private void AimAtCurrentTarget()
    {
        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + aimAngleOffset;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        if (turnSpeed <= 0f)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
    }

    private void ApplyLevelSprite()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        if (bodyRenderer == null || levelSprites == null || levelSprites.Length == 0)
            return;

        int index = Mathf.Clamp(currentLevel - 1, 0, levelSprites.Length - 1);
        if (levelSprites[index] != null)
            bodyRenderer.sprite = levelSprites[index];
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
