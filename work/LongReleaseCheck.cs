using System;
using System.Collections.Generic;
using DropShot.Judgement;

static class LongReleaseCheck
{
    static int passed;
    static int failed;
    static JudgeSettings settings;

    sealed class Rig
    {
        public JudgeCore core;
        public List<JudgeEvent> events = new List<JudgeEvent>();
        public int ends;
        public Rig(params ChartNote[] notes)
        {
            core = new JudgeCore(notes, settings);
            core.Judged += e => events.Add(e);
            core.LongHoldEnded += _ => ends++;
        }
    }

    static Rig Long()
    {
        var r = new Rig(new ChartNote { type = "Long", lane = 0, time = 2f, duration = 1f });
        r.core.Press(0, 2);
        return r;
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
    }

    static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    static void Release(double offset, JudgeResult expected)
    {
        var r = Long();
        if (offset >= 0 && offset <= settings.goodWindow) r.core.Tick(3);
        r.core.Release(0, 3 + offset);
        Require(r.events.Count == 2, "Expected head and tail exactly once");
        Require(r.events[0].result == JudgeResult.Perfect, "Head changed");
        Require(r.events[1].phase == JudgePhase.Tail && r.events[1].result == expected, "Incorrect release judgement");
        Require(Math.Abs(r.events[1].offsetMs - offset * 1000) < 0.01, "Incorrect release offset");
        Require(r.ends == 1 && r.core.Finished, "Hold did not finish exactly once");
    }

    static int Main()
    {
        foreach (bool gameplay in new[] { false, true })
        {
            settings = gameplay ? new JudgeSettings { perfectWindow = 0.08f, goodWindow = 0.15f, missWindow = 0.2f } : new JudgeSettings();
            string prefix = gameplay ? "Gameplay " : "Default ";
            Test(prefix + "early Perfect", () => Release(-settings.perfectWindow / 2, JudgeResult.Perfect));
            Test(prefix + "late Perfect", () => Release(settings.perfectWindow / 2, JudgeResult.Perfect));
            Test(prefix + "exact release", () => Release(0, JudgeResult.Perfect));
            Test(prefix + "early Good", () => Release(-(settings.perfectWindow + settings.goodWindow) / 2, JudgeResult.Good));
            Test(prefix + "late Good", () => Release((settings.perfectWindow + settings.goodWindow) / 2, JudgeResult.Good));
            Test(prefix + "early Miss", () => Release(-settings.goodWindow - 0.01, JudgeResult.Miss));
            Test(prefix + "late Miss without intervening tick", () => Release(settings.goodWindow + 0.01, JudgeResult.Miss));
            Test(prefix + "early Perfect boundary", () => Release(-settings.perfectWindow, JudgeResult.Perfect));
            Test(prefix + "late Perfect boundary", () => Release(settings.perfectWindow, JudgeResult.Perfect));
            Test(prefix + "early Good boundary", () => Release(-settings.goodWindow, JudgeResult.Good));
            Test(prefix + "late Good boundary", () => Release(settings.goodWindow, JudgeResult.Good));
            Test(prefix + "hold through endpoint and grace window", () =>
            {
                var r = Long();
                r.core.Tick(3);
                Require(r.events.Count == 1 && !r.core.Finished, "Auto-completed at endpoint");
                r.core.Tick(3.0 + settings.goodWindow);
                Require(r.events.Count == 1 && r.ends == 0, "Auto-completed inside release window");
                r.core.Tick(3.0 + settings.goodWindow + 0.001);
                Require(r.events.Count == 2 && r.events[1].result == JudgeResult.Miss && r.events[1].phase == JudgePhase.Tail, "No timeout Miss");
                Require(r.ends == 1 && r.core.Finished && r.core.Combo == 0, "Timeout state not cleaned up");
                r.core.Tick(4); r.core.Release(0, 4);
                Require(r.events.Count == 2 && r.ends == 1, "Repeated tail result after timeout");
            });
        }
        Test("Normal timing unchanged", () =>
        {
            var r = new Rig(new ChartNote { type = "Normal", lane = 0, time = 1 });
            r.core.Press(0, 1.02);
            Require(r.events.Count == 1 && r.events[0].result == JudgeResult.Perfect && r.core.Finished, "Normal changed");
        });
        Test("Strike release unchanged", () =>
        {
            var r = new Rig(new ChartNote { type = "Strike", lane = -1, time = 4, targetTime = 4.5f });
            Require(r.core.TryStartStrike(4), "Strike failed to start");
            r.core.Release(-1, 4.52);
            Require(r.events.Count == 1 && r.events[0].phase == JudgePhase.Release && r.events[0].result == JudgeResult.Perfect, "Strike release changed");
        });
        Test("Strike hold timeout unchanged", () =>
        {
            var r = new Rig(new ChartNote { type = "Strike", lane = -1, time = 4, targetTime = 4.5f });
            r.core.TryStartStrike(4); r.core.Tick(4.7);
            Require(r.events.Count == 1 && r.events[0].result == JudgeResult.Miss, "Strike timeout changed");
        });
        Test("Long hold and other lane remain independent", () =>
        {
            var r = new Rig(new ChartNote { type = "Long", lane = 0, time = 2, duration = 1 }, new ChartNote { type = "Normal", lane = 1, time = 2.5f });
            r.core.Press(0, 2); r.core.Press(1, 2.5); r.core.Tick(3); r.core.Release(0, 3.02);
            Require(r.events.Count == 3 && r.events[2].phase == JudgePhase.Tail && r.events[2].result == JudgeResult.Perfect && r.core.Finished, "Lanes interfere");
        });
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
