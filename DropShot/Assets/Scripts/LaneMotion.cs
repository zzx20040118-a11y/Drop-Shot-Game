using UnityEngine;
using DropShot.Judgement;

public class LaneMotion : MonoBehaviour
{
    public JudgementSystem judge;
    public bool rotateLanes = false;
    public float rotationSpeed = 20f;

    public Vector3 axisCenter = Vector3.zero;

    public float Angle
    {
        get
        {
            if (!rotateLanes || judge == null)
                return 0f;

            return (float)judge.SongTime * rotationSpeed;
        }
    }
}