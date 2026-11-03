using UnityEngine;
using DropShot.Judgement;

public class BallLaneFollow : MonoBehaviour
{
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    private int currentLane = 0;

    void Start()
    {
        if (leftHitPoint == null || rightHitPoint == null)
        {
            Debug.LogError("請指定球的左右 HitPoint。", this);
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (KeyInput.Down(KeyCode.D))
            currentLane = 0;

        if (KeyInput.Down(KeyCode.F))
            currentLane = 1;

        Transform target =
            currentLane == 0 ? leftHitPoint : rightHitPoint;

        transform.position = target.position;
    }
}