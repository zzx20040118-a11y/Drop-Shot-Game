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

    private Vector3 normalOffset;
    private Vector3 currentOffset;
    private Vector3 cameraVelocity;

    private bool isChasing;
    private bool isRecovering;

    public bool IsRecoveryComplete { get; private set; } = true;

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
        if (target == null)
            return;

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

    // 切换摄像机跟随目标
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
