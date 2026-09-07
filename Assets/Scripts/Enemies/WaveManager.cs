using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 波次管理器（手动推进）：
/// 点击 StartNextWave 才开始下一波；一波内敌人全部消失后，
/// UI 上的“开始下一波”按钮会重新可用。
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

    private bool waveInProgress;

    public int EnemiesAlive => enemiesAlive;
    public int CurrentWaveNumber => currentWaveNumber;
    public int TotalWaveCount => waves == null ? 0 : waves.Length;
    public bool AllWavesCleared { get; private set; }
    public bool CanStartNextWave =>
        !waveInProgress &&
        enemiesAlive <= 0 &&
        currentWaveNumber < TotalWaveCount &&
        !IsGameOver();

    /// <summary>参数：当前波号、总波数。</summary>
    public event Action<int, int> WaveBegan;
    public event Action<int, int> WaveCleared;

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
            Debug.LogWarning("[WaveManager] 缺少路径或波次配置", this);
    }

    public void StartNextWave()
    {
        if (!CanStartNextWave)
        {
            Debug.Log("[Wave] 当前不能开始下一波", this);
            return;
        }

        waveInProgress = true;
        currentWaveNumber++;
        StartCoroutine(RunWave(currentWaveNumber));
    }

    private IEnumerator RunWave(int waveNumber)
    {
        WaveSettings wave = waves[waveNumber - 1];
        int total = TotalWaveCount;

        Debug.Log("[Wave] 第 " + waveNumber + "/" + total + " 波开始");
        WaveBegan?.Invoke(waveNumber, total);

        if (wave.DelayBeforeWave > 0f)
            yield return new WaitForSeconds(wave.DelayBeforeWave);

        if (IsGameOver())
        {
            EndCurrentWave();
            yield break;
        }

        for (int groupIndex = 0; groupIndex < wave.Groups.Length; groupIndex++)
        {
            EnemyGroup group = wave.Groups[groupIndex];
            if (group == null || group.EnemyPrefab == null)
                continue;

            for (int spawned = 0; spawned < group.Count; spawned++)
            {
                if (IsGameOver())
                {
                    EndCurrentWave();
                    yield break;
                }

                SpawnEnemy(group.EnemyPrefab);

                if (spawned < group.Count - 1)
                    yield return new WaitForSeconds(group.SpawnInterval);
            }
        }

        // 等待本波敌人全部消失（被消灭或到达核心）
        while (enemiesAlive > 0)
        {
            if (IsGameOver())
            {
                EndCurrentWave();
                yield break;
            }

            yield return null;
        }

        // 关键：必须在发送 WaveCleared 之前复位“进行中”，
        // 否则 UI 收到事件时 CanStartNextWave 仍为 false。
        EndCurrentWave();

        // 最后一波的最后一只敌人若正好触发 Game Over，
        // 此时 enemiesAlive 已为 0，仍需检查并停止，不广播清除。
        if (IsGameOver())
            yield break;

        Debug.Log("[Wave] 第 " + waveNumber + " 波清除");
        WaveCleared?.Invoke(waveNumber, total);

        if (waveNumber >= total)
        {
            AllWavesCleared = true;
            if (GameManager.Instance != null)
                GameManager.Instance.WinGame();
        }
    }

    private void EndCurrentWave()
    {
        waveInProgress = false;
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
