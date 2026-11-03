using UnityEngine;

public class RoadMove : MonoBehaviour
{
    public float speed = 40f;
    public float deleteDistance = 20f;

    private Renderer roadRenderer;
    private Transform cameraTransform;

    void Start()
    {
        roadRenderer = GetComponent<Renderer>();
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        
        if (roadRenderer.bounds.min.x >
            cameraTransform.position.x + deleteDistance)
        {
            Destroy(gameObject);
        }
    }
}