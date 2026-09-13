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

    [Header("HUD Sprite Elements")]
    public Image goldIcon;
    public Image goldColon;
    public HudSpriteNumber goldNumber;
    public Image lifeIcon;
    public Image lifeColon;
    public HudSpriteNumber lifeNumber;
    public Image waveIcon;
    public Image waveSlash;
    public HudSpriteNumber waveCurrentNumber;
    public HudSpriteNumber waveTotalNumber;

    [Header("Tower Buttons")]
    public Button[] towerButtons;
    public Button demolishButton;
    public Text demolishButtonText;

    [Header("Wave")]
    public Button startButton;
    public Text startButtonText;
    public CanvasGroup startButtonGroup;
    public RectTransform waveProgressFill;
    public float waveProgressFullWidth = 420f;
    [SerializeField, Min(0.05f)] private float waveButtonFadeDuration = 0.2f;

    private float waveButtonTargetAlpha;

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
        AudioManager.PlayBgm("bgm_battle");

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
        UpdateWaveButtonFade();
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
        AddClickSound(startButton);

        if (towerButtons != null)
        {
            for (int i = 0; i < towerButtons.Length; i++)
            {
                int index = i;
                towerButtons[i].onClick.AddListener(() => buildManager.SelectTower(index));
                AddClickSound(towerButtons[i]);
            }
        }

        if (winRestartButton != null)
        {
            winRestartButton.onClick.AddListener(RestartGame);
            AddClickSound(winRestartButton);
        }
        if (loseRestartButton != null)
        {
            loseRestartButton.onClick.AddListener(RestartGame);
            AddClickSound(loseRestartButton);
        }
        if (winMenuButton != null)
        {
            winMenuButton.onClick.AddListener(LoadMainMenu);
            AddClickSound(winMenuButton);
        }
        if (loseMenuButton != null)
        {
            loseMenuButton.onClick.AddListener(LoadMainMenu);
            AddClickSound(loseMenuButton);
        }
        if (demolishButton != null)
        {
            demolishButton.onClick.AddListener(buildManager.ToggleDemolishMode);
            AddClickSound(demolishButton);
        }
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(RequestTowerUpgrade);
            AddClickSound(upgradeButton);
        }
    }

    private static void AddClickSound(Button button)
    {
        if (button != null)
            button.onClick.AddListener(() => AudioManager.PlaySfx("click", 0.7f));
    }

    private void CacheTowerLabels()
    {
        if (towerButtons == null)
        {
            towerButtonLabels = new string[0];
            return;
        }

        towerButtonLabels = new string[towerButtons.Length];
        for (int i = 0; i < towerButtons.Length; i++)
        {
            Text label = towerButtons[i] != null
                ? towerButtons[i].GetComponentInChildren<Text>()
                : null;
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

        if (goldNumber != null)
            goldNumber.SetNumber(gold);

        if (upgradePanel != null && upgradePanel.activeSelf && buildManager != null)
            RefreshUpgradePanel(buildManager.SelectedUpgradeTower);
    }

    private void OnLivesChanged(int lives)
    {
        if (livesText != null)
            livesText.text = "Lives: " + lives;

        if (lifeNumber != null)
            lifeNumber.SetNumber(lives);
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
            float remaining = waveManager.IntermissionRemaining;
            float duration = waveManager.IntermissionTotal;

            ShowWaveButton(true);
            SetWaveProgress(remaining / duration);

            if (waveText != null)
                waveText.text = "Wave: " + nextWave + " / " + total + "  (ready)";

            SetWaveNumbers(nextWave, total);
            return;
        }

        ShowWaveButton(false);
        SetWaveProgress(0f);

        if (waveManager.IsSpawningWave)
        {
            if (waveText != null)
                waveText.text = "Wave: " + waveManager.CurrentWaveNumber + " / " + total;

            SetWaveNumbers(waveManager.CurrentWaveNumber, total);
        }
        else if (waveManager.HasNextWave)
        {
            // 游戏刚开始或波次之间的极短过渡
            if (waveText != null)
                waveText.text = "Wave: " + waveManager.NextWaveNumber + " / " + total;

            SetWaveNumbers(waveManager.NextWaveNumber, total);
        }
        else if (waveManager.AllWavesSpawned)
        {
            SetWaveNumbers(total, total);
        }
    }

    private void ShowWaveButton(bool visible)
    {
        waveButtonTargetAlpha = visible ? 1f : 0f;

        if (!visible)
        {
            // 淡出时立即停止接收点击，避免还能点到
            if (startButtonGroup != null)
            {
                startButtonGroup.blocksRaycasts = false;
                startButtonGroup.interactable = false;
            }

            if (startButton != null)
                startButton.interactable = false;
        }
    }

    private void UpdateWaveButtonFade()
    {
        if (startButtonGroup == null)
            return;

        float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, waveButtonFadeDuration);
        startButtonGroup.alpha = Mathf.MoveTowards(
            startButtonGroup.alpha,
            waveButtonTargetAlpha,
            step);

        bool clickable = waveButtonTargetAlpha > 0.5f;
        startButtonGroup.blocksRaycasts = clickable;
        startButtonGroup.interactable = clickable;

        if (startButton != null)
            startButton.interactable = clickable;
    }

    private void SetWaveProgress(float ratio)
    {
        if (waveProgressFill == null)
            return;

        ratio = Mathf.Clamp01(ratio);
        Vector2 size = waveProgressFill.sizeDelta;
        size.x = waveProgressFullWidth * ratio;
        waveProgressFill.sizeDelta = size;
    }

    private void SetWaveNumbers(int current, int total)
    {
        if (waveCurrentNumber != null)
            waveCurrentNumber.SetNumber(current);
        if (waveTotalNumber != null)
            waveTotalNumber.SetNumber(total);
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
        {
            buildManager.RefreshSelectedTowerRange();
            RefreshUpgradePanel(tower);
        }
    }

    private void OnSelectionChanged(int index)
    {
        if (towerButtons != null)
        {
            for (int i = 0; i < towerButtons.Length; i++)
            {
                if (towerButtons[i] == null)
                    continue;

                Image image = towerButtons[i].GetComponent<Image>();
                if (image != null)
                    image.color = i == index ? SelectedButtonColor : NormalButtonColor;
            }
        }

        if (hintText == null)
            return;

        if (index < 0 || towerButtonLabels == null || index >= towerButtonLabels.Length)
            hintText.text = "Click an empty tile to build, or click a tower to upgrade / remove";
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
                : "Click an empty tile to build, or click a tower to upgrade / remove";
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
