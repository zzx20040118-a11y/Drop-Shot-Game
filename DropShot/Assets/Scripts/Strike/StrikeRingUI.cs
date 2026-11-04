using UnityEngine;
using UnityEngine.InputSystem;
using DropShot.Judgement;
using TMPro;

public class StrikeRingUI : MonoBehaviour
{
    [Header("外圈")]
    public GameObject strikeRing;
    public RectTransform ringTransform;

    [Header("内圈 PERFECT 目标")]
    public GameObject targetRing;
    public RectTransform targetRingTransform;

    [Header("提示文字")]
    public TMP_Text holdSpaceText;

    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("缩圈设置")]
    [Tooltip("视觉缩圈时间，单位秒")]
    public float visualShrinkDuration = 1.0f;

    private Vector3 initialScale;
    private Vector3 targetScale;

    private bool isCharging;
    private int currentNoteIndex = -1;

    private double chargeStartSongTime;
    private double targetSongTime;

    private void Awake()
    {
        if (ringTransform != null)
        {
            initialScale = ringTransform.localScale;
        }

        CalculateTargetScale();

        Debug.Log("[StrikeRingUI] Awake 初始化完成");
    }

    private void CalculateTargetScale()
    {
        if (ringTransform == null || targetRingTransform == null)
        {
            Debug.LogError("[StrikeRingUI] 圆环 RectTransform 未绑定");
            return;
        }

        // 外圈初始 Scale
        initialScale = ringTransform.localScale;

        // 内圈实际 Scale 作为外圈最终目标
        targetScale = targetRingTransform.localScale;

        Debug.Log(
            $"[StrikeRingUI] InitialScale={initialScale} | " +
            $"TargetScale={targetScale}"
        );
    }

    private void OnEnable()
    {
        if (judgement == null)
        {
            Debug.LogError("[StrikeRingUI] Judgement 未绑定");
            return;
        }

        judgement.StrikeAutoStarted += ShowRing;
        judgement.StrikeChargeEnded += HideRing;

        Debug.Log("[StrikeRingUI] 已订阅 Judgement 事件");
    }

    private void OnDisable()
    {
        if (judgement == null)
            return;

        judgement.StrikeAutoStarted -= ShowRing;
        judgement.StrikeChargeEnded -= HideRing;
    }

    private void Update()
    {
        // 检测 Space 按下
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[StrikeRingUI] 检测到 Space");

            if (!isCharging)
            {
                StartCharging();
            }
        }

        if (!isCharging)
            return;

        if (judgement == null || ringTransform == null)
            return;

        double currentSongTime = judgement.SongTime;

        double duration = targetSongTime - chargeStartSongTime;

        if (duration <= 0)
        {
            ringTransform.localScale = targetScale;
            return;
        }

        float progress = Mathf.Clamp01(
            (float)((currentSongTime - chargeStartSongTime) / duration)
        );

        // 缩圈
        ringTransform.localScale = Vector3.Lerp(
            initialScale,
            targetScale,
            progress
        );

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log(
                $"[StrikeRingUI] SongTime={currentSongTime:F3} | " +
                $"Progress={progress:F3} | " +
                $"Scale={ringTransform.localScale}"
            );
        }
    }

    private void StartCharging()
    {
        Debug.Log("[StrikeRingUI] StartCharging 已执行");

        if (judgement == null)
        {
            Debug.LogError("[StrikeRingUI] Judgement 为 null");
            return;
        }

        if (judgement.Chart == null)
        {
            Debug.LogError("[StrikeRingUI] Chart 为 null");
            return;
        }

        if (currentNoteIndex < 0 ||
            currentNoteIndex >= judgement.Chart.notes.Count)
        {
            Debug.LogError(
                $"[StrikeRingUI] 当前音符索引无效: {currentNoteIndex}"
            );
            return;
        }

        ChartNote note = judgement.Chart.notes[currentNoteIndex];

        if (note.type != "Strike")
        {
            Debug.LogWarning(
                $"[StrikeRingUI] 当前音符不是 Strike: {note.type}"
            );
            return;
        }

        // 重新读取圆环尺寸
        CalculateTargetScale();

        chargeStartSongTime = judgement.SongTime;
        targetSongTime = note.targetTime;

        isCharging = true;

        Debug.Log(
            $"[StrikeRingUI] 开始缩圈 | " +
            $"NoteIndex={currentNoteIndex} | " +
            $"Start={chargeStartSongTime:F3} | " +
            $"Target={targetSongTime:F3}"
        );

        // 缩圈开始时恢复外圈大小
        ringTransform.localScale = initialScale;

        if (holdSpaceText != null)
        {
            holdSpaceText.gameObject.SetActive(false);
        }
    }

    private void ShowRing(int noteIndex)
    {
        currentNoteIndex = noteIndex;

        Debug.Log(
            $"[StrikeRingUI] ShowRing | NoteIndex={noteIndex}"
        );

        if (strikeRing != null)
        {
            strikeRing.SetActive(true);
        }

        if (targetRing != null)
        {
            targetRing.SetActive(true);
        }

        isCharging = false;

        if (ringTransform != null)
        {
            ringTransform.localScale = initialScale;
        }

        if (holdSpaceText != null)
        {
            holdSpaceText.gameObject.SetActive(true);
        }
    }

    private void HideRing(int noteIndex)
    {
        Debug.Log(
            $"[StrikeRingUI] HideRing | NoteIndex={noteIndex}"
        );

        if (strikeRing != null)
        {
            strikeRing.SetActive(false);
        }

        if (targetRing != null)
        {
            targetRing.SetActive(false);
        }

        isCharging = false;
        currentNoteIndex = -1;

        if (ringTransform != null)
        {
            ringTransform.localScale = initialScale;
        }

        if (holdSpaceText != null)
        {
            holdSpaceText.gameObject.SetActive(true);
        }
    }
}
