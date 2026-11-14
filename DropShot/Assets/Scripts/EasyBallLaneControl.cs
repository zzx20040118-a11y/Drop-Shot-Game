using UnityEngine;
using DropShot.Judgement;

public class EasyBallLaneControl : BallLaneControl
{
    public JudgementSystem judge;

    protected override void Start()
    {
        base.Start();
        if (judge == null)
            judge = FindFirstObjectByType<JudgementSystem>();
    }

    protected override bool ReadLaneInput()
    {
        KeyCode left = judge != null ? judge.lane0Key : KeyCode.D;
        KeyCode right = judge != null ? judge.lane1Key : KeyCode.F;
        if (KeyInput.Down(left)) ChangeLane(0);
        if (KeyInput.Down(right)) ChangeLane(1);
        return true;
    }
}
