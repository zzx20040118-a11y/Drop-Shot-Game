using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class TrailHolePositions : MonoBehaviour
{
    public Camera targetCamera;
    public Transform ball;
    [Min(0f)] public float sideOffset = 0.035f;
    [Min(0f)] public float downOffset = 0.08f;

    static readonly int Left = Shader.PropertyToID("_HoleLeft");
    static readonly int Right = Shader.PropertyToID("_HoleRight");
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
        float y = screen.y - downOffset;
        foreach (Renderer renderer in trails)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetVector(Left, new Vector4(x - sideOffset, y, 0, 0));
            properties.SetVector(Right, new Vector4(x + sideOffset, y, 0, 0));
            renderer.SetPropertyBlock(properties);
        }
    }
}
