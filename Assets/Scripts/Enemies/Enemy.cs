using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人：沿 PathManager 路径移动，可被减速，拥有类型化属性（血量/速度/金币）。
/// 存活敌人会登记到 ActiveEnemies，供炮塔索敌使用。
/// </summary>
public sealed class Enemy : MonoBehaviour
{
    /// <summary>当前场上所有存活敌人的列表，由敌人自行登记/注销。</summary>
    public static readonly List<Enemy> ActiveEnemies = new List<Enemy>(64);

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 10;

    [Header("Reward")]
    [SerializeField, Min(0)] private int goldReward = 5;

    [Header("Slow Effect Visual")]
    [SerializeField] private Color slowTint = new Color(0.3f, 0.8f, 1f, 1f);

    private PathManager pathManager;
    private int nextWaypointIndex = 1;
    private int currentHealth;
    private bool isDead;
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor;

    private float slowRemaining;
    private float moveSpeedMultiplier = 1f;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public float MoveSpeed => moveSpeed;
    public int GoldReward => goldReward;

    /// <summary>当前目标路径点索引，用于炮塔判断哪个敌人最接近终点。</summary>
    public int WaypointProgress => nextWaypointIndex;

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

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    /// <summary>
    /// 绑定路径。出生位置为路径第一个点，因此从第二个路径点开始前进。
    /// </summary>
    public void Initialize(PathManager path)
    {
        pathManager = path;
        nextWaypointIndex = 1;
        currentHealth = maxHealth;
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(0.01f, value);
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = maxHealth;
    }

    public void SetGoldReward(int value)
    {
        goldReward = Mathf.Max(0, value);
    }

    /// <summary>
    /// 施加减速。multiplier 是速度倍率（0.6 = 减速 40%），duration 为持续秒数。
    /// 重复施加时保留更慢的倍率与更长的剩余时间。
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (isDead || duration <= 0f)
            return;

        moveSpeedMultiplier = Mathf.Min(moveSpeedMultiplier, Mathf.Clamp(multiplier, 0.05f, 1f));
        slowRemaining = Mathf.Max(slowRemaining, duration);
        RefreshVisual();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayDamageFlash();

        if (currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Died?.Invoke(this);
        SpawnDeathBurst();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (slowRemaining > 0f)
        {
            slowRemaining -= Time.deltaTime;
            if (slowRemaining <= 0f)
            {
                slowRemaining = 0f;
                moveSpeedMultiplier = 1f;
                RefreshVisual();
            }
        }

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
        float step = moveSpeed * moveSpeedMultiplier * Time.deltaTime;

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
        Debug.Log("[Enemy] " + name + " 到达核心", this);
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
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = slowRemaining > 0f
            ? Color.Lerp(originalSpriteColor, slowTint, 0.65f)
            : originalSpriteColor;
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
