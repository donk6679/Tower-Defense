using System;
using UnityEngine;

/// <summary>
/// 核心管理器：负责核心生命、金币与游戏状态。
/// 敌人到达核心时扣生命；敌人被消灭时加金币。
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core")]
    [SerializeField, Min(1)] private int startingLives = 20;
    [SerializeField, Min(0)] private int startingGold = 200;

    public int Lives { get; private set; }
    public int Gold { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action<int> LivesChanged;
    public event Action<int> GoldChanged;
    public event Action GameOverOccurred;

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
            TriggerGameOver();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
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

    private void TriggerGameOver()
    {
        IsGameOver = true;
        Debug.Log("[Game] 游戏结束：核心生命归零");
        GameOverOccurred?.Invoke();
    }

    // 临时调试 HUD，第 5 天做正式 UI 后会删除
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 260f, 70f));
        GUILayout.Label("Gold: " + Gold + "   Lives: " + Lives);
        if (IsGameOver)
            GUILayout.Label("GAME OVER");
        GUILayout.EndArea();
    }
}
