using System;
using UnityEngine;

/// <summary>
/// 一波敌人的配置数据。WaveManager 会自动按这个配置刷怪。
/// </summary>
[Serializable]
public sealed class WaveSettings
{
    [Header("Spawn")]
    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.2f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float delayBeforeWave = 1.5f;
    [SerializeField, Min(0f)] private float delayAfterWave = 2f;

    [Header("Enemy")]
    [SerializeField, Min(1)] private int enemyHealth = 5;

    public WaveSettings()
    {
    }

    public WaveSettings(
        int count,
        float interval,
        float beforeDelay,
        float afterDelay,
        int health)
    {
        enemyCount = count;
        spawnInterval = interval;
        delayBeforeWave = beforeDelay;
        delayAfterWave = afterDelay;
        enemyHealth = health;
    }

    public int EnemyCount => enemyCount;
    public float SpawnInterval => spawnInterval;
    public float DelayBeforeWave => delayBeforeWave;
    public float DelayAfterWave => delayAfterWave;
    public int EnemyHealth => enemyHealth;
}
