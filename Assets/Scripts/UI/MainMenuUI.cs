using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单：进入玩法场景 / 退出游戏。
/// 由 TDMainMenuSetup 编辑器工具创建并绑定按钮。
/// </summary>
public sealed class MainMenuUI : MonoBehaviour
{
    private const string GameSceneName = "Main";

    private void Awake()
    {
        AudioManager.PlayBgm("bgm_menu");

        // 运行时绑定按钮：Unity 编辑器里用 AddListener 添加的
        // 普通监听不会随场景保存，因此在 Awake 统一查找并绑定。
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            switch (buttons[i].name)
            {
                case "StartButton":
                    buttons[i].onClick.AddListener(StartGame);
                    buttons[i].onClick.AddListener(() => AudioManager.PlaySfx("click", 0.7f));
                    break;
                case "ExitButton":
                    buttons[i].onClick.AddListener(QuitGame);
                    buttons[i].onClick.AddListener(() => AudioManager.PlaySfx("click", 0.7f));
                    break;
            }
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[Menu] 开始游戏，加载 " + GameSceneName);
        SceneManager.LoadScene(GameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[Menu] 退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
