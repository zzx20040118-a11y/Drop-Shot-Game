using DropShot.Judgement;

public class EasyLaneHitInput : LaneHitInput
{
    protected override void OnEnable()
    {
        if (judge == null)
            return;
        judge.readKeyboard = false;
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (judge != null)
            base.OnDisable();
    }

    protected override bool HitPressed()
    {
        return judge != null && KeyInput.Down(judge.strikeKey);
    }

    protected override bool HitReleased()
    {
        return judge != null && KeyInput.Up(judge.strikeKey);
    }
}
