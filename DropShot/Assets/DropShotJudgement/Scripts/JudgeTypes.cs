using System;
using System.Collections.Generic;

namespace DropShot.Judgement
{
    // 与郑子昕的谱面 JSON 字段一一对应：{"notes":[{"type","lane","time","duration","targetTime"}]}
    [Serializable]
    public class ChartNote
    {
        public string type;        // "Normal" / "Long" / "Strike"
        public int lane;           // 0 = D，1 = F，-1 = Strike（空格）
        public float time;         // 击打时间（秒）；Strike 为开始按住空格的时间
        public float duration;     // Long 的持续时间（秒）
        public float targetTime;   // Strike 的松开卡点时间（秒）
    }

    [Serializable]
    public class ChartFile
    {
        public List<ChartNote> notes = new List<ChartNote>();
    }

    public enum NoteKind { Normal, Long, Strike }

    public enum JudgeResult { Perfect, Good, Miss }

    // Head = 按下（Normal 只有这一段）；Tail = Long 结束；Release = Strike 松开
    public enum JudgePhase { Head, Tail, Release }

    public struct JudgeEvent
    {
        public int noteIndex;      // 谱面 notes 数组里的下标，便于让音符物体自己消失
        public NoteKind kind;
        public int lane;
        public JudgePhase phase;
        public JudgeResult result;
        public float offsetMs;     // 正数 = 按晚了，负数 = 按早了；自动 Miss 时为 0
        public int combo;          // 判定后的连击数
    }

    [Serializable]
    public class JudgeSettings
    {
        // 判定窗口（秒，单边）。建议初值，原型期试玩后调整
        public float perfectWindow = 0.05f;
        public float goodWindow = 0.10f;
        // 超出 Good 但在此范围内的按键算 Miss；再早的按键直接忽略，不吃音符
        public float missWindow = 0.15f;
        // Strike 按下空格的时机比较宽松，真正计分的是松开
        public float strikePressWindow = 0.15f;

        public int perfectScore = 300;
        public int goodScore = 100;
    }

    public static class NoteKindParser
    {
        public static NoteKind Parse(string type)
        {
            if (string.Equals(type, "Long", StringComparison.OrdinalIgnoreCase)) return NoteKind.Long;
            if (string.Equals(type, "Strike", StringComparison.OrdinalIgnoreCase)) return NoteKind.Strike;
            return NoteKind.Normal;
        }
    }
}
