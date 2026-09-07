using System.Collections;
using UnityEngine;

/// <summary>
/// 波次管理器：按 WaveSettings 中的“敌人组”顺序刷怪。
/// 一组刷完立刻刷下一组；整个波次的敌人全部消失后才进入下一波。
/// </summary>
public sealed class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathManager pathManager;

    [Header("Wave Config")]
    [SerializeField] private WaveSettings[] waves = new WaveSettings[0];

    [Header("Runtime Status (read only)")]
    [SerializeField, Min(0)] private int enemiesAlive;
    [SerializeField, Min(0)] private int currentWaveNumber;

    public int EnemiesAlive => enemiesAlive;
    public int CurrentWaveNumber => currentWaveNumber;
    public bool AllWavesCleared { get; private set; }

    public void Setup(PathManager path, WaveSettings[] waveConfigs)
    {
        pathManager = path;
        waves = waveConfigs == null ? new WaveSettings[0] : waveConfigs;
    }

    private void Start()
    {
        if (pathManager == null)
            pathManager = FindObjectOfType<PathManager>();

        if (pathManager == null || waves == null || waves.Length == 0)
        {
            Debug.LogWarning("[WaveManager] 缺少路径或波次配置，无法开始波次", this);
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

            for (int groupIndex = 0; groupIndex < wave.Groups.Length; groupIndex++)
            {
                EnemyGroup group = wave.Groups[groupIndex];
                if (group == null || group.EnemyPrefab == null)
                    continue;

                for (int spawned = 0; spawned < group.Count; spawned++)
                {
                    if (IsGameOver())
                        yield break;

                    SpawnEnemy(group.EnemyPrefab);

                    if (spawned < group.Count - 1)
                        yield return new WaitForSeconds(group.SpawnInterval);
                }
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

    private void SpawnEnemy(Enemy enemyPrefab)
    {
        Enemy enemy = Instantiate(enemyPrefab, pathManager.SpawnPosition, Quaternion.identity);
        enemiesAlive++;

        enemy.Initialize(pathManager);
        enemy.Died += HandleEnemyDied;
        enemy.ReachedBase += HandleEnemyReachedBase;
    }

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        Debug.Log("[Wave] " + enemy.name + " 到达核心（场上剩余 " + enemiesAlive + "）");

        if (GameManager.Instance != null)
            GameManager.Instance.LoseLife();
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        if (GameManager.Instance != null && enemy != null)
            GameManager.Instance.AddGold(enemy.GoldReward);

        Debug.Log("[Wave] " + (enemy != null ? enemy.name : "?") +
                  " 被消灭，获得 " + (enemy != null ? enemy.GoldReward : 0) +
                  " 金币（场上剩余 " + enemiesAlive + "）");
    }

    private bool IsGameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGameOver;
    }
}
