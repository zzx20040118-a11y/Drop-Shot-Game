using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DropShot.UI;
using System;

public class LeaderboardSubmitController : MonoBehaviour
{
    [Header("Leaderboard Manager / 排行榜管理器")]
    public LeaderboardManager leaderboardManager;

    [Header("Leaderboard Panel Controller / 排行榜页面控制器")]
    public LeaderboardPanelController panelController;

    [Header("Score UI / 成绩 UI")]
    public TMP_InputField playerNameInput;
    public TMP_Text scoreText;
    public Button submitButton;

    private int finalScore;
    private string currentMode;
    private bool submitted = false;

    public void InitializeScore(int score)
    {
        finalScore = score;

        if (scoreText != null)
            scoreText.text = score.ToString();

        currentMode = GetCurrentMode();
        submitted = false;

        if (submitButton != null)
            submitButton.interactable = true;
    }

    private string GetCurrentMode()
    {
        string sceneName = ScoreBoard.GameSceneName;

        if (string.IsNullOrEmpty(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        return sceneName.Contains("Hard") ? "Hard" : "Easy";
    }

    public void SubmitScore()
    {
        if (submitted)
            return;

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

        if (panelController == null)
        {
            Debug.LogError("LeaderboardPanelController 未绑定！");
            return;
        }

        submitted = true;

        if (submitButton != null)
            submitButton.interactable = false;

        leaderboardManager.SubmitScore(
            playerName,
            finalScore,
            currentMode
        );

        Debug.Log($"提交成绩：{playerName} / {finalScore} / {currentMode}");
    }

    // 由 LeaderboardManager 在上传成功后调用
    public void OnUploadSuccess()
    {
        Debug.Log("成绩上传成功！");
        panelController.OnScoreSubmitted();
    }

    // 上传失败后恢复按钮，允许重试
    public void OnUploadFailed()
    {
        submitted = false;

        if (submitButton != null)
            submitButton.interactable = true;

        Debug.LogWarning("成绩上传失败，可以重新提交！");
    }
}
