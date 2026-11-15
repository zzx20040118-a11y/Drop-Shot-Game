using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using TMPro;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Supabase 配置")]
    public string supabaseUrl;
    public string publishableKey;

    [Header("排行榜 UI")]
    public TMP_Text leaderboardText;

    [Header("当前难度")]
    public string currentMode = "Easy";

    private string TableUrl =>
        supabaseUrl.TrimEnd('/') + "/rest/v1/leaderboard";

    // =========================================
    // 提交成绩
    // =========================================

    public void SubmitScore(string playerName, int score, string mode)
    {
        StartCoroutine(SubmitScoreCoroutine(playerName, score, mode));
    }

    private IEnumerator SubmitScoreCoroutine(
        string playerName,
        int score,
        string mode)
    {
        string json =
            "{\"name\":\"" + EscapeJson(playerName) +
            "\",\"score\":" + score +
            ",\"mode\":\"" + EscapeJson(mode) + "\"}";

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
               new UnityWebRequest(TableUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("apikey", publishableKey);
            request.SetRequestHeader(
                "Authorization", "Bearer " + publishableKey);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "return=minimal");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("排行榜成绩提交成功！");
                Debug.Log("玩家：" + playerName +
                          " | 分数：" + score +
                          " | 模式：" + mode);
            }
            else
            {
                Debug.LogError("成绩提交失败：" + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }

    // =========================================
    // 读取排行榜
    // =========================================

    public void LoadLeaderboard(string mode)
    {
        currentMode = mode;
        StartCoroutine(LoadLeaderboardCoroutine(mode));
    }

    private IEnumerator LoadLeaderboardCoroutine(string mode)
    {
        string url = TableUrl +
            "?select=name,score,mode" +
            "&mode=eq." + UnityWebRequest.EscapeURL(mode) +
            "&order=score.desc" +
            "&limit=100";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", publishableKey);
            request.SetRequestHeader(
                "Authorization", "Bearer " + publishableKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = "{\"items\":" +
                    request.downloadHandler.text + "}";

                LeaderboardResponse response =
                    JsonUtility.FromJson<LeaderboardResponse>(json);

                ShowLeaderboard(response.items);

                Debug.Log("排行榜读取成功！");
            }
            else
            {
                Debug.LogError("排行榜读取失败：" + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }

    // =========================================
    // 显示排行榜
    // =========================================

    private void ShowLeaderboard(LeaderboardEntry[] entries)
    {
        if (leaderboardText == null)
        {
            Debug.LogWarning("没有绑定排行榜 Text！");
            return;
        }

        string result = "RANK       PLAYER       SCORE\n\n";

        if (entries == null || entries.Length == 0)
        {
            result += "No Records";
        }
        else
        {
            for (int i = 0; i < entries.Length; i++)
            {
                result +=
                    (i + 1).ToString("D2") +
                    "       " +
                    entries[i].name +
                    "       " +
                    entries[i].score +
                    "\n";
            }
        }

        leaderboardText.text = result;
    }

    // =========================================
    // Easy / Hard 按钮
    // =========================================

    public void OnEasyButtonClick()
    {
        LoadLeaderboard("Easy");
    }

    public void OnHardButtonClick()
    {
        LoadLeaderboard("Hard");
    }

    // =========================================
    // 测试功能
    // =========================================

    [ContextMenu("测试提交成绩")]
    private void TestSubmitScore()
    {
        SubmitScore("TestPlayer", 1000, "Easy");
    }

    [ContextMenu("测试读取 Easy 排行榜")]
    private void TestLoadEasy()
    {
        LoadLeaderboard("Easy");
    }

    [ContextMenu("测试读取 Hard 排行榜")]
    private void TestLoadHard()
    {
        LoadLeaderboard("Hard");
    }

    // =========================================
    // JSON 转义
    // =========================================

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    // =========================================
    // 排行榜数据
    // =========================================

    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public string mode;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] items;
    }
}
