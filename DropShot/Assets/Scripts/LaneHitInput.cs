using UnityEngine;
using UnityEngine.InputSystem;
using DropShot.Judgement;

public class LaneHitInput : MonoBehaviour
{
    public JudgementSystem judge;
    public BallLaneControl ballLane;
    public BallHit ballHit;
    private int heldLane;
    private bool spaceHeld;

    protected virtual void OnEnable()
    {
        judge.Judged += OnJudged;
    }

    protected virtual void OnDisable()
    {
        judge.Judged -= OnJudged;
    }

    // Run after D/F has updated the selected lane.
    protected virtual void LateUpdate()
    {
        if (!judge.Running || judge.Core == null)
            return;

        if (HitPressed())
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

        if (spaceHeld && HitReleased())
        {
            judge.Core.Release(heldLane, judge.SongTime);
            spaceHeld = false;
        }
    }

    protected virtual bool HitPressed()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    protected virtual bool HitReleased()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
    }

    protected virtual void OnGUI()
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
