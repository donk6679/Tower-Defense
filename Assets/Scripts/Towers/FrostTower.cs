using UnityEngine;

/// <summary>
/// 冰霜塔：低伤害、持续减速，配合 FrostProjectile 实现控制效果。
/// 升级可提升减速幅度与持续时间。
/// </summary>
public sealed class FrostTower : TowerBase
{
    protected override void ConfigureProjectile(Projectile projectile)
    {
        if (!(projectile is FrostProjectile frost))
            return;

        TowerLevelStats stats = CurrentLevelStats;
        if (stats != null)
            frost.SetSlowEffect(stats.slowSpeedMultiplier, stats.slowDuration);
    }

    public override string GetUpgradeDescription()
    {
        string description = base.GetUpgradeDescription();
        if (!CanUpgrade)
            return description;

        TowerLevelStats current = CurrentLevelStats;
        TowerLevelStats next = NextLevelStats;

        string slowChange = "\nSlow: " +
                            Mathf.RoundToInt((1f - current.slowSpeedMultiplier) * 100f) +
                            "% -> " +
                            Mathf.RoundToInt((1f - next.slowSpeedMultiplier) * 100f) +
                            "%\nDuration: " +
                            current.slowDuration.ToString("0.0") + "s -> " +
                            next.slowDuration.ToString("0.0") + "s";

        return description + slowChange;
    }
}
