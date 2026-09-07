using System;
using UnityEngine;

/// <summary>
/// 一波敌人的完整配置：由若干“敌人组”组成。
/// 每组指定使用哪种敌人 Prefab、刷几只、间隔多久。
/// </summary>
[Serializable]
public sealed class WaveSettings
{
    [SerializeField] private EnemyGroup[] groups = new EnemyGroup[0];

    [Header("Timing")]
    [SerializeField, Min(0f)] private float delayBeforeWave = 1.5f;
    [SerializeField, Min(0f)] private float delayAfterWave = 2f;

    public WaveSettings()
    {
    }

    public WaveSettings(EnemyGroup[] enemyGroups, float beforeDelay, float afterDelay)
    {
        groups = enemyGroups == null ? new EnemyGroup[0] : enemyGroups;
        delayBeforeWave = beforeDelay;
        delayAfterWave = afterDelay;
    }

    public EnemyGroup[] Groups => groups;
    public float DelayBeforeWave => delayBeforeWave;
    public float DelayAfterWave => delayAfterWave;
}

/// <summary>
/// 敌人组：同一波内连续生成的一组相同类型敌人。
/// </summary>
[Serializable]
public sealed class EnemyGroup
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField, Min(1)] private int count = 3;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.2f;

    public EnemyGroup()
    {
    }

    public EnemyGroup(Enemy prefab, int enemyCount, float interval)
    {
        enemyPrefab = prefab;
        count = Mathf.Max(1, enemyCount);
        spawnInterval = Mathf.Max(0.1f, interval);
    }

    public Enemy EnemyPrefab => enemyPrefab;
    public int Count => count;
    public float SpawnInterval => spawnInterval;
}
