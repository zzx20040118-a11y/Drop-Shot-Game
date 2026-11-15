using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class TrailHolePositions : MonoBehaviour
{
    public Camera targetCamera;
    public Transform ball;
    [Min(0f)] public float sideOffset = 0.08f;
    [Min(0f)] public float downOffset = 0f;
    [Min(0.01f)] public float minimumTravel = 0.35f;

    static readonly int Left = Shader.PropertyToID("_HoleLeft");
    static readonly int Right = Shader.PropertyToID("_HoleRight");
    static readonly int Travel = Shader.PropertyToID("_HoleTravel");
    Renderer[] trails;
    MaterialPropertyBlock properties;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (ball == null) ball = transform;
        var targets = new List<Renderer>();
        foreach (ParticleSystemRenderer renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(Left))
                targets.Add(renderer);
        trails = targets.ToArray();
        properties = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (targetCamera == null || ball == null || trails == null) return;
        Vector3 screen = targetCamera.WorldToViewportPoint(ball.position);
        if (screen.z <= 0f) return;
        float x = screen.x * targetCamera.aspect;
        float radius = 0f;
        float bottom = screen.y - downOffset - minimumTravel;
        foreach (Renderer renderer in trails)
        {
            if (renderer == null) continue;
            radius = Mathf.Max(radius, renderer.sharedMaterial.GetFloat("_HoleSize") * 0.5f);
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds bounds = renderer.bounds;
            // Project the original trail's bounds instead of resetting halfway along it.
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1,
                    (corner & 4) == 0 ? -1 : 1));
                Vector3 point = targetCamera.WorldToViewportPoint(bounds.center + offset);
                if (point.z > targetCamera.nearClipPlane)
                    bottom = Mathf.Min(bottom, Mathf.Max(-1f, point.y));
            }
        }
        // Start above the head and finish below the tail, with a full radius of clearance.
        float y = screen.y - downOffset + radius + 0.01f;
        float travel = y - bottom + radius + 0.01f;
        foreach (Renderer renderer in trails)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetVector(Left, new Vector4(x - sideOffset, y, 0, 0));
            properties.SetVector(Right, new Vector4(x + sideOffset, y, 0, 0));
            properties.SetFloat(Travel, travel);
            renderer.SetPropertyBlock(properties);
        }
    }
}
