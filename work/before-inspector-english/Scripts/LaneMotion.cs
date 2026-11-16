using UnityEngine;

public class LaneMotion : MonoBehaviour
{
    [Header("轨道运动")]
    public bool rotateLanes = true;

    [Tooltip("最大旋转角度")]
    [Range(0f, 90f)]
    public float maxAngle = 35f;

    [Tooltip("最小旋转角度")]
    [Range(-90f, 0f)]
    public float minAngle = -20f;

    [Header("随机速度")]
    public Vector2 speedRange = new Vector2(8f, 25f);

    [Header("随机运动时间")]
    public Vector2 moveTimeRange = new Vector2(1.5f, 4f);

    [Header("随机停顿时间")]
    public Vector2 pauseTimeRange = new Vector2(0.5f, 2f);

    [Header("管道中心")]
    public Vector3 axisCenter = Vector3.zero;

    private float currentAngle;
    private float targetAngle;
    private float currentSpeed;

    private float timer;
    private float duration;

    private bool isMoving;

    public float Angle => currentAngle;

    void Start()
    {
        currentAngle = 0f;
        StartMoving();
    }

    void Update()
    {
        if (!rotateLanes)
            return;

        timer += Time.deltaTime;

        if (isMoving)
        {
            currentAngle = Mathf.MoveTowards(
                currentAngle,
                targetAngle,
                currentSpeed * Time.deltaTime
            );
        }

        if (timer >= duration)
        {
            timer = 0f;

            if (isMoving)
            {
                isMoving = false;

                duration = Random.Range(
                    pauseTimeRange.x,
                    pauseTimeRange.y
                );
            }
            else
            {
                StartMoving();
            }
        }
    }

    void StartMoving()
    {
        isMoving = true;

        targetAngle = Random.Range(minAngle, maxAngle);

        currentSpeed = Random.Range(
            speedRange.x,
            speedRange.y
        );

        duration = Random.Range(
            moveTimeRange.x,
            moveTimeRange.y
        );
    }
}
