using UnityEngine;
using DropShot.Judgement;

public class LaneHitInput : MonoBehaviour
{
    public JudgementSystem judge;
    public BallLaneControl ballLane;
    public BallHit ballHit;
    private int heldLane;
    private bool spaceHeld;

    void OnEnable()
    {
        // 换轨 + 空格打击由这边和球轨脚本负责，关掉判定器自带的键盘，避免 D/F 被当成击打。
        if (judge != null) judge.readKeyboard = false;
        judge.Judged += OnJudged;
    }

    void OnDisable()
    {
        judge.Judged -= OnJudged;
    }

    // Run after D/F has updated the selected lane.
    void LateUpdate()
    {
        if (!judge.Running || judge.Core == null)
            return;

        KeyCode hitKey = judge.strikeKey;
        if (KeyInput.Down(hitKey))
        {
            double time = judge.SongTime;
            spaceHeld = true;
            if (judge.Core.TryStartStrike(time))
            {
                heldLane = JudgeCore.StrikeLane;
            }
            else
            {
                heldLane = ballLane.currentLane;
                judge.Core.Press(heldLane, time);
            }
        }

        if (spaceHeld && KeyInput.Up(hitKey))
        {
            judge.Core.Release(heldLane, judge.SongTime);
            spaceHeld = false;
        }
    }

    void OnGUI()
    {
        if (judge.Chart == null) return;
        string hint = "Chart complete";
        foreach (var note in judge.Chart.notes)
        {
            if (note.time + judge.settings.goodWindow < judge.SongTime) continue;
            string lane = note.lane == 0 ? "LEFT (D)" : "RIGHT (F)";
            hint = note.type == "Strike"
                ? $"CHARGE: hold SPACE at {note.time:F2}s, release at {note.targetTime:F2}s"
                : $"{note.type}: {lane} + SPACE at {note.time:F2}s";
            break;
        }
        GUI.Box(new Rect(12, 150, 650, 35), hint);
    }

    void OnJudged(JudgeEvent result)
    {
        Debug.Log($"[Hit Test] {result.result} | note {result.noteIndex + 1} | lane {result.lane} | {result.phase} | {result.offsetMs:+0;-0;0} ms");
        if (result.result != JudgeResult.Miss && ballHit != null)
            ballHit.Hit();
    }
}