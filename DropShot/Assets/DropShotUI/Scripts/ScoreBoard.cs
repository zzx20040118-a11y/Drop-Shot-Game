using UnityEngine;
using DropShot.Judgement;

namespace DropShot.UI
{
    // 跨场景传成绩：游戏场景写，结局场景读
    public static class ScoreBoard
    {
        public static bool HasResult;
        public static int Score, Perfect, Good, Miss, MaxCombo, NoteCount;

        // 准确率：Perfect 记 1，Good 记 0.5
        public static float Accuracy
        {
            get
            {
                int judged = Perfect + Good + Miss;
                return judged == 0 ? 0f : (Perfect + 0.5f * Good) / judged;
            }
        }

        public static string Rank
        {
            get
            {
                float a = Accuracy;
                if (a >= 0.95f) return "S";
                if (a >= 0.85f) return "A";
                if (a >= 0.70f) return "B";
                return "C";
            }
        }

        public static void Record(JudgeCore core)
        {
            HasResult = true;
            Score = core.Score;
            Perfect = core.PerfectCount;
            Good = core.GoodCount;
            Miss = core.MissCount;
            MaxCombo = core.MaxCombo;
            NoteCount = core.NoteCount;
        }
    }
}
