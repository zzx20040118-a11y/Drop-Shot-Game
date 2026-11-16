using UnityEngine;

public class BallMeshRotation : MonoBehaviour
{
    [Header("Rotation Settings / 旋转设置")]
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Rotation Speed / 旋转速度")]
    public float rotationSpeed = 360f;

    void Update()
    {
        transform.Rotate(
            rotationAxis,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }
}
