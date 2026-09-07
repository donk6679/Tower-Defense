using System.Collections;
using UnityEngine;

/// <summary>
/// 波次管理器：自动按 WaveSettings 列表刷怪。
/// 每一波内敌人全部消失后才会进入下一波；所有波次结束触发胜利日志。
/// </summary>
public sealed class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathManager pathManager;
    [SerializeField] private Enemy enemyPrefab;

    [Header("Wave Config")]
    [SerializeField] private WaveSettings[] waves = new WaveSettings[0];

    [Header("Runtime Status (read only)")]
    [SerializeField, Min(0)] private int enemiesAlive;
    [SerializeField, Min(0)] private int currentWaveNumber;

    public int EnemiesAlive => enemiesAlive;
    public int CurrentWaveNumber => currentWaveNumber;
    public bool AllWavesCleared { get; private set; }

    public void Setup(PathManager path, Enemy prefab, WaveSettings[] waveConfigs)
    {
        pathManager = path;
        enemyPrefab = prefab;

        if (waveConfigs != null)
            waves = waveConfigs;
    }

    private void Start()
    {
        if (pathManager == null)
            pathManager = FindObjectOfType<PathManager>();

        if (enemyPrefab == null || pathManager == null || waves == null || waves.Length == 0)
        {
            Debug.LogWarning("[WaveManager] 缺少路径、敌人 Prefab 或波次配置，无法开始波次", this);
            return;
        }

        StartCoroutine(RunAllWaves());
    }

    private IEnumerator RunAllWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            WaveSettings wave = waves[i];
            currentWaveNumber = i + 1;

            Debug.Log("[Wave] 第 " + currentWaveNumber + "/" + waves.Length +
                      " 波将在 " + wave.DelayBeforeWave.ToString("0.#") + " 秒后开始");

            yield return new WaitForSeconds(wave.DelayBeforeWave);

            if (IsGameOver())
                yield break;

            Debug.Log("[Wave] 第 " + currentWaveNumber + " 波开始");

            for (int spawned = 0; spawned < wave.EnemyCount; spawned++)
            {
                if (IsGameOver())
                    yield break;

                SpawnEnemy(wave.EnemyHealth);

                if (spawned < wave.EnemyCount - 1)
                    yield return new WaitForSeconds(wave.SpawnInterval);
            }

            // 等待本波敌人全部消失（被消灭或到达核心）
            while (enemiesAlive > 0 && !IsGameOver())
                yield return null;

            if (IsGameOver())
                yield break;

            Debug.Log("[Wave] 第 " + currentWaveNumber + " 波清除");

            if (i < waves.Length - 1)
                yield return new WaitForSeconds(wave.DelayAfterWave);
        }

        AllWavesCleared = true;
        Debug.Log("[Wave] 所有波次完成，胜利！");
    }

    private void SpawnEnemy(int health)
    {
        Enemy enemy = Instantiate(enemyPrefab, pathManager.SpawnPosition, Quaternion.identity);
        enemiesAlive++;

        enemy.Initialize(pathManager, health);
        enemy.Died += HandleEnemyDied;
        enemy.ReachedBase += HandleEnemyReachedBase;
    }

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        Debug.Log("[Wave] 敌人到达核心（场上剩余 " + enemiesAlive + "）");

        if (GameManager.Instance != null)
            GameManager.Instance.LoseLife();
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        Debug.Log("[Wave] 敌人被消灭（场上剩余 " + enemiesAlive + "）");
    }

    private bool IsGameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameOver;
    }
}
