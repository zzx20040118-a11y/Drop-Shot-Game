using UnityEngine;
using DropShot.Judgement;

public class BallLaneFollow : MonoBehaviour
{
    [Header("轨道参考点")]
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    [Header("轨道跟随")]
    public bool followLane = true;

    private int currentLane = 0;

    private BallCameraFollow ballCamera;

    void Start()
    {
        ballCamera = FindObjectOfType<BallCameraFollow>();

        if (leftHitPoint == null || rightHitPoint == null)
        {
            Debug.LogError("请指定球的左右 HitPoint。", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (KeyInput.Down(KeyCode.D))
            currentLane = 0;

        if (KeyInput.Down(KeyCode.F))
            currentLane = 1;
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
