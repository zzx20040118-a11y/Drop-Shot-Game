using UnityEngine;
using UnityEngine.InputSystem;

public class BallLaneControl : MonoBehaviour
{
    public float laneSpacing = 3f;
    public int currentLane = 0;

    public float laneChangeDuration = 0.25f;

    public BallLaneFollow laneFollow;

    private Vector3 targetPosition;
    private Vector3 startPosition;

    private float laneChangeTimer;
    private bool isChangingLane;

    void Start()
    {
        if (laneFollow == null)
        {
            laneFollow = GetComponent<BallLaneFollow>();
        }

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
        if (Keyboard.current == null || laneFollow == null)
        {
            return;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            ChangeLane(0);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ChangeLane(1);
        }

        if (isChangingLane)
        {
            laneChangeTimer += Time.deltaTime;

            float t = Mathf.Clamp01(
                laneChangeTimer / laneChangeDuration
            );

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = transform.position;

            position.y = Mathf.Lerp(
                startPosition.y,
                targetPosition.y,
                smoothT
            );

            position.z = Mathf.Lerp(
                startPosition.z,
                targetPosition.z,
                smoothT
            );

            transform.position = position;

            if (t >= 1f)
            {
                position.y = targetPosition.y;
                position.z = targetPosition.z;

                transform.position = position;

                isChangingLane = false;
            }
        }
    }

    void ChangeLane(int lane)
    {
        currentLane = lane;

        Transform hitPoint = lane == 0
            ? laneFollow.leftHitPoint
            : laneFollow.rightHitPoint;

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
