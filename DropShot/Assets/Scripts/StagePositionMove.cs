using UnityEngine;

public class StagePositionMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   
      public Vector3 moveSpeed = new Vector3(-0.2f, 0f, 0f); 
    

    // Update is called once per frame
    void Update()
    {
       transform.localPosition += moveSpeed * Time.deltaTime; 
    }
}
