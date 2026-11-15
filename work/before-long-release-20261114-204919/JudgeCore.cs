using System;
using System.Collections.Generic;

namespace DropShot.Judgement
{
    // 纯逻辑判定器：不依赖 Unity，只吃「时间 + 按键」，吐出判定事件。
    // JudgementSystem 负责把歌曲时间和键盘输入喂给它。
    public class JudgeCore
    {
        public const int StrikeLane = -1;

        enum State { Pending, Holding, Done }

        struct Entry
        {
            public int index;      // 原谱面下标
            public NoteKind kind;
            public int lane;
            public double time;
            public double end;      // Long = time + duration；Strike = targetTime；Normal = time
            public State state;
            public bool strikePressed; // Strike 是否已经按下 Space
            public bool isStrikeWall;

            // Strike 蓄力开始时间
            public double strikeChargeStartTime;

            // Strike 实际蓄力时长
            public double strikeChargeDuration;
        }

        readonly Entry[] entries;
        readonly JudgeSettings settings;
        readonly Dictionary<int, int> holding = new Dictionary<int, int>(); // lane -> entries 下标
        int cursor;
        int wallNoteCount;

        public event Action<JudgeEvent> Judged;
        public event Action<int> StrikeChargeStarted;   // 参数 = 谱面下标
        public event Action<int> StrikeChargeEnded;
        public event Action<int> StrikeAutoStarted;
        public event Action<int> LongHoldStarted;
        public event Action<int> LongHoldEnded;

