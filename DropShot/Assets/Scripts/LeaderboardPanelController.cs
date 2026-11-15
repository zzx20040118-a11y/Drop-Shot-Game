using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanelController : MonoBehaviour
{
    [Header("排行榜面板")]
    public GameObject leaderboardPanel;

    [Header("排行榜管理器")]
    public LeaderboardManager leaderboardManager;

    [Header("排行榜底图")]
    public Image backgroundImage;
    public Sprite easyBackground;
    public Sprite hardBackground;

    private void Start()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    // 打开排行榜，默认显示 Easy
    public void OpenLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        ShowEasyLeaderboard();
    }

    // Easy 排行榜
    public void ShowEasyLeaderboard()
    {
        if (backgroundImage != null && easyBackground != null)
        {
            backgroundImage.sprite = easyBackground;
        }

        leaderboardManager.OnEasyButtonClick();
    }

    // Hard 排行榜
    public void ShowHardLeaderboard()
    {
        if (backgroundImage != null && hardBackground != null)
        {
            backgroundImage.sprite = hardBackground;
        }

        leaderboardManager.OnHardButtonClick();
    }

    // 刷新排行榜
    public void RefreshLeaderboard()
    {
        leaderboardManager.LoadLeaderboard(
            leaderboardManager.currentMode
        );
    }

    // 关闭排行榜
    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }
}
