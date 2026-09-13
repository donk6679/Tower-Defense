using System;
using UnityEngine;

/// <summary>
/// 核心管理器：负责金币、生命、游戏结束/胜利状态。
/// UI 通过 GameEnded / GoldChanged / LivesChanged 事件更新。
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField, Min(1)] private int startingLives = 20;
    [SerializeField, Min(0)] private int startingGold = 200;

    public int Lives { get; private set; }
    public int Gold { get; private set; }
    public bool HasWon { get; private set; }

    /// <summary>true 表示游戏已结束（无论胜利还是失败）。</summary>
    public bool IsGameOver { get; private set; }

    public event Action<int> LivesChanged;
    public event Action<int> GoldChanged;
    public event Action<bool> GameEnded; // 参数：true=胜利, false=失败

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Lives = startingLives;
        Gold = startingGold;
    }

    public void SetStartingLives(int value)
    {
        startingLives = Mathf.Max(1, value);
    }

    public void SetStartingGold(int value)
    {
        startingGold = Mathf.Max(0, value);
    }

    public void LoseLife()
    {
        if (IsGameOver)
            return;

        Lives = Mathf.Max(0, Lives - 1);
        LivesChanged?.Invoke(Lives);

        if (Lives <= 0)
            EndGame(false);
    }

    public void WinGame()
    {
        if (IsGameOver)
            return;

        HasWon = true;
        EndGame(true);
    }

    public void AddGold(int amount)
    {
        if (IsGameOver || amount <= 0)
            return;

        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (IsGameOver || amount <= 0 || Gold < amount)
            return false;

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    private void EndGame(bool won)
    {
        IsGameOver = true;
        HasWon = won;

        Debug.Log(won ? "[Game] 胜利！所有波次已被守住" : "[Game] 游戏结束：核心生命归零");
        AudioManager.PlaySfx(won ? "victory" : "defeat");
        GameEnded?.Invoke(won);
    }
}
