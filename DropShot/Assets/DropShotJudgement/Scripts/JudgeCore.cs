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
            public double end;     // Long = time + duration；Strike = targetTime；Normal = time
            public State state;
        }

        readonly Entry[] entries;
        readonly JudgeSettings settings;
        readonly Dictionary<int, int> holding = new Dictionary<int, int>(); // lane -> entries 下标
        int cursor;

        public event Action<JudgeEvent> Judged;
        public event Action<int> StrikeChargeStarted;   // 参数 = 谱面下标
        public event Action<int> StrikeChargeEnded;
        public event Action<int> LongHoldStarted;
        public event Action<int> LongHoldEnded;

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Score { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }
        public int NoteCount => entries.Length;
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
                };
            }
            Array.Sort(entries, (a, b) => a.time.CompareTo(b.time));
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
                    e.state = State.Holding;
                    holding[lane] = k;
                    StrikeChargeStarted?.Invoke(e.index);
                    break;
            }
            AdvanceCursor();
        }

        public void Release(int lane, double t)
        {
            if (!holding.TryGetValue(lane, out int k)) return;
            holding.Remove(lane);

            ref Entry e = ref entries[k];
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
                Emit(e, JudgePhase.Release, Grade(d), d);
                StrikeChargeEnded?.Invoke(e.index);
            }
            AdvanceCursor();
        }

        // 每帧调用：结算按住到底的长条、超时没松的 Strike、以及错过的音符
        public void Tick(double t)
        {
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
                double window = e.kind == NoteKind.Strike ? settings.strikePressWindow : settings.missWindow;
                if (e.time - t > settings.missWindow + settings.strikePressWindow) break;
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
                case JudgeResult.Perfect: PerfectCount++; Score += settings.perfectScore; Combo++; break;
                case JudgeResult.Good: GoodCount++; Score += settings.goodScore; Combo++; break;
                default: MissCount++; Combo = 0; break;
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
                combo = Combo,
            });
        }

        void AdvanceCursor()
        {
            while (cursor < entries.Length && entries[cursor].state == State.Done) cursor++;
        }
    }
}
