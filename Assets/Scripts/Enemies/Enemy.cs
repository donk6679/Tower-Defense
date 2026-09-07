using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 敌人：沿 PathManager 提供的路径点移动，并拥有生命值。
/// 死亡或到达核心时会发出对应事件，由 WaveManager / GameManager 响应。
/// </summary>
public sealed class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 5;

    private PathManager pathManager;
    private int nextWaypointIndex = 1;
    private int currentHealth;
    private bool isDead;
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public float MoveSpeed => moveSpeed;

    /// <summary>敌人到达核心（此时 WaveManager 会通知 GameManager 扣生命）。</summary>
    public event Action<Enemy> ReachedBase;

    /// <summary>敌人被消灭。</summary>
    public event Action<Enemy> Died;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;
    }

    /// <summary>
    /// 绑定路径并可选覆盖生命值。出生位置为路径第一个点，因此从第二个路径点开始前进。
    /// </summary>
    public void Initialize(PathManager path, int healthOverride = -1)
    {
        pathManager = path;
        nextWaypointIndex = 1;

        if (healthOverride > 0)
            maxHealth = healthOverride;

        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayDamageFlash();

        Debug.Log("[Enemy] 受到 " + damage + " 点伤害，剩余生命 " + currentHealth, this);

        if (currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("[Enemy] 被消灭", this);

        Died?.Invoke(this);
        SpawnDeathBurst();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (pathManager == null || pathManager.WaypointCount == 0)
            return;

        // 已经走完所有路径点：到达核心
        if (nextWaypointIndex >= pathManager.WaypointCount)
        {
            ReachBase();
            return;
        }

        Transform target = pathManager.GetWaypoint(nextWaypointIndex);
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        float step = moveSpeed * Time.deltaTime;

        // 一步之内到达：直接吸附到路径点，避免转弯抖动
        if (direction.magnitude <= step)
        {
            transform.position = target.position;
            nextWaypointIndex++;
            return;
        }

        transform.position += direction.normalized * step;
    }

    private void ReachBase()
    {
        Debug.Log("[Enemy] 到达核心", this);
        ReachedBase?.Invoke(this);
        Destroy(gameObject);
    }

    private void PlayDamageFlash()
    {
        if (spriteRenderer == null)
            return;

        StopAllCoroutines();
        StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = originalSpriteColor;
    }

    private void SpawnDeathBurst()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Color burstColor = Color.Lerp(originalSpriteColor, Color.white, 0.55f);

        for (int i = 0; i < 7; i++)
        {
            GameObject shardObject = new GameObject("DeathShard_" + i);
            shardObject.transform.position = transform.position;

            float shardScale = UnityEngine.Random.Range(0.08f, 0.16f);
            shardObject.transform.localScale = new Vector3(shardScale, shardScale, 1f);

            SpriteRenderer shardRenderer = shardObject.AddComponent<SpriteRenderer>();
            shardRenderer.sprite = spriteRenderer.sprite;
            shardRenderer.color = burstColor;
            shardRenderer.sortingOrder = 12;

            DeathShard shard = shardObject.AddComponent<DeathShard>();
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            shard.Launch(direction * UnityEngine.Random.Range(1.5f, 3.5f), UnityEngine.Random.Range(0.25f, 0.5f));
        }
    }
}
