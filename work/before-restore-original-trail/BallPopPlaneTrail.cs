using UnityEngine;

[DefaultExecutionOrder(200)]
public class BallPopPlaneTrail : MonoBehaviour
{
    public BallStrikeFlight flight;
    public bool rollingTrail;
    public Material bodyMaterial;
    public Material frontMaterial;
    [Min(0.01f)] public float width = 0.8f;
    [Min(0.01f)] public float length = 1.8f;
    [Min(0f)] public float rootOffset = 0.2f;

    Transform body;
    Transform front;
    Renderer bodyRenderer;
    Renderer frontRenderer;
    Camera cam;
    Vector3 previousPosition;
    Vector2 tailDirection = Vector2.down;
    bool wasVisible;

    void Awake()
    {
        if (flight == null) flight = GetComponent<BallStrikeFlight>();
        cam = flight != null && flight.ballCamera != null
            ? flight.ballCamera.GetComponent<Camera>() : Camera.main;
        if (flight == null || cam == null || bodyMaterial == null || frontMaterial == null)
        {
            enabled = false;
            return;
        }
        body = CreatePlane("Pop Trail Body", bodyMaterial, out bodyRenderer);
        front = CreatePlane("Pop Trail Front Connection", frontMaterial, out frontRenderer);
        previousPosition = transform.position;
        SetVisible(false);
    }

    Transform CreatePlane(string label, Material material, out Renderer renderer)
    {
        var plane = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
        plane.transform.SetParent(transform, false);
        plane.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        renderer = plane.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return plane.transform;
    }

    void LateUpdate()
    {
        bool visible = flight != null && flight.enabled && (rollingTrail
            ? flight.currentState == BallStrikeFlight.BallState.Normal
                && flight.judgement != null && flight.judgement.Running
            : flight.currentState == BallStrikeFlight.BallState.Flight);
        if (body == null) return;
        SetVisible(visible);
        if (visible)
        {
            Vector3 movement = transform.position - previousPosition;
            Vector3 behind = rollingTrail || !wasVisible || movement.sqrMagnitude < 0.000001f
                ? Vector3.right : -movement;
            Vector2 screenDirection = new Vector2(Vector3.Dot(behind, cam.transform.right),
                Vector3.Dot(behind, cam.transform.up));
            if (screenDirection.sqrMagnitude > 0.000001f)
                tailDirection = Vector2.Lerp(tailDirection, screenDirection.normalized,
                    1f - Mathf.Exp(-20f * Time.unscaledDeltaTime)).normalized;
            Vector3 tail = cam.transform.right * tailDirection.x + cam.transform.up * tailDirection.y;
            Vector3 position = transform.position + tail * (rootOffset + length * 0.5f);
            float angle = Mathf.Atan2(tailDirection.y, tailDirection.x) * Mathf.Rad2Deg + 90f;
            Quaternion rotation = cam.transform.rotation * Quaternion.Euler(0, 0, angle);
            Place(body, position, rotation);
            Place(front, position, rotation);
        }
        previousPosition = transform.position;
        wasVisible = visible;
    }

    void Place(Transform plane, Vector3 position, Quaternion rotation)
    {
        plane.SetPositionAndRotation(position, rotation);
        Vector3 parentScale = transform.lossyScale;
        plane.localScale = new Vector3(width / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            length / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
    }

    void SetVisible(bool value)
    {
        if (bodyRenderer != null) bodyRenderer.enabled = value;
        if (frontRenderer != null) frontRenderer.enabled = value;
    }

    void OnDisable() { SetVisible(false); wasVisible = false; }
}
