using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全部正式 UI：金币/生命/波次 HUD、炮塔选择按钮、
/// 开始波次按钮、胜利与失败面板（含重新开始）。
/// 由 TDSetupUI 编辑器工具创建并绑定。
/// </summary>
public sealed class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public Text goldText;
    public Text livesText;
    public Text waveText;
    public Text hintText;

    [Header("Tower Buttons")]
    public Button[] towerButtons;

    [Header("Wave")]
    public Button startButton;
    public Text startButtonText;

    [Header("Results")]
    public GameObject winPanel;
    public Button winRestartButton;
    public GameObject losePanel;
    public Button loseRestartButton;

    private GameManager gameManager;
    private WaveManager waveManager;
    private BuildManager buildManager;

    private string[] towerButtonLabels;
    private static readonly Color NormalButtonColor = new Color(0.15f, 0.25f, 0.4f, 1f);
    private static readonly Color SelectedButtonColor = new Color(0.9f, 0.7f, 0.15f, 1f);

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        waveManager = FindObjectOfType<WaveManager>();
        buildManager = FindObjectOfType<BuildManager>();

        if (gameManager == null || waveManager == null || buildManager == null)
        {
            Debug.LogWarning("[UI] 缺少 GameManager / WaveManager / BuildManager，UI 无法工作", this);
            return;
        }

        Subscribe();
        SetupButtonListeners();
        CacheTowerLabels();
        RefreshAll();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        gameManager.GoldChanged += OnGoldChanged;
        gameManager.LivesChanged += OnLivesChanged;
        gameManager.GameEnded += OnGameEnded;
        waveManager.WaveBegan += OnWaveBegan;
        waveManager.WaveCleared += OnWaveCleared;
        buildManager.SelectionChanged += OnSelectionChanged;
    }

    private void Unsubscribe()
    {
        if (gameManager == null || waveManager == null || buildManager == null)
            return;

        gameManager.GoldChanged -= OnGoldChanged;
        gameManager.LivesChanged -= OnLivesChanged;
        gameManager.GameEnded -= OnGameEnded;
        waveManager.WaveBegan -= OnWaveBegan;
        waveManager.WaveCleared -= OnWaveCleared;
        buildManager.SelectionChanged -= OnSelectionChanged;
    }

    private void SetupButtonListeners()
    {
        startButton.onClick.AddListener(StartNextWave);

        for (int i = 0; i < towerButtons.Length; i++)
        {
            int index = i;
            towerButtons[i].onClick.AddListener(() => buildManager.SelectTower(index));
        }

        if (winRestartButton != null)
            winRestartButton.onClick.AddListener(RestartGame);
        if (loseRestartButton != null)
            loseRestartButton.onClick.AddListener(RestartGame);
    }

    private void CacheTowerLabels()
    {
        towerButtonLabels = new string[towerButtons.Length];
        for (int i = 0; i < towerButtons.Length; i++)
        {
            Text label = towerButtons[i].GetComponentInChildren<Text>();
            towerButtonLabels[i] = label != null ? label.text : "Tower " + i;
        }
    }

    private void RefreshAll()
    {
        OnGoldChanged(gameManager.Gold);
        OnLivesChanged(gameManager.Lives);
        UpdateWaveStartUI();
        OnSelectionChanged(-1);
    }

    private void OnGoldChanged(int gold)
    {
        if (goldText != null)
            goldText.text = "Gold: " + gold;
    }

    private void OnLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = "Lives: " + lives;
    }

    private void OnWaveBegan(int current, int total)
    {
        if (waveText != null)
            waveText.text = "Wave: " + current + " / " + total;

        if (startButtonText != null)
            startButtonText.text = "Wave " + current + " in progress...";

        if (startButton != null)
            startButton.interactable = false;
    }

    private void OnWaveCleared(int current, int total)
    {
        UpdateWaveStartUI();
    }

    private void UpdateWaveStartUI()
    {
        if (waveManager == null)
            return;

        int nextWave = waveManager.CurrentWaveNumber + 1;

        if (waveText != null)
            waveText.text = "Wave: " + Mathf.Min(nextWave, waveManager.TotalWaveCount) +
                            " / " + waveManager.TotalWaveCount;

        if (startButton != null)
            startButton.interactable = waveManager.CanStartNextWave;

        if (startButtonText != null)
            startButtonText.text = waveManager.CanStartNextWave
                ? "Start Wave " + nextWave
                : startButtonText.text;
    }

    private void StartNextWave()
    {
        waveManager.StartNextWave();
    }

    private void OnSelectionChanged(int index)
    {
        if (towerButtons == null || towerButtons.Length == 0)
            return;

        for (int i = 0; i < towerButtons.Length; i++)
        {
            if (towerButtons[i] == null)
                continue;

            Image image = towerButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = i == index ? SelectedButtonColor : NormalButtonColor;
        }

        if (hintText == null)
            return;

        if (index < 0 || towerButtonLabels == null || index >= towerButtonLabels.Length)
            hintText.text = "Select a tower button, then click a green tile";
        else
            hintText.text = "Selected: " + towerButtonLabels[index] +
                            "  |  Click a green tile to build (Esc to cancel)";
    }

    private void OnGameEnded(bool won)
    {
        if (startButton != null)
            startButton.interactable = false;

        if (winPanel != null)
            winPanel.SetActive(won);
        if (losePanel != null)
            losePanel.SetActive(!won);

        // 暂停游戏世界，让结算面板稳定显示
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
