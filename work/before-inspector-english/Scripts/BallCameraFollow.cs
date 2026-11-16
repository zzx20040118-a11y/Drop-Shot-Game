using UnityEngine;

public class BallCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("正常跟随")]
    public float normalSmoothTime = 0.12f;

    [Header("击球追踪")]
    public float chaseSmoothTime = 0.65f;
    public float chaseDistance = 5f;

    [Header("摄像机追赶")]
    public float recoverySmoothTime = 0.25f;
    public float recoveryOffsetSpeed = 5f;
    public float recoveryPositionTolerance = 0.05f;

    [Header("Strike 镜头震动")]
    public bool enableStrikeShake = true;
    [Min(0f)] public float strikeShakeStrength = 0.08f;
    [Min(0f)] public float strikeShakeDuration = 0.16f;
    [Min(1f)] public float strikeShakeFrequency = 25f;

    [Header("判定震屏")]
    public bool enableJudgementShake = true;
    [Min(0f)] public float judgementShakeDuration = 0.15f;
    [Min(1f)] public float judgementShakeFrequency = 35f;

    private float shakeRemaining;
    private float shakeElapsed;
    private Vector3 shakeOffset;

    private float judgementShakeRemaining;
    private float judgementShakeElapsed;
    private float judgementShakeStrength;
    private Vector3 judgementShakeOffset;

    private Vector3 normalOffset;
    private Vector3 currentOffset;
    private Vector3 cameraVelocity;

    private bool isChasing;
    private bool isRecovering;

    public bool IsRecoveryComplete { get; private set; } = true;

    void OnDisable()
    {
        transform.position -= shakeOffset;
        transform.position -= judgementShakeOffset;

        shakeOffset = Vector3.zero;
        judgementShakeOffset = Vector3.zero;

        shakeRemaining = 0f;
        judgementShakeRemaining = 0f;
    }

    // ================= Strike 震屏 =================

    [ContextMenu("Preview Strike Shake (Play Mode)")]
    public void ShakeStrike()
    {
        if (!Application.isPlaying || !enableStrikeShake)
            return;

        shakeRemaining = strikeShakeDuration;
        shakeElapsed = 0f;
    }

    // ================= 判定震屏 =================

    public void ShakeJudgement(float strength)
    {
        if (!Application.isPlaying || !enableJudgementShake)
            return;

        judgementShakeStrength = strength;
        judgementShakeRemaining = judgementShakeDuration;
        judgementShakeElapsed = 0f;
    }

    [ContextMenu("Preview Judgement Shake (Play Mode)")]
    public void PreviewJudgementShake()
    {
        ShakeJudgement(0.2f);
    }

    // ================= 摄像机跟随 =================

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("请指定摄像机跟随目标 Ball！", this);
            enabled = false;
            return;
        }

        normalOffset = transform.position - target.position;
        currentOffset = normalOffset;
    }

    void LateUpdate()
    {
        // 移除上一帧震屏偏移，防止位置累积
        transform.position -= shakeOffset;
        transform.position -= judgementShakeOffset;

        shakeOffset = Vector3.zero;
        judgementShakeOffset = Vector3.zero;

        if (target == null)
            return;

        // 击球追踪
        if (isChasing)
        {
            Vector3 chaseOffset =
                normalOffset + normalOffset.normalized * chaseDistance;

            currentOffset = Vector3.Lerp(
                currentOffset,
                chaseOffset,
                Time.deltaTime * 2f
            );
        }

        // 摄像机恢复
        if (isRecovering)
        {
            currentOffset = Vector3.Lerp(
                currentOffset,
                normalOffset,
                Time.deltaTime * recoveryOffsetSpeed
            );

            if (Vector3.Distance(currentOffset, normalOffset)
                <= recoveryPositionTolerance)
            {
                currentOffset = normalOffset;
            }
        }

        Vector3 targetPosition = target.position + currentOffset;

        float smoothTime = isChasing
            ? chaseSmoothTime
            : isRecovering
                ? recoverySmoothTime
                : normalSmoothTime;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref cameraVelocity,
            smoothTime
        );

        // 恢复完成
        if (isRecovering
            && currentOffset == normalOffset
            && Vector3.Distance(
                transform.position,
                target.position + normalOffset
            ) <= recoveryPositionTolerance)
        {
            isRecovering = false;
            isChasing = false;

            cameraVelocity = Vector3.zero;
            IsRecoveryComplete = true;
        }

        // Strike 震屏
        if (enableStrikeShake
            && shakeRemaining > 0f
            && strikeShakeDuration > 0f)
        {
            shakeElapsed += Time.unscaledDeltaTime;
            shakeRemaining = Mathf.Max(
                0f,
                shakeRemaining - Time.unscaledDeltaTime
            );

            float strength = strikeShakeStrength *
                Mathf.Clamp01(shakeRemaining / strikeShakeDuration);

            float phase = shakeElapsed * strikeShakeFrequency;

            float horizontal = Mathf.PerlinNoise(phase, 17f) * 2f - 1f;
            float vertical = Mathf.PerlinNoise(43f, phase) * 2f - 1f;

            shakeOffset =
                (transform.right * horizontal +
                 transform.up * vertical) * strength;

            transform.position += shakeOffset;
        }

        // 判定震屏：独立于 Strike
        if (enableJudgementShake
            && judgementShakeRemaining > 0f
            && judgementShakeDuration > 0f)
        {
            judgementShakeElapsed += Time.unscaledDeltaTime;

            judgementShakeRemaining = Mathf.Max(
                0f,
                judgementShakeRemaining - Time.unscaledDeltaTime
            );

            float decay = Mathf.Clamp01(
                judgementShakeRemaining / judgementShakeDuration
            );

            float phase =
                judgementShakeElapsed * judgementShakeFrequency;

            float horizontal =
                Mathf.PerlinNoise(phase, 51f) * 2f - 1f;

            float vertical =
                Mathf.PerlinNoise(73f, phase) * 2f - 1f;

            judgementShakeOffset =
                (transform.right * horizontal +
                 transform.up * vertical)
                * judgementShakeStrength * decay;

            transform.position += judgementShakeOffset;
        }
    }

    public void FollowBall()
    {
        isChasing = true;
        isRecovering = false;
        IsRecoveryComplete = false;
    }

    public void ReturnToNormal()
    {
        isChasing = false;
        isRecovering = true;
        IsRecoveryComplete = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
