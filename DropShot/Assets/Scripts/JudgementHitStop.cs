using UnityEngine;
using DropShot.Judgement;

public class JudgementHitStop : MonoBehaviour
{
    [Header("顿帧时长")]
    public float perfectHitStop = 0.1f;
    public float goodHitStop = 0.05f;

    [Header("判定震屏强度")]
    public float perfectShakeStrength = 0.2f;
    public float goodShakeStrength = 0.08f;

    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("摄像机")]
    public BallCameraFollow ballCameraFollow;

    private void OnEnable()
    {
        if (judgement != null)
            judgement.Judged += OnJudged;
    }

    private void OnDisable()
    {
        if (judgement != null)
            judgement.Judged -= OnJudged;
    }

    private void OnJudged(JudgeEvent e)
    {
        // Strike 保留原本的慢动作与击球震屏
        if (e.kind == NoteKind.Strike)
            return;

        if (e.result == JudgeResult.Perfect)
        {
            ApplyHitStop(perfectHitStop);
            ApplyShake(perfectShakeStrength);
        }
        else if (e.result == JudgeResult.Good)
        {
            ApplyHitStop(goodHitStop);
            ApplyShake(goodShakeStrength);
        }
    }

    private void ApplyHitStop(float duration)
    {
        if (HitStop.Instance != null)
            HitStop.Instance.Stop(duration);
    }

    private void ApplyShake(float strength)
    {
        if (ballCameraFollow != null)
            ballCameraFollow.ShakeJudgement(strength);
    }
}
