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
    public GameObject rankingRowPrefab;
    public Transform rankingRowsParent;

    [Header("当前难度")]
    public string currentMode = "Easy";

    [Header("成绩提交控制器")]
    public LeaderboardSubmitController submitController;

    private string TableUrl =>
        supabaseUrl.TrimEnd('/') + "/rest/v1/leaderboard";

    // =========================
    // 提交成绩
    // =========================

    public void SubmitScore(string playerName, int score, string mode)
    {
        StartCoroutine(
            SubmitScoreCoroutine(playerName, score, mode)
        );
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
                "Authorization",
                "Bearer " + publishableKey
            );

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "Prefer",
                "return=minimal"
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("排行榜成绩提交成功！");

                if (submitController != null)
                    submitController.OnUploadSuccess();
                else
                    Debug.LogWarning(
                        "LeaderboardManager 没有绑定 SubmitController！"
                    );
            }
            else
            {
                Debug.LogError("成绩提交失败：" + request.error);
                Debug.LogError(request.downloadHandler.text);

                if (submitController != null)
                    submitController.OnUploadFailed();
            }
        }
    }

    // =========================
    // 读取排行榜
    // =========================

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
                "Authorization",
                "Bearer " + publishableKey
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json =
                    "{\"items\":" + request.downloadHandler.text + "}";

                LeaderboardResponse response =
                    JsonUtility.FromJson<LeaderboardResponse>(json);

                ShowLeaderboard(response.items);

                Debug.Log(mode + " 排行榜读取成功！");
            }
            else
            {
                Debug.LogError("排行榜读取失败：" + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }

    // =========================
    // 自动生成排行榜 UI
    // =========================

    private void ShowLeaderboard(LeaderboardEntry[] entries)
    {
        if (rankingRowPrefab == null)
        {
            Debug.LogError("RankingRow Prefab 没有绑定！");
            return;
        }

        if (rankingRowsParent == null)
        {
            Debug.LogError("RankingRows 没有绑定！");
            return;
        }

        // 清除旧的排行榜行
        foreach (Transform child in rankingRowsParent)
        {
            Destroy(child.gameObject);
        }

        // 没有成绩
        if (entries == null || entries.Length == 0)
        {
            Debug.Log("排行榜暂无成绩");
            return;
        }

        // 自动生成成绩行
        for (int i = 0; i < entries.Length; i++)
        {
            GameObject row = Instantiate(
                rankingRowPrefab,
                rankingRowsParent
            );

            TMP_Text[] texts =
                row.GetComponentsInChildren<TMP_Text>(true);

            if (texts.Length < 3)
            {
                Debug.LogError(
                    "RankingRow Prefab 至少需要三个 TMP Text！"
                );

                Destroy(row);
                continue;
            }

            // 第一个：排名
            texts[0].text = (i + 1).ToString("D2");

            // 第二个：昵称
            texts[1].text = entries[i].name;

            // 第三个：分数
            texts[2].text = entries[i].score.ToString();
        }
    }

    // =========================
    // Easy / Hard 切换
    // =========================

    public void OnEasyButtonClick()
    {
        LoadLeaderboard("Easy");
    }

    public void OnHardButtonClick()
    {
        LoadLeaderboard("Hard");
    }

    // =========================
    // JSON 处理
    // =========================

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

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
