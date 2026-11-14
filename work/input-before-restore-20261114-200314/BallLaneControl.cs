using UnityEngine;
using DropShot.Judgement;

public class BallLaneControl : MonoBehaviour
{
    public float laneSpacing = 3f;
    public int currentLane = 0;

    public float laneChangeDuration = 0.25f;

    public BallLaneFollow laneFollow;
    public JudgementSystem judge;

    private Vector3 targetPosition;
    private Vector3 startPosition;

    private float laneChangeTimer;
    private bool isChangingLane;

    void Start()
    {
        if (laneFollow == null)
            laneFollow = GetComponent<BallLaneFollow>();
        if (judge == null)
            judge = FindObjectOfType<JudgementSystem>();

        if (laneFollow == null)
        {
            Debug.LogError("BallLaneControl 找不到 BallLaneFollow。", this);
            enabled = false;
            return;
        }

        currentLane = laneFollow.CurrentLane;

        Transform hitPoint = laneFollow.CurrentHitPoint;

        if (hitPoint != null)
        {
            Vector3 position = transform.position;
            position.y = hitPoint.position.y;
            position.z = hitPoint.position.z;
            transform.position = position;
        }
    }

    void Update()
    {

        if (laneFollow == null || !laneFollow.enabled)
            return;

        KeyCode left = judge != null ? judge.lane0Key : KeyCode.D;
        KeyCode right = judge != null ? judge.lane1Key : KeyCode.F;
        if (KeyInput.Down(left)) ChangeLane(0);
        if (KeyInput.Down(right)) ChangeLane(1);

        if (!laneFollow.enabled || !laneFollow.followLane)
            return;

        Transform point = currentLane == 0 ? laneFollow.leftHitPoint : laneFollow.rightHitPoint;

        //update target position based on the current lane's hit point
        targetPosition = point.position;

        if (isChangingLane)
        {
            laneChangeTimer += Time.deltaTime;

            float t = Mathf.Clamp01(laneChangeTimer / laneChangeDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = transform.position;

            position.y = Mathf.Lerp(startPosition.y, targetPosition.y, smoothT);

            position.z = Mathf.Lerp(startPosition.z, targetPosition.z, smoothT);

            transform.position = position;

            if (t >= 1f)
            {
                position.y = targetPosition.y;
                position.z = targetPosition.z;

                transform.position = position;

                isChangingLane = false;
            }
        }
        else
        {
            // if not changing lane, just follow the target position
            Vector3 position = transform.position;
            position.y = targetPosition.y;
            position.z = targetPosition.z;
            transform.position = position;
        }
    }

    void ChangeLane(int lane)
    {
        currentLane = lane;

        Transform hitPoint = lane == 0 ? laneFollow.leftHitPoint : laneFollow.rightHitPoint;

        if (hitPoint == null)
        {
            Debug.LogError("当前车道 HitPoint 未设置。", this);
            return;
        }

        startPosition = transform.position;

        targetPosition = new Vector3(
            transform.position.x,
            hitPoint.position.y,
            hitPoint.position.z
        );

        laneFollow.SetLaneFollow(true);

        laneChangeTimer = 0f;
        isChangingLane = true;
    }
}