using System.Collections;
using UnityEngine;
using TMPro;
using DropShot.Judgement;

public class GameplayCountdown : MonoBehaviour
{
    [Header("倒计时 UI")]
    public TMP_Text countdownText;

    [Header("判定系统")]
    public JudgementSystem judgementSystem;

    [Header("场景移动")]
    public StagePositionMove stagePositionMove;

    [Header("球体特效")]
    public GameObject ballEffectController;

    [Header("倒计时音效")]
    public AudioSource countdownAudio;
    public AudioClip tickClip;
    public AudioClip goClip;

    [Header("时间设置")]
    public float numberDuration = 1f;
    public float goDuration = 0.5f;
    public float startDelayAfterGo = 1f;

    [Header("动画设置")]
    public float popScale = 1.5f;
    public float animationDuration = 0.2f;

    private bool started = false;
    private Coroutine scaleCoroutine;

    void Start()
    {
        if (judgementSystem == null)
            judgementSystem = FindObjectOfType<JudgementSystem>();

        if (stagePositionMove == null)
            stagePositionMove = FindObjectOfType<StagePositionMove>();

        if (judgementSystem == null || countdownText == null)
        {
            Debug.LogError("[Countdown] 缺少 JudgementSystem 或 CountdownText！");
            return;
        }

        // 倒计时期间停止判定
        judgementSystem.Stop();

        // 倒计时期间停止场景移动
        if (stagePositionMove != null)
            stagePositionMove.StopMoving();

        // 倒计时期间关闭球体特效
        if (ballEffectController != null)
            ballEffectController.SetActive(false);

        // 初始化倒计时 UI
        countdownText.gameObject.SetActive(true);
        countdownText.transform.localScale = Vector3.one;

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        yield return ShowNumber("3");
        yield return ShowNumber("2");
        yield return ShowNumber("1");

        // GO 音效
        countdownText.text = "GO!";
        PlaySound(goClip);
        AnimateScale();

        // GO 显示时间
        yield return new WaitForSeconds(goDuration);

        // GO 消失
        countdownText.gameObject.SetActive(false);

        // GO 结束后额外等待
        yield return new WaitForSeconds(startDelayAfterGo);

        // 正式开始游戏
        StartGame();
    }

    IEnumerator ShowNumber(string number)
    {
        countdownText.text = number;

        PlaySound(tickClip);
        AnimateScale();

        yield return new WaitForSeconds(numberDuration);
    }

    void PlaySound(AudioClip clip)
    {
        if (countdownAudio != null && clip != null)
            countdownAudio.PlayOneShot(clip);
    }

    void AnimateScale()
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleAnimation());
    }

    IEnumerator ScaleAnimation()
    {
        Transform target = countdownText.transform;

        target.localScale = Vector3.one * popScale;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);

            target.localScale = Vector3.Lerp(
                Vector3.one * popScale,
                Vector3.one,
                t
            );

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    void StartGame()
    {
        if (started) return;
        started = true;

        // 开始判定
        if (judgementSystem != null)
            judgementSystem.Begin();

        // 开始场景移动
        if (stagePositionMove != null)
            stagePositionMove.StartMoving();

        // 开启球体特效
        if (ballEffectController != null)
            ballEffectController.SetActive(true);

        Debug.Log("[Countdown] 游戏正式开始！");
    }
}
