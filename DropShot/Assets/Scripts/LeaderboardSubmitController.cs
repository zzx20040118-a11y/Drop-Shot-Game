using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DropShot.UI;

public class LeaderboardSubmitController : MonoBehaviour
{
    [Header("排行榜管理器")]
    public LeaderboardManager leaderboardManager;

    [Header("成绩 UI")]
    public TMP_InputField playerNameInput;
    public TMP_Text scoreText;
    public Button submitButton;

    [Header("难度")]
    public string easySceneName = "Game_B_Easy";
    public string hardSceneName = "Game_B_Hard";

    private int finalScore;
    private string currentMode;
    private bool submitted = false;

    // 结算页面调用：初始化成绩
    public void InitializeScore(int score)
    {
        finalScore = score;

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        currentMode = GetCurrentMode();

        submitted = false;

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        Debug.Log($"排行榜成绩初始化：{finalScore} / {currentMode}");
    }

    // 根据实际游戏场景判断难度
    private string GetCurrentMode()
    {
        string sceneName = ScoreBoard.GameSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = SceneManager.GetActiveScene().name;
        }

        if (sceneName.Contains("Hard"))
        {
            return "Hard";
        }

        return "Easy";
    }

    // 点击 SUBMIT SCORE 按钮调用
    public void SubmitScore()
    {
        if (submitted)
        {
            return;
        }

        if (playerNameInput == null)
        {
            Debug.LogError("PlayerNameInput 未绑定！");
            return;
        }

        string playerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("请先输入玩家昵称！");
            return;
        }

        if (leaderboardManager == null)
        {
            Debug.LogError("LeaderboardManager 未绑定！");
            return;
        }

        submitted = true;

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        leaderboardManager.SubmitScore(
            playerName,
            finalScore,
            currentMode
        );

        Debug.Log(
            $"正在提交成绩：{playerName} / {finalScore} / {currentMode}"
        );
    }
}
