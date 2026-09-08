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
    public Button demolishButton;
    public Text demolishButtonText;

    [Header("Wave")]
    public Button startButton;
    public Text startButtonText;

    [Header("Upgrade Panel")]
    public GameObject upgradePanel;
    public Text upgradeInfoText;
    public Button upgradeButton;
    public Text upgradeButtonText;

    [Header("Results")]
    public GameObject winPanel;
    public Button winRestartButton;
    public Button winMenuButton;
    public GameObject losePanel;
    public Button loseRestartButton;
    public Button loseMenuButton;

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

    private void Update()
    {
        if (gameManager != null && gameManager.IsGameOver)
            return;

        RefreshWaveHud();
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
        buildManager.SelectionChanged += OnSelectionChanged;
        buildManager.DemolishModeChanged += OnDemolishModeChanged;
        buildManager.UpgradeSelectionChanged += OnUpgradeSelectionChanged;
    }

    private void Unsubscribe()
    {
        if (gameManager == null || waveManager == null || buildManager == null)
            return;

        gameManager.GoldChanged -= OnGoldChanged;
        gameManager.LivesChanged -= OnLivesChanged;
        gameManager.GameEnded -= OnGameEnded;
        buildManager.SelectionChanged -= OnSelectionChanged;
        buildManager.DemolishModeChanged -= OnDemolishModeChanged;
        buildManager.UpgradeSelectionChanged -= OnUpgradeSelectionChanged;
    }

    private void SetupButtonListeners()
    {
        startButton.onClick.AddListener(RequestEarlyNextWave);

        for (int i = 0; i < towerButtons.Length; i++)
        {
            int index = i;
            towerButtons[i].onClick.AddListener(() => buildManager.SelectTower(index));
        }

        if (winRestartButton != null)
            winRestartButton.onClick.AddListener(RestartGame);
        if (loseRestartButton != null)
            loseRestartButton.onClick.AddListener(RestartGame);
        if (winMenuButton != null)
            winMenuButton.onClick.AddListener(LoadMainMenu);
        if (loseMenuButton != null)
            loseMenuButton.onClick.AddListener(LoadMainMenu);
        if (demolishButton != null)
            demolishButton.onClick.AddListener(buildManager.ToggleDemolishMode);
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(RequestTowerUpgrade);
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
        RefreshWaveHud();
        OnSelectionChanged(-1);
        OnDemolishModeChanged(buildManager.IsDemolishMode);
    }

    private void OnGoldChanged(int gold)
    {
        if (goldText != null)
            goldText.text = "Gold: " + gold;

        if (upgradePanel != null && upgradePanel.activeSelf && buildManager != null)
            RefreshUpgradePanel(buildManager.SelectedUpgradeTower);
    }

    private void OnLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = "Lives: " + lives;
    }

    private void RefreshWaveHud()
    {
        if (waveManager == null)
            return;

        int total = waveManager.TotalWaveCount;
        if (total == 0)
            return;

        if (waveManager.IsWaitingForNextWave)
        {
            int nextWave = waveManager.NextWaveNumber;
            int bonus = waveManager.EstimatedEarlyBonus;
            float remaining = waveManager.IntermissionRemaining;

            if (startButton != null)
                startButton.interactable = true;

            if (startButtonText != null)
                startButtonText.text = "Start Wave " + nextWave + " Now  +" + bonus +
                                       "g  | auto " + remaining.ToString("0.0") + "s";

            if (waveText != null)
                waveText.text = "Wave: " + nextWave + " / " + total + "  (ready)";
        }
        else if (waveManager.IsSpawningWave)
        {
            if (startButton != null)
                startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "Wave " + waveManager.CurrentWaveNumber + " spawning...";

            if (waveText != null)
                waveText.text = "Wave: " + waveManager.CurrentWaveNumber + " / " + total;
        }
        else if (waveManager.HasNextWave)
        {
            // 游戏刚开始或波次之间的极短过渡
            if (startButton != null)
                startButton.interactable = false;

            if (startButtonText != null)
                startButtonText.text = "Wave " + waveManager.NextWaveNumber + " starting soon...";

            if (waveText != null)
                waveText.text = "Wave: " + waveManager.NextWaveNumber + " / " + total;
        }
        else if (waveManager.AllWavesSpawned)
        {
            if (startButton != null)
                startButton.interactable = false;

            if (startButtonText != null)
            {
                startButtonText.text = waveManager.EnemiesAlive > 0
                    ? "Clearing final wave..."
                    : "Victory!";
            }
        }
    }

    private void RequestEarlyNextWave()
    {
        if (waveManager == null || !waveManager.RequestImmediateNextWave())
            Debug.Log("[UI] 当前还不能提前开波");
    }

    private void OnUpgradeSelectionChanged(TowerBase tower)
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(tower != null);

        if (tower != null)
            RefreshUpgradePanel(tower);
    }

    private void RefreshUpgradePanel(TowerBase tower)
    {
        if (tower == null)
            return;

        if (upgradeInfoText != null)
            upgradeInfoText.text = tower.GetUpgradeDescription();

        if (upgradeButtonText != null)
            upgradeButtonText.text = tower.CanUpgrade
                ? "Upgrade  " + tower.NextUpgradeCost + "g"
                : "MAX LEVEL";

        if (upgradeButton != null)
        {
            upgradeButton.interactable = tower.CanUpgrade &&
                                         gameManager != null &&
                                         gameManager.Gold >= tower.NextUpgradeCost;
        }
    }

    private void RequestTowerUpgrade()
    {
        TowerBase tower = buildManager != null ? buildManager.SelectedUpgradeTower : null;
        if (tower == null)
            return;

        if (tower.TryUpgrade())
            RefreshUpgradePanel(tower);
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

    private void OnDemolishModeChanged(bool isActive)
    {
        if (demolishButton != null && demolishButtonText != null)
        {
            Image image = demolishButton.GetComponent<Image>();
            if (image != null)
                image.color = isActive ? SelectedButtonColor : NormalButtonColor;

            demolishButtonText.text = isActive
                ? "Remove Mode: ON"
                : "Remove Mode (X)";
        }

        if (hintText != null)
        {
            hintText.text = isActive
                ? "REMOVE MODE: click an occupied tile to demolish its tower"
                : "Select a tower button, then click a green tile";
        }
    }

    private void OnGameEnded(bool won)
    {
        if (startButton != null)
            startButton.interactable = false;
        if (upgradePanel != null)
            upgradePanel.SetActive(false);

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

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
