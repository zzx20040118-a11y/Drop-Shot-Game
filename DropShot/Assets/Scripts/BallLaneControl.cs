using UnityEngine;
using UnityEngine.InputSystem;

public class BallLaneControl : MonoBehaviour
{
    public float laneSpacing = 3f;
    public int currentLane = 0; 

    private Vector3 leftPosition;
    private Vector3 rightPosition;

    void Start()
    {
        
        leftPosition = transform.position;
        leftPosition.z -= laneSpacing / 2f;

        rightPosition = transform.position;
        rightPosition.z += laneSpacing / 2f;

        
        transform.position = leftPosition;
        currentLane = 0;
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            transform.position = leftPosition;
            currentLane = 0;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            transform.position = rightPosition;
            currentLane = 1;
        }
    }
}