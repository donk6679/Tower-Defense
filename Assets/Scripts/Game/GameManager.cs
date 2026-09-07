using System;
using UnityEngine;

/// <summary>
/// 核心管理器：负责核心生命与游戏状态。
/// 敌人到达核心时通过 WaveManager 调用 LoseLife()。
/// 后续金币、胜利/失败 UI 也会集中在这里。
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core")]
    [SerializeField, Min(1)] private int startingLives = 20;

    public int Lives { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action<int> LivesChanged;
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
    }

    public void SetStartingLives(int value)
    {
        startingLives = Mathf.Max(1, value);
    }

    public void LoseLife()
    {
        if (IsGameOver)
            return;

        Lives = Mathf.Max(0, Lives - 1);
        Debug.Log("[Game] 核心生命 -1，剩余 " + Lives);
        LivesChanged?.Invoke(Lives);

        if (Lives <= 0)
            TriggerGameOver();
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
        GUILayout.BeginArea(new Rect(10f, 10f, 240f, 80f));
        GUILayout.Label("Lives: " + Lives);
        if (IsGameOver)
            GUILayout.Label("GAME OVER");
        GUILayout.EndArea();
    }
}
