using UnityEngine;

/// <summary>
/// 炮塔基类：周期性在射程内寻找目标，并发射追踪子弹。
/// 后续冰霜塔/狙击塔可以通过继承本类扩展行为。
/// </summary>
public class TowerBase : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float range = 3f;
    [SerializeField, Min(1)] private int damage = 5;
    [SerializeField, Min(0.05f)] private float fireRate = 2f; // 每秒射击次数
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;

    private float attackCooldown;

    public float Range => range;
    public int Damage => damage;
    public float FireRate => fireRate;

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

    private void Update()
    {
        if (projectilePrefab == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f)
            return;

        Enemy target = FindTarget();
        if (target == null)
            return;

        attackCooldown = 1f / Mathf.Max(fireRate, 0.05f);
        FireAt(target);
    }

    /// <summary>
    /// 优先选择“路径进度最大”（最接近终点）的敌人；
    /// 进度相同时选择距离更近的，避免在转弯处乱换目标。
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
            if (distance > range)
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
        projectile.Launch(target, damage);
    }
}
