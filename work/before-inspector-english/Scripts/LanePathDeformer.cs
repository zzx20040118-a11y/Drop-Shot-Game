
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LanePathDeformer : MonoBehaviour
{
    [Header("轨道交叉")]
    public float crossAmplitude = 3f;
    public float crossDuration = 3f;
    public float crossInterval = 8f;

    [Header("轨道弯曲")]
    public float swayAmplitude = 1.5f;
    public float swayFrequency = 0.08f;

    [Header("轨道编号")]
    [Range(0, 1)]
    public int laneIndex = 0;

    private Mesh originalMesh;
    private Mesh deformMesh;

    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    private float timer;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        originalMesh = meshFilter.sharedMesh;
        deformMesh = Instantiate(originalMesh);
        meshFilter.mesh = deformMesh;

        originalVertices = originalMesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];

        timer = Random.Range(0f, crossInterval);
    }

    void Update()
    {
        timer += Time.deltaTime;

        float crossT = Mathf.Clamp01(timer / crossDuration);

        float crossOffset =
            Mathf.Sin(crossT * Mathf.PI) * crossAmplitude;

        if (timer >= crossInterval + crossDuration)
        {
            timer = 0f;
        }

        float laneSign = laneIndex == 0 ? 1f : -1f;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            float longitudinal = vertex.y;

            float sway =
                Mathf.Sin(longitudinal * swayFrequency + Time.time)
                * swayAmplitude;

            vertex.x += laneSign * (sway + crossOffset);

            deformedVertices[i] = vertex;
        }

        deformMesh.vertices = deformedVertices;
        deformMesh.RecalculateNormals();
        deformMesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        if (deformMesh != null)
        {
            Destroy(deformMesh);
        }
    }
}