        // Strike 蓄力相关事件
        public event Action<int, double> StrikeChargeUpdated;

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Score { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        // Strike 当前蓄力时长
        public double StrikeChargeDuration { get; private set; }

        // Strike 当前蓄力比例（0~1）
        public float StrikeChargeProgress { get; private set; }

        // 蓄力评分设置
        public double MaxStrikeChargeDuration { get; set; } = 2.0;
        public int MaxStrikeChargeScore { get; set; } = 300;

        public int NoteCount => entries.Length - wallNoteCount;
        public bool Finished => cursor >= entries.Length && holding.Count == 0;

        public JudgeCore(IList<ChartNote> notes, JudgeSettings settings)
        {
            this.settings = settings ?? new JudgeSettings();
            entries = new Entry[notes.Count];

            for (int i = 0; i < notes.Count; i++)
            {
                ChartNote n = notes[i];
                NoteKind kind = NoteKindParser.Parse(n.type);

                double end = kind == NoteKind.Long ? n.time + n.duration
                             : kind == NoteKind.Strike ? n.targetTime
                             : n.time;

                entries[i] = new Entry
                {
                    index = i,
                    kind = kind,
                    lane = kind == NoteKind.Strike ? StrikeLane : n.lane,
                    time = n.time,
                    end = end,
                    state = State.Pending,
                    strikePressed = false,
                    strikeChargeStartTime = 0,
                    strikeChargeDuration = 0
                };
            }

            Array.Sort(entries, (a, b) => a.time.CompareTo(b.time));
        }

        // This normal note becomes scenery for the preceding Strike, not a second input.
        public bool UseAsStrikeWall(int noteIndex)
        {
            for (int k = 0; k < entries.Length; k++)
            {
                if (entries[k].index != noteIndex || entries[k].kind != NoteKind.Normal
                    || entries[k].state != State.Pending || entries[k].isStrikeWall)
                    continue;

                entries[k].isStrikeWall = true;
                wallNoteCount++;
                return true;
            }
            return false;
        }

        // Returns true only when this press starts a pending charge note.
        public bool TryStartStrike(double t)
        {
            Tick(t);
            if (holding.ContainsKey(StrikeLane) || FindCandidate(StrikeLane, t) < 0)
                return false;

            Press(StrikeLane, t);
            return true;
        }

        public void Press(int lane, double t)
        {
            Tick(t);
            if (holding.ContainsKey(lane)) return;

            int k = FindCandidate(lane, t);
            if (k < 0) return; // 空按：不吃音符、不扣分

            ref Entry e = ref entries[k];
            double d = t - e.time;

            switch (e.kind)
            {
                case NoteKind.Normal:
                    e.state = State.Done;
                    Emit(e, JudgePhase.Head, Grade(d), d);
                    break;

                case NoteKind.Long:
                    JudgeResult head = Grade(d);
                    Emit(e, JudgePhase.Head, head, d);

                    if (head == JudgeResult.Miss)
                    {
                        e.state = State.Done;
                    }
                    else
                    {
                        e.state = State.Holding;
                        holding[lane] = k;
                        LongHoldStarted?.Invoke(e.index);
                    }
                    break;

                case NoteKind.Strike:
                    // 按下只是开始蓄力，计分看松开
                    if (e.state != State.Holding || e.strikePressed)
                        return;

                    e.strikePressed = true;

                    // 记录蓄力开始时间
                    e.strikeChargeStartTime = t;
                    e.strikeChargeDuration = 0;

                    StrikeChargeDuration = 0;
                    StrikeChargeProgress = 0;

                    holding[lane] = k;

                    StrikeChargeStarted?.Invoke(e.index);
                    StrikeChargeUpdated?.Invoke(e.index, 0);
                    break;
            }

            AdvanceCursor();
        }

        public void Release(int lane, double t)
        {
            if (!holding.TryGetValue(lane, out int k)) return;

            ref Entry e = ref entries[k];

            // Strike 尚未按下 Space，不允许提前结束
            if (e.kind == NoteKind.Strike && !e.strikePressed)
                return;

            holding.Remove(lane);
            e.state = State.Done;

            double d = t - e.end;

            if (e.kind == NoteKind.Long)
            {
                // 提前松开：在 Good 窗口内仍算成功，否则断掉
                Emit(e, JudgePhase.Tail, Grade(d), d);
                LongHoldEnded?.Invoke(e.index);
            }
            else
            {
                // 计算 Strike 实际蓄力时间
                e.strikeChargeDuration = Math.Max(
                    0,
                    t - e.strikeChargeStartTime
                );

                StrikeChargeDuration = e.strikeChargeDuration;

                // 计算蓄力比例
                double maxDuration = Math.Max(0.01, MaxStrikeChargeDuration);

                StrikeChargeProgress = MathfClamp01(
                    e.strikeChargeDuration / maxDuration
                );

                // 原有时间判定仍然保留
                JudgeResult result = Grade(d);

                // 原有 Perfect / Good / Miss 分数
                Emit(e, JudgePhase.Release, result, d);

                // 蓄力额外得分：蓄力越久，额外分越高
                if (result != JudgeResult.Miss)
                {
                    int chargeScore = (int)Math.Round(
                        StrikeChargeProgress * MaxStrikeChargeScore
                    );

                    Score += chargeScore;
                }

                StrikeChargeUpdated?.Invoke(
                    e.index,
                    e.strikeChargeDuration
                );

                StrikeChargeEnded?.Invoke(e.index);
            }

            AdvanceCursor();
        }

        // 每帧调用：结算按住到底的长条、超时没松的 Strike、以及错过的音符
        public void Tick(double t)
        {
            // Strike 到达谱面 time 时自动进入 Holding 并触发事件
            for (int k = 0; k < entries.Length; k++)
            {
                ref Entry e = ref entries[k];

                if (e.kind != NoteKind.Strike ||
                    e.state != State.Pending ||
                    t < e.time)
                    continue;

                e.state = State.Holding;
                StrikeAutoStarted?.Invoke(e.index);
            }

            // Strike 自动进入 Holding 后，如果玩家一直没有按 Space，
            // 超过目标时间的 Good 窗口就自动 Miss
            for (int k = 0; k < entries.Length; k++)
            {
                ref Entry e = ref entries[k];

                if (e.kind != NoteKind.Strike ||
                    e.state != State.Holding ||
                    e.strikePressed)
                    continue;

                if (t > e.end + settings.goodWindow)
                {
                    e.state = State.Done;
                    Emit(e, JudgePhase.Release, JudgeResult.Miss, 0);
                    StrikeChargeEnded?.Invoke(e.index);
                }
            }

            // 更新正在蓄力的 Strike 时长
            for (int k = 0; k < entries.Length; k++)
            {
                ref Entry e = ref entries[k];

                if (e.kind != NoteKind.Strike ||
                    e.state != State.Holding ||
                    !e.strikePressed)
                    continue;

                e.strikeChargeDuration = Math.Max(
                    0,
                    t - e.strikeChargeStartTime
                );

                StrikeChargeDuration = e.strikeChargeDuration;

                double maxDuration = Math.Max(0.01, MaxStrikeChargeDuration);

                StrikeChargeProgress = MathfClamp01(
                    e.strikeChargeDuration / maxDuration
                );

                StrikeChargeUpdated?.Invoke(
                    e.index,
                    e.strikeChargeDuration
                );
            }

            if (holding.Count > 0)
            {
                foreach (int lane in new List<int>(holding.Keys))
                {
                    int k = holding[lane];
                    ref Entry e = ref entries[k];

                    if (e.kind == NoteKind.Long && t >= e.end)
                    {
                        holding.Remove(lane);
                        e.state = State.Done;
                        Emit(e, JudgePhase.Tail, JudgeResult.Perfect, 0);
                        LongHoldEnded?.Invoke(e.index);
                    }
                    else if (e.kind == NoteKind.Strike && t > e.end + settings.goodWindow)
                    {
                        holding.Remove(lane);
                        e.state = State.Done;
                        Emit(e, JudgePhase.Release, JudgeResult.Miss, 0);
                        StrikeChargeEnded?.Invoke(e.index);
                    }
                }
            }

            for (int k = cursor; k < entries.Length; k++)
            {
                ref Entry e = ref entries[k];

                if (e.time > t) break;
                if (e.state != State.Pending) continue;

                if (e.isStrikeWall)
                {
                    e.state = State.Done;
                    continue;
                }

                double late = e.kind == NoteKind.Strike ? settings.strikePressWindow : settings.goodWindow;

                if (t - e.time > late)
                {
                    e.state = State.Done;
                    Emit(e, e.kind == NoteKind.Strike ? JudgePhase.Release : JudgePhase.Head, JudgeResult.Miss, 0);
                }
            }

            AdvanceCursor();
        }

        int FindCandidate(int lane, double t)
        {
            for (int k = cursor; k < entries.Length; k++)
            {
                Entry e = entries[k];
                if (e.isStrikeWall) continue;
                double window = e.kind == NoteKind.Strike ? settings.strikePressWindow : settings.missWindow;

                if (e.time - t > settings.missWindow + settings.strikePressWindow) break;

                if (e.kind == NoteKind.Strike)
                {
                    // Strike 已自动触发，只允许在 Holding 状态按下 Space
                    if (e.lane == lane &&
                        e.state == State.Holding &&
                        !e.strikePressed &&
                        t >= e.time)
                        return k;

                    continue;
                }

                if (e.state != State.Pending || e.lane != lane) continue;
                if (Math.Abs(t - e.time) <= window) return k;
            }

            return -1;
        }

        JudgeResult Grade(double d)
        {
            double a = Math.Abs(d);
            if (a <= settings.perfectWindow) return JudgeResult.Perfect;
            if (a <= settings.goodWindow) return JudgeResult.Good;
            return JudgeResult.Miss;
        }

        void Emit(Entry e, JudgePhase phase, JudgeResult result, double d)
        {
            switch (result)
            {
                case JudgeResult.Perfect:
                    PerfectCount++;
                    Score += settings.perfectScore;
                    Combo++;
                    break;

                case JudgeResult.Good:
                    GoodCount++;
                    Score += settings.goodScore;
                    Combo++;
                    break;

                default:
                    MissCount++;
                    Combo = 0;
                    break;
            }

            if (Combo > MaxCombo) MaxCombo = Combo;

            Judged?.Invoke(new JudgeEvent
            {
                noteIndex = e.index,
                kind = e.kind,
                lane = e.lane,
                phase = phase,
                result = result,
                offsetMs = (float)(d * 1000.0),
                combo = Combo
            });
        }

        void AdvanceCursor()
        {
            while (cursor < entries.Length && entries[cursor].state == State.Done)
                cursor++;
        }

        // 不依赖 Unity Mathf 的 Clamp01
        static float MathfClamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return (float)value;
        }
    }
}
