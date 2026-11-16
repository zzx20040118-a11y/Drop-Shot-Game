using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject roadPrefabB;
    public int initialSegmentCount = 4;

    private GameObject lastRoad;
    private float replenishPositionX;
    private bool spawnA = true;

    void Start()
    {
        if (initialSegmentCount < 1)
        {
            initialSegmentCount = 1;
        }

        for (int i = 0; i < initialSegmentCount; i++)
        {
            Spawn();
            if (!enabled) return;
        }

        float distance = lastRoad.transform.position.x -
            lastRoad.transform.Find("NextSpawnPoint").position.x;

        replenishPositionX = lastRoad.transform.position.x + distance;
    }

    void Update()
    {
        if (lastRoad == null)
        {
            return;
        }

        if (lastRoad.transform.position.x >= replenishPositionX)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        GameObject prefab;

        if (spawnA)
        {
            prefab = roadPrefab;
        }
        else
        {
            prefab = roadPrefabB;
        }

        Vector3 position = transform.position;

        if (lastRoad != null)
        {
            position = lastRoad.transform.Find("NextSpawnPoint").position;
            if (position.x >= lastRoad.transform.position.x - 0.01f)
            {
                Debug.LogError("NextSpawnPoint must be behind the road on world X.", lastRoad);
                enabled = false;
                return;
            }
        }

        lastRoad = Instantiate(prefab, position, prefab.transform.rotation);
        spawnA = !spawnA;
    }
}
