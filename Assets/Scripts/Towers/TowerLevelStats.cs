using System;
using UnityEngine;

/// <summary>
/// 单个炮塔等级的数据。
/// upgradeCost 表示“升到该等级”所需金币：
/// 1 级用不到，2 级填写 1→2 的费用。当前升级上限为 2 级。
/// </summary>
[Serializable]
public sealed class TowerLevelStats
{
    [Header("Combat")]
    public float range;
    public int damage;
    public float fireRate;

    [Header("Slow (only Frost)")]
    public float slowSpeedMultiplier = 1f; // 1 = 不减速，0.6 = 减速 40%
    public float slowDuration;

    [Header("Cost to reach this level")]
    public int upgradeCost;

    public TowerLevelStats()
    {
    }

    public TowerLevelStats(
        float towerRange,
        int towerDamage,
        float towerFireRate,
        float slowMultiplier,
        float slowSeconds,
        int cost)
    {
        range = towerRange;
        damage = towerDamage;
        fireRate = towerFireRate;
        slowSpeedMultiplier = slowMultiplier;
        slowDuration = slowSeconds;
        upgradeCost = cost;
    }
}
