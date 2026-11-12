using UnityEngine;
using DropShot.Judgement;

namespace DropShot.UI
{
    // 挂在游戏场景任意物体上：每次判定都把成绩存进 ScoreBoard，
    // 这样不管是曲终还是 Portal 提前切到 End 场景，结局页都能拿到最新分数
    public class ScoreReporter : MonoBehaviour
    {
        public JudgementSystem judge;

        void Awake()
        {
            if (judge == null) judge = FindAnyObjectByType<JudgementSystem>();
            ScoreBoard.HasResult = false;
        }

        void OnEnable() { if (judge != null) judge.Judged += OnJudged; }
        void OnDisable() { if (judge != null) judge.Judged -= OnJudged; }

        void OnJudged(JudgeEvent e)
        {
            if (judge.Core != null) ScoreBoard.Record(judge.Core);
        }
    }
}
