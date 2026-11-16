using UnityEngine;

[DefaultExecutionOrder(200)]
public class BallJumpPopVFX : MonoBehaviour
{
    public BallStrikeFlight flight;
    public Material particleMaterial;
    [Header("Rolling Ball Trail (disable for the flying ball) / 普通滚球（飞行球不要勾选）")]
    public bool rollingTrail;
    [Min(0f)] public float rollingTrailSpeed = 6f;
    [Min(0.02f)] public float trailInterval = 0.065f;
    [Min(0.01f)] public float trailLifetime = 0.24f;
    [Min(0.01f)] public float dotSize = 0.18f;
    [Min(0f)] public float burstSize = 0.7f;
    public Color fillColor = new Color(1f, 0.87f, 0.48f);
    public Color outlineColor = new Color(0.2f, 0.09f, 0.05f);

    ParticleSystem outlines;
    ParticleSystem fills;
    Camera cam;
    bool wasFlying;
    float elapsed;
    int marks;
    Vector3 previousPosition;

    void Awake()
    {
        if (flight == null) flight = GetComponent<BallStrikeFlight>();
        cam = flight != null && flight.ballCamera != null
            ? flight.ballCamera.GetComponent<Camera>() : Camera.main;
        if (flight == null || particleMaterial == null || cam == null)
        {
            enabled = false;
            return;
        }
        outlines = CreateParticles("Comic Outline", 1);
        fills = CreateParticles("Comic Fill", 2);
    }

    ParticleSystem CreateParticles(string label, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startSpeed = 0;
        main.startSize3D = true;
        main.maxParticles = 96;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = order;
        particles.Play();
        return particles;
    }

    void LateUpdate()
    {
        bool flying = flight != null && flight.enabled && (rollingTrail
            ? flight.currentState == BallStrikeFlight.BallState.Normal
                && flight.judgement != null && flight.judgement.Running
            : flight.currentState == BallStrikeFlight.BallState.Flight);
        if (!flying)
        {
            // Portal entry takes over by disabling flight: clear all marks immediately.
            if (wasFlying && (rollingTrail || flight == null || !flight.enabled)) Clear();
            wasFlying = false;
            return;
        }
        Vector3 position = transform.position;
        if (!wasFlying)
        {
            if (!rollingTrail) Burst(position - Vector3.up * 0.25f);
            elapsed = 0;
            marks = 0;
            previousPosition = position;
            wasFlying = true;
        }
        elapsed += Time.deltaTime;
        if (elapsed >= trailInterval)
        {
            Vector3 direction = position - previousPosition;
            Vector3 drift = rollingTrail ? Vector3.right * rollingTrailSpeed : Vector3.zero;
            if (rollingTrail) direction = -drift;
            float angle = Mathf.Atan2(Vector3.Dot(direction, cam.transform.up),
                Vector3.Dot(direction, cam.transform.right)) * Mathf.Rad2Deg;
            bool line = marks++ % 3 == 0;
            Vector2 size = line ? new Vector2(dotSize * 2.8f, dotSize * 0.6f) : Vector2.one * dotSize;
            // The rolling ball stays almost fixed while the environment passes it.
            // Give its marks backward velocity so they do not stack in one spot.
            Vector3 origin = rollingTrail
                ? position + Vector3.right * 0.35f + cam.transform.right * (marks % 2 == 0 ? 0.06f : -0.06f)
                : position;
            Emit(origin, drift, size, angle, trailLifetime);
            elapsed %= trailInterval;
        }
        previousPosition = position;
    }

    void Burst(Vector3 origin)
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f + 15f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = cam.transform.right * Mathf.Cos(radians) + cam.transform.up * Mathf.Sin(radians);
            Emit(origin + direction * burstSize * 0.35f, direction * burstSize,
                new Vector2(burstSize * 0.32f, burstSize * 0.09f), angle, 0.18f);
        }
    }

    void Emit(Vector3 position, Vector3 velocity, Vector2 size, float angle, float lifetime)
    {
        var particle = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize3D = new Vector3(size.x, size.y, 1),
            rotation = -angle,
            startLifetime = lifetime,
            startColor = outlineColor
        };
        outlines.Emit(particle, 1);
        particle.position -= cam.transform.forward * 0.005f;
        particle.startSize3D = new Vector3(size.x * 0.72f, size.y * 0.72f, 1);
        particle.startColor = fillColor;
        fills.Emit(particle, 1);
    }

    void Clear()
    {
        if (outlines != null) outlines.Clear(true);
        if (fills != null) fills.Clear(true);
    }

    void OnDisable()
    {
        Clear();
        wasFlying = false;
    }
}
