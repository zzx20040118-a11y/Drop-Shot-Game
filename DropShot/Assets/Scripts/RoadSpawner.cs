using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    public GameObject roadPrefab;
    public float interval = 22.44136f;

    private float timer;
    private GameObject lastRoad;

    void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            Spawn();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        while (timer >= interval)
        {
            timer -= interval;
            Spawn();
        }
    }

    void Spawn()
    {
        Vector3 position = transform.position;

        if (lastRoad != null)
        {
            Bounds bounds = lastRoad.GetComponent<Renderer>().bounds;

            position = lastRoad.transform.position;
            position.x -= bounds.size.x;
        }

        lastRoad = Instantiate(roadPrefab, position, roadPrefab.transform.rotation);

        Debug.Log($"生成管道：時間 {Time.time:F1} 秒，位置 {lastRoad.transform.position}");
    }
}
