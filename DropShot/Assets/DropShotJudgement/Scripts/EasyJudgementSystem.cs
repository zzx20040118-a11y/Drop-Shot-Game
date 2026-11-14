using UnityEngine;

namespace DropShot.Judgement
{
    // Keep the shared chart, clock, scoring and events; only replace keyboard input.
    // Inheriting JudgementSystem preserves existing scene references to that type.
    public class EasyJudgementSystem : JudgementSystem
    {
        public int CurrentLane { get; private set; }

        bool strikeKeyHeld;
        int heldLane;

        protected override void ReadKeyboardInput(double t)
        {
            if (KeyInput.Down(lane0Key)) CurrentLane = 0;
            if (KeyInput.Down(lane1Key)) CurrentLane = 1;

            if (KeyInput.Down(strikeKey))
            {
                strikeKeyHeld = true;
                if (Core.TryStartStrike(t))
                    heldLane = JudgeCore.StrikeLane;
                else
                {
                    heldLane = CurrentLane;
                    Core.Press(heldLane, t);
                }
            }

            if (strikeKeyHeld && KeyInput.Up(strikeKey))
            {
                Core.Release(heldLane, t);
                strikeKeyHeld = false;
            }
        }
    }
}
