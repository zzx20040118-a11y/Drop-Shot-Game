using UnityEngine;
using DropShot.Judgement;

public class BallLaneFollow : MonoBehaviour
{
    [Header("Lane Reference Points / 轨道参考点")]
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    [Header("Lane Following / 轨道跟随")]
    public bool followLane = true;

    [Header("Ball Steering / 球体转向")]
    public Transform ballRotationTarget;
    public float rotationSpeed = 8f;
    public float leftLaneRotation = -15f;
    public float rightLaneRotation = 15f;

    private int currentLane = 0;
    private BallCameraFollow ballCamera;

    private Quaternion targetRotation;

    protected virtual void Start()
    {
        ballCamera = FindObjectOfType<BallCameraFollow>();

        if (leftHitPoint == null || rightHitPoint == null)
        {
            Debug.LogError("请指定球的左右 HitPoint。", this);
            enabled = false;
            return;
        }

        if (ballRotationTarget == null)
            ballRotationTarget = transform;

        targetRotation = ballRotationTarget.rotation;
    }

    protected virtual void Update()
    {
        ReadLaneInput();

        // 平滑旋转球体（世界坐标）
        ballRotationTarget.rotation = Quaternion.Slerp(
            ballRotationTarget.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    protected virtual void ReadLaneInput()
    {
        if (KeyInput.Down(KeyCode.D)) SelectLane(0);
        if (KeyInput.Down(KeyCode.F)) SelectLane(1);
    }

    protected void SelectLane(int lane)
    {
        currentLane = lane;
        UpdateBallRotation();
    }

    void UpdateBallRotation()
    {
        float xRotation = currentLane == 0
            ? -leftLaneRotation
            : -rightLaneRotation;

        // 使用世界坐标 X 轴旋转
        targetRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void SetLaneFollow(bool value)
    {
        followLane = value;
    }

    public int CurrentLane
    {
        get { return currentLane; }
    }

    public Transform CurrentHitPoint
    {
        get
        {
            return currentLane == 0
                ? leftHitPoint
                : rightHitPoint;
        }
    }
}
