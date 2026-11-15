using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DropShot.Judgement.EditorTests
{
    // 菜单 DropShot > 判定自检：不用进 Play 模式，模拟按键检查判定逻辑。
    public static class JudgeCoreSelfTest
    {
        static int failures;

        [MenuItem("DropShot/判定自检")]
        public static void RunFromMenu()
        {
            Run();
        }

        // 命令行：Unity -batchmode -projectPath <项目> -executeMethod DropShot.Judgement.EditorTests.JudgeCoreSelfTest.RunBatch
        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static void Run()
        {
            failures = 0;

            // 1. 郑子昕的 JSON 格式能被正确读入
            string json = "{\"notes\":[" +
                "{\"type\":\"Normal\",\"lane\":0,\"time\":19.669,\"duration\":0,\"targetTime\":0}," +
                "{\"type\":\"Long\",\"lane\":0,\"time\":23.595,\"duration\":0.981,\"targetTime\":0}," +
                "{\"type\":\"Strike\",\"lane\":-1,\"time\":31.445,\"duration\":0,\"targetTime\":31.936}]}";
            ChartFile chart = JsonUtility.FromJson<ChartFile>(json);
            Check("读入 3 个音符", chart.notes.Count == 3);
            Check("Long 的 duration", Mathf.Approximately(chart.notes[1].duration, 0.981f));
            Check("Strike 的 targetTime", Mathf.Approximately(chart.notes[2].targetTime, 31.936f));

            // 2. Normal
            Expect("Normal 晚 20ms = Perfect", Normal(), r => { r.core.Press(0, 1.02); }, JudgeResult.Perfect);
            Expect("Normal 晚 80ms = Good", Normal(), r => { r.core.Press(0, 1.08); }, JudgeResult.Good);
            Expect("Normal 早 120ms = Miss", Normal(), r => { r.core.Press(0, 0.88); }, JudgeResult.Miss);
            Expect("太早的按键被忽略，错过后自动 Miss", Normal(), r => { r.core.Press(0, 0.80); Check("  忽略太早按键", r.events.Count == 0); r.core.Tick(1.2); }, JudgeResult.Miss);
            {
                Rig r = Normal();
                r.core.Press(1, 1.0);
                Check("按错通道不判定", r.events.Count == 0);
            }

            // 3. Long
            {
                Rig r = Make(new ChartNote { type = "Long", lane = 0, time = 2f, duration = 1f });
                r.core.Press(0, 2.0);
                r.core.Tick(3.0);
                Check("Long 按住到底：头 Perfect + 尾 Perfect", r.events.Count == 2 && r.events[0].result == JudgeResult.Perfect && r.events[1].phase == JudgePhase.Tail && r.events[1].result == JudgeResult.Perfect);
            }
            {
                Rig r = Make(new ChartNote { type = "Long", lane = 0, time = 2f, duration = 1f });
                r.core.Press(0, 2.0);
                r.core.Release(0, 2.5);
                Check("Long 提前 500ms 松开 = 尾 Miss", r.events.Count == 2 && r.events[1].result == JudgeResult.Miss);
            }

            // 4. Strike
            {
                Rig r = Make(new ChartNote { type = "Strike", lane = -1, time = 4f, targetTime = 4.5f });
                r.core.Press(JudgeCore.StrikeLane, 4.05);
                Check("Strike 按下只开始蓄力", r.events.Count == 0 && r.chargeStarts == 1);
                r.core.Release(JudgeCore.StrikeLane, 4.52);
                Check("Strike 在卡点松开 = Perfect", r.events.Count == 1 && r.events[0].phase == JudgePhase.Release && r.events[0].result == JudgeResult.Perfect && r.chargeEnds == 1);
            }
            {
                Rig r = Make(new ChartNote { type = "Strike", lane = -1, time = 4f, targetTime = 4.5f });
                r.core.Press(JudgeCore.StrikeLane, 4.0);
                r.core.Tick(4.65);
                Check("Strike 按住太久没松 = Miss", r.events.Count == 1 && r.events[0].result == JudgeResult.Miss && r.chargeEnds == 1);
            }
            Expect("Strike 没按 = Miss", Make(new ChartNote { type = "Strike", lane = -1, time = 4f, targetTime = 4.5f }), r => { r.core.Tick(4.3); }, JudgeResult.Miss);

            // 5. 连击与得分
            {
                Rig r = Make(
                    new ChartNote { type = "Normal", lane = 0, time = 1f },
                    new ChartNote { type = "Normal", lane = 1, time = 1.5f },
                    new ChartNote { type = "Normal", lane = 0, time = 2f });
                r.core.Press(0, 1.0);
                r.core.Press(1, 1.58);
                Check("连击 2", r.core.Combo == 2);
                r.core.Tick(2.5);
                Check("Miss 后连击清零，最大连击 2，得分 400", r.core.Combo == 0 && r.core.MaxCombo == 2 && r.core.Score == 400);
                Check("全部结算后 Finished", r.core.Finished);
            }

            // 6. 同时出现的两个通道互不干扰
            {
                Rig r = Make(
                    new ChartNote { type = "Normal", lane = 0, time = 1f },
                    new ChartNote { type = "Normal", lane = 1, time = 1f });
                r.core.Press(1, 1.0);
                r.core.Press(0, 1.03);
                Check("双押各自判定", r.events.Count == 2 && r.events[0].lane == 1 && r.events[1].lane == 0);
            }

            string summary = failures == 0 ? "[判定自检] 全部通过" : $"[判定自检] {failures} 项失败";
            if (failures == 0) Debug.Log(summary); else Debug.LogError(summary);
        }

        class Rig
        {
            public JudgeCore core;
            public List<JudgeEvent> events = new List<JudgeEvent>();
            public int chargeStarts, chargeEnds;
        }

        static Rig Normal() => Make(new ChartNote { type = "Normal", lane = 0, time = 1f });

        static Rig Make(params ChartNote[] notes)
        {
            Rig r = new Rig { core = new JudgeCore(notes, new JudgeSettings()) };
            r.core.Judged += e => r.events.Add(e);
            r.core.StrikeChargeStarted += _ => r.chargeStarts++;
            r.core.StrikeChargeEnded += _ => r.chargeEnds++;
            return r;
        }

        static void Expect(string name, Rig r, System.Action<Rig> act, JudgeResult expected)
        {
            act(r);
            Check(name, r.events.Count >= 1 && r.events[r.events.Count - 1].result == expected);
        }

        static void Check(string name, bool ok)
        {
            if (ok) Debug.Log("PASS  " + name);
            else { failures++; Debug.LogError("FAIL  " + name); }
        }
    }
}
