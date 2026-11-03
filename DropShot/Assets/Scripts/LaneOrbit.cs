using UnityEngine;


[DefaultExecutionOrder(-100)]
public class LaneOrbit : MonoBehaviour
{
    private LaneMotion motion;
    private Vector3 originalOffset;

    void Start()
    {
        motion = FindFirstObjectByType<LaneMotion>();

        if (motion == null)
        {
            Debug.LogError("場景中找不到 LaneMotion。", this);
            enabled = false;
            return;
        }

        originalOffset = transform.position - motion.axisCenter;
        originalOffset.x = 0f;
    }

    void Update()
    {
        Quaternion rotation =
            Quaternion.AngleAxis(motion.Angle, Vector3.right);

        Vector3 position =
            motion.axisCenter + rotation * originalOffset;

       
        position.x = transform.position.x;

        transform.position = position;
    }
}