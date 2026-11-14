using UnityEngine;
using DropShot.Judgement;

public class EasyBallLaneFollow : BallLaneFollow
{
    public JudgementSystem judge;

    protected override void Start()
    {
        base.Start();
        if (judge == null)
            judge = FindFirstObjectByType<JudgementSystem>();
    }

    protected override void ReadLaneInput()
    {
        KeyCode left = judge != null ? judge.lane0Key : KeyCode.D;
        KeyCode right = judge != null ? judge.lane1Key : KeyCode.F;
        if (KeyInput.Down(left)) SelectLane(0);
        if (KeyInput.Down(right)) SelectLane(1);
    }
}
