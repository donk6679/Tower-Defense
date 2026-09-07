using UnityEngine;

/// <summary>
/// 冰霜子弹：命中造成伤害并给敌人附加减速效果。
/// </summary>
public sealed class FrostProjectile : Projectile
{
    [Header("Slow Effect")]
    [SerializeField, Range(0.05f, 1f)] private float moveSpeedMultiplier = 0.6f;
    [SerializeField, Range(0.1f, 10f)] private float slowDuration = 2f;

    public void SetSlowEffect(float multiplier, float duration)
    {
        moveSpeedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        slowDuration = Mathf.Max(0.1f, duration);
    }

    protected override void OnHitTarget(Enemy enemy)
    {
        base.OnHitTarget(enemy);

        if (enemy != null && !enemy.IsDead)
            enemy.ApplySlow(moveSpeedMultiplier, slowDuration);
    }
}
