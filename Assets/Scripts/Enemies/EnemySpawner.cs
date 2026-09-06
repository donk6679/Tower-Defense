using System.Collections;
using UnityEngine;

/// <summary>
/// 测试用敌人生成器：按固定间隔生成若干敌人，用于验证路径移动。
/// 后续实现正式波次系统时会被 WaveManager 替代或扩展。
/// </summary>
public sealed class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathManager pathManager;
    [SerializeField] private Enemy enemyPrefab;

    [Header("Test Spawn Settings")]
    [SerializeField, Min(1)] private int totalToSpawn = 5;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;

    private int spawnedCount;

    public void Setup(PathManager path, Enemy prefab)
    {
        pathManager = path;
        enemyPrefab = prefab;
    }

    private void Start()
    {
        if (pathManager == null)
            pathManager = FindObjectOfType<PathManager>();

        if (enemyPrefab == null || pathManager == null)
        {
            Debug.LogWarning("[EnemySpawner] 缺少 enemyPrefab 或 pathManager 引用，无法生成敌人", this);
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (spawnedCount < totalToSpawn)
        {
            spawnedCount++;
            SpawnOneEnemy();

            if (spawnedCount < totalToSpawn)
                yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("[EnemySpawner] 测试敌人已全部生成", this);
    }

    private void SpawnOneEnemy()
    {
        Enemy enemy = Instantiate(enemyPrefab, pathManager.SpawnPosition, Quaternion.identity);
        enemy.Initialize(pathManager);
    }
}
