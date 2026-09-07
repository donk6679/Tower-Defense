using UnityEngine;

/// <summary>
/// 追踪子弹基类：飞行中持续锁定目标，命中后调用 OnHitTarget。
/// 子类（如 FrostProjectile）可覆盖命中效果。
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField, Min(0.1f)] private float speed = 9f;
    [SerializeField, Min(0.1f)] private float maxLifeTime = 3f;

    protected int damage;

    private Enemy target;
    private float lifeTime;
    private SpriteRenderer spriteRenderer;
    private Color trailColor = Color.white;
    private float trailTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            trailColor = spriteRenderer.color;
    }

    public void Launch(Enemy enemy, int damageAmount)
    {
        target = enemy;
        damage = Mathf.Max(1, damageAmount);
    }

    public void SetFlightSpeed(float value)
    {
        speed = Mathf.Max(0.1f, value);
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        // 每帧按间隔留下光点拖尾
        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f)
        {
            SpawnTrail();
            trailTimer = 0.025f;
        }

        if (lifeTime >= maxLifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = target.transform.position - transform.position;
        float step = speed * Time.deltaTime;

        // 接近到接触距离即判定命中
        if (direction.magnitude <= step + 0.25f)
        {
            OnHitTarget(target);
            VFXBurst.PlayImpact(transform.position, trailColor);
            Destroy(gameObject);
            return;
        }

        transform.position += direction.normalized * step;
    }

    protected virtual void OnHitTarget(Enemy enemy)
    {
        if (enemy != null)
            enemy.TakeDamage(damage);
    }

    private void SpawnTrail()
    {
        float startScale = Mathf.Max(0.06f, transform.localScale.x * 0.55f);
        EffectPulse.Create(
            VFXFactory.GlowSprite,
            trailColor,
            transform.position,
            startScale,
            0f,
            0.14f);
    }
}
