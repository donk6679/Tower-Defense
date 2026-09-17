using System.Collections;
using UnityEngine;

/// <summary>
/// 波次管理器（自动衔接 + 提前开波奖励）：
/// 1. 第 1 波在短暂延迟后自动开始；
/// 2. 每波最后一个敌人生成后进入“准备下一波”倒计时；
/// 3. 倒计时结束自动开始下一波，不要求上一波敌人被清空；
/// 4. 倒计时期间玩家可点击按钮立即开波，
///    并按照剩余倒计时秒数获得金币奖励。
/// </summary>
public sealed class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathManager pathManager;

    [Header("Wave Config")]
    [SerializeField] private WaveSettings[] waves = new WaveSettings[0];

    [Header("Enemy Health Growth")]
    [Tooltip("每往后一波，敌人基础血量额外增加的倍率。\n最终倍率 = 该波 Health Multiplier + 本值 × (波数 - 1)。")]
    [SerializeField, Min(0f)] private float healthGrowthPerWave = 0.15f;

    [Header("Auto Start Timing")]
    [SerializeField, Min(0f)] private float firstWaveDelay = 1.5f;
    [SerializeField, Min(0.5f)] private float nextWaveAutoDelay = 10f;
    [SerializeField, Min(0f)] private float goldPerRemainingSecond = 2f;

    [Header("Runtime Status (read only)")]
    [SerializeField, Min(0)] private int enemiesAlive;
    [SerializeField, Min(0)] private int currentWaveNumber;

    private bool spawningWave;
    private bool waitingForNextWave;
    private bool allWavesSpawned;
    private bool skipIntermissionRequested;
    private int nextWaveNumber = 1;
    private float intermissionRemaining;
    private float intermissionTotal;

    public int EnemiesAlive => enemiesAlive;
    public int CurrentWaveNumber => currentWaveNumber;
    public int TotalWaveCount => waves == null ? 0 : waves.Length;
    public float HealthGrowthPerWave => healthGrowthPerWave;
    public int NextWaveNumber => nextWaveNumber;
    public float IntermissionRemaining => Mathf.Max(0f, intermissionRemaining);
    public float IntermissionTotal => Mathf.Max(0.01f, intermissionTotal);

    public bool IsSpawningWave => spawningWave;
    public bool IsWaitingForNextWave => waitingForNextWave;
    public bool HasNextWave => !allWavesSpawned && nextWaveNumber <= TotalWaveCount;
    public bool AllWavesSpawned => allWavesSpawned;
    public bool AllWavesCleared { get; private set; }

    public bool CanRequestEarlyNextWave =>
        waitingForNextWave &&
        !skipIntermissionRequested &&
        !IsGameOver() &&
        HasNextWave;

    public int EstimatedEarlyBonus =>
        CanRequestEarlyNextWave
            ? Mathf.RoundToInt(intermissionRemaining * goldPerRemainingSecond)
            : 0;

    public void Setup(PathManager path, WaveSettings[] waveConfigs)
    {
        pathManager = path;
        waves = waveConfigs == null ? new WaveSettings[0] : waveConfigs;
        nextWaveNumber = 1;
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

        StartCoroutine(RunWaveFlow());
    }

    /// <summary>
    /// 倒计时期间点击“立即开始下一波”：
    /// 下一波马上到来，并按剩余秒数发金币。
    /// </summary>
    public bool RequestImmediateNextWave()
    {
        if (!CanRequestEarlyNextWave)
            return false;

        float remaining = IntermissionRemaining;
        int bonus = Mathf.RoundToInt(remaining * goldPerRemainingSecond);

        if (bonus > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(bonus);
            Debug.Log("[Wave] 提前开启下一波，剩余 " +
                      remaining.ToString("0.0") + " 秒，奖励 " + bonus + " 金币");
        }

        skipIntermissionRequested = true;
        AudioManager.PlaySfx("early_wave");
        return true;
    }

    private IEnumerator RunWaveFlow()
    {
        if (firstWaveDelay > 0f)
            yield return new WaitForSeconds(firstWaveDelay);

        if (IsGameOver())
            yield break;

        while (nextWaveNumber <= TotalWaveCount && !IsGameOver())
        {
            int waveNumber = nextWaveNumber;

            yield return StartCoroutine(SpawnWave(waveNumber));

            if (IsGameOver())
                break;

            // 最后一波不需要“准备下一波”倒计时，等待场上敌人全部消失后胜利
            if (waveNumber >= TotalWaveCount)
            {
                allWavesSpawned = true;
                break;
            }

            yield return StartCoroutine(RunIntermission(waveNumber));

            if (IsGameOver())
                break;

            nextWaveNumber++;
        }

        if (IsGameOver())
            yield break;

        allWavesSpawned = true;

        // 所有波都已发出：等待场上剩余敌人消失（或被漏到核心导致失败）
        while (enemiesAlive > 0 && !IsGameOver())
            yield return null;

        if (IsGameOver())
            yield break;

        AllWavesCleared = true;
        Debug.Log("[Wave] 全部波次结束，胜利！");

        if (GameManager.Instance != null)
            GameManager.Instance.WinGame();
    }

    private IEnumerator SpawnWave(int waveNumber)
    {
        spawningWave = true;
        currentWaveNumber = waveNumber;

        WaveSettings wave = waves[waveNumber - 1];
        int total = TotalWaveCount;

        float waveHealthMultiplier =
            wave.HealthMultiplier + healthGrowthPerWave * (waveNumber - 1);

        Debug.Log("[Wave] 第 " + waveNumber + "/" + total +
                  " 波开始生成，血量倍率 ×" + waveHealthMultiplier.ToString("0.00"));
        AudioManager.PlaySfx("wave_start");

        if (IsGameOver())
        {
            StopSpawning();
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
                    StopSpawning();
                    yield break;
                }

                SpawnEnemy(group.EnemyPrefab, waveHealthMultiplier);

                if (spawned < group.Count - 1)
                    yield return new WaitForSeconds(group.SpawnInterval);
            }
        }

        // 注意：最后一个敌人生成后，本协程就结束并进入倒计时，
        // 不等待敌人被消灭——上一波残余敌人可以与下一波同时在场。
        StopSpawning();
    }

    private IEnumerator RunIntermission(int completedWaveNumber)
    {
        waitingForNextWave = true;
        intermissionRemaining = Mathf.Max(0.5f, nextWaveAutoDelay);
        intermissionTotal = intermissionRemaining;

        Debug.Log("[Wave] 第 " + completedWaveNumber + " 波已生成完毕，" +
                  intermissionRemaining.ToString("0.0") +
                  " 秒后自动开始下一波（可提前开波拿金币）");

        while (intermissionRemaining > 0f && !IsGameOver())
        {
            if (skipIntermissionRequested)
            {
                skipIntermissionRequested = false;
                break;
            }

            intermissionRemaining -= Time.deltaTime;
            yield return null;
        }

        skipIntermissionRequested = false;
        waitingForNextWave = false;
    }

    private void StopSpawning()
    {
        spawningWave = false;
    }

    private void SpawnEnemy(Enemy enemyPrefab, float healthMultiplier)
    {
        int health = Mathf.Max(
            1,
            Mathf.RoundToInt(enemyPrefab.MaxHealth * healthMultiplier));

        Enemy enemy = Instantiate(enemyPrefab, pathManager.SpawnPosition, Quaternion.identity);
        enemiesAlive++;

        enemy.Initialize(pathManager, health);
        enemy.Died += HandleEnemyDied;
        enemy.ReachedBase += HandleEnemyReachedBase;
    }

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        Debug.Log("[Wave] " + enemy.name + " 到达核心（场上剩余 " + enemiesAlive + "）");

        if (GameManager.Instance != null)
            GameManager.Instance.LoseLife();

        AudioManager.PlaySfx("life_lost", 0.8f);
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
