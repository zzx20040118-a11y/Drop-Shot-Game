using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanelController : MonoBehaviour
{
    [Header("提交成绩页面")]
    public GameObject submitScorePanel;

    [Header("排行榜页面")]
    public GameObject leaderboardPanel;

    [Header("排行榜管理器")]
    public LeaderboardManager leaderboardManager;

    [Header("排行榜底图")]
    public Image backgroundImage;
    public Sprite easyBackground;
    public Sprite hardBackground;

    private void Start()
    {
        if (submitScorePanel != null)
            submitScorePanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    // 结算页点击 LEADERBOARD
    public void OpenSubmitScorePanel()
    {
        if (submitScorePanel != null)
            submitScorePanel.SetActive(true);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    // 上传成功后自动进入排行榜
    public void OnScoreSubmitted()
    {
        OpenLeaderboard();
    }

    // 打开排行榜
    public void OpenLeaderboard()
    {
        if (submitScorePanel != null)
            submitScorePanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);

        ShowEasyLeaderboard();
    }

    // Easy
    public void ShowEasyLeaderboard()
    {
        if (backgroundImage != null && easyBackground != null)
            backgroundImage.sprite = easyBackground;

        if (leaderboardManager != null)
            leaderboardManager.OnEasyButtonClick();
    }

    // Hard
    public void ShowHardLeaderboard()
    {
        if (backgroundImage != null && hardBackground != null)
            backgroundImage.sprite = hardBackground;

        if (leaderboardManager != null)
            leaderboardManager.OnHardButtonClick();
    }

    // 刷新
    public void RefreshLeaderboard()
    {
        if (leaderboardManager != null)
            leaderboardManager.LoadLeaderboard(
                leaderboardManager.currentMode
            );
    }

    // 关闭排行榜，返回结算页面
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }
}
