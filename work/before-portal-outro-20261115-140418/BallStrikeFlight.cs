using DropShot.Judgement;
using UnityEngine;

public class BallStrikeFlight : MonoBehaviour
{
    public enum BallState
    {
        Normal,
        Flight,
        CameraCatchUp,
    }

    [Header("正常球")]
    public GameObject normalBall;

    [Header("当前状态")]
    public BallState currentState = BallState.Normal;

    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("球体组件")]
    public BallHit ballHit;
    public BallCameraFollow ballCamera;

    [Header("击球设置")]
    public float launchForwardSpeed = 15f;
    public float launchUpSpeed = 8f;
    public float gravity = 18f;

    [Header("落地设置")]
    public float groundY = 0f;
    public float ballRadius = 0.5f;

    private Vector3 velocity;
    private bool isCharging;

    private Renderer[] flightRenderers;
    private Renderer[] normalRenderers;

    private BallMove normalMove;
    private BallLaneFollow normalLaneFollow;
    private BallLaneFollow flightLaneFollow;

    void Awake()
    {
        flightRenderers = GetComponentsInChildren<Renderer>();

        if (normalBall != null)
        {
            normalRenderers = normalBall.GetComponentsInChildren<Renderer>();

            normalMove = normalBall.GetComponent<BallMove>();
            normalLaneFollow = normalBall.GetComponent<BallLaneFollow>();
        }

        flightLaneFollow = GetComponent<BallLaneFollow>();

        SetRenderersVisible(flightRenderers, false);

        if (flightLaneFollow != null)
            flightLaneFollow.enabled = false;
    }

    void OnEnable()
    {
        if (judgement != null)
        {
            judgement.StrikeChargeStarted += OnStrikeChargeStarted;
            judgement.StrikeChargeEnded += OnStrikeChargeEnded;
        }
    }

    void OnDisable()
    {
        if (judgement != null)
        {
            judgement.StrikeChargeStarted -= OnStrikeChargeStarted;
            judgement.StrikeChargeEnded -= OnStrikeChargeEnded;
        }
    }

    void OnStrikeChargeStarted(int noteIndex)
    {
        if (currentState == BallState.Normal)
            isCharging = true;
    }

    void OnStrikeChargeEnded(int noteIndex)
    {
        if (!isCharging)
            return;

        isCharging = false;
        Launch();
    }

    void Update()
    {
        switch (currentState)
        {
            case BallState.Flight:
                UpdateFlight();
                break;

            case BallState.CameraCatchUp:
                UpdateCameraCatchUp();
                break;
        }
    }

    public void Launch()
    {
        if (currentState != BallState.Normal)
            return;

        if (normalBall == null || ballCamera == null)
        {
            Debug.LogError("请指定 NormalBall 和 BallCameraFollow。", this);
            return;
        }

        // 同步飞行球位置
        transform.position = normalBall.transform.position;
        transform.rotation = normalBall.transform.rotation;

        // 停止正常球移动
        if (normalMove != null)
            normalMove.enabled = false;

        if (normalLaneFollow != null)
            normalLaneFollow.enabled = false;

        // 隐藏正常球
        SetRenderersVisible(normalRenderers, false);

        // 显示飞行球
        SetRenderersVisible(flightRenderers, true);

        // 关闭飞行球轨道跟随
        if (flightLaneFollow != null)
            flightLaneFollow.enabled = false;

        currentState = BallState.Flight;

        velocity = new Vector3(-launchForwardSpeed, launchUpSpeed, 0f);

        if (ballHit != null)
            ballHit.Hit();

        ballCamera.SetTarget(transform);
        ballCamera.FollowBall();
    }

    void UpdateFlight()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (transform.position.y <= groundY + ballRadius && velocity.y < 0f)
        {
            Land();
        }
    }

    void Land()
    {
        Vector3 position = transform.position;
        position.y = groundY + ballRadius;

        transform.position = position;

        velocity = Vector3.zero;

        // Hand off at the same position before returning to the lane.
        normalBall.transform.position = transform.position;
        normalBall.transform.rotation = transform.rotation;
        SetRenderersVisible(flightRenderers, false);
        SetRenderersVisible(normalRenderers, true);
        ballCamera.SetTarget(normalBall.transform);

        currentState = BallState.CameraCatchUp;

        ballCamera.ReturnToNormal();
    }

    void UpdateCameraCatchUp()
    {
        Transform hitPoint = normalLaneFollow.CurrentHitPoint;
        if (hitPoint == null)
            return;

        normalBall.transform.position = Vector3.MoveTowards(
            normalBall.transform.position,
            hitPoint.position,
            30f * Time.deltaTime
        );

        if (Vector3.Distance(normalBall.transform.position, hitPoint.position) > 0.03f)
            return;

        normalBall.transform.position = hitPoint.position;
        normalBall.transform.rotation = transform.rotation;

        // 隐藏飞行球
        SetRenderersVisible(flightRenderers, false);

        // 显示正常球
        SetRenderersVisible(normalRenderers, true);

        // 恢复正常球移动
        if (normalMove != null)
            normalMove.enabled = true;

        if (normalLaneFollow != null)
            normalLaneFollow.enabled = true;

        // 切换摄像机目标
        ballCamera.SetTarget(normalBall.transform);

        currentState = BallState.Normal;
    }

    void SetRenderersVisible(Renderer[] renderers, bool visible)
    {
        if (renderers == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}
