using UnityEngine;

public class StagePositionMove : MonoBehaviour
{
    [Header("移动速度")]
    public Vector3 moveSpeed = new Vector3(-0.2f, 0f, 0f);

    [Header("移动状态")]
    public bool canMove = false;

    void Update()
    {
        if (!canMove)
            return;

        transform.localPosition += moveSpeed * Time.deltaTime;
    }

    public void StartMoving()
    {
        canMove = true;
    }

    public void StopMoving()
    {
        canMove = false;
    }
}
