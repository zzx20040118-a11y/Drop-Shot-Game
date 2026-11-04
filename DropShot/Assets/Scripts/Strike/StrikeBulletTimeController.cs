using UnityEngine;
using DropShot.Judgement;

public class StrikeBulletTimeController : MonoBehaviour
{
    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("Bullet Time")]
    [Range(0.05f, 1f)]
    public float slowMotionScale = 0.2f;

    private bool isBulletTime = false;

    private void OnEnable()
    {
        if (judgement == null) return;

        judgement.StrikeAutoStarted += OnStrikeStarted;
        judgement.StrikeChargeEnded += OnStrikeEnded;
    }

    private void OnDisable()
    {
        if (judgement != null)
        {
            judgement.StrikeAutoStarted -= OnStrikeStarted;
            judgement.StrikeChargeEnded -= OnStrikeEnded;
        }

        RestoreTime();
    }

    private void OnStrikeStarted(int noteIndex)
    {
        if (isBulletTime) return;

        isBulletTime = true;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log($"[BulletTime] Strike {noteIndex} 自动开始！");
    }

    private void OnStrikeEnded(int noteIndex)
    {
        RestoreTime();

        Debug.Log($"[BulletTime] Strike {noteIndex} 结束！");
    }

    private void RestoreTime()
    {
        isBulletTime = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
