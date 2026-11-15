using UnityEngine;

public class BallCometShell : MonoBehaviour
{
    public GameObject modelPrefab;
    public Material shellMaterial;
    public Renderer ballRenderer;
    public BallStrikeFlight flight;
    public bool rollingBall = true;
    [Range(0.5f, 2f)] public float size = 1.08f;
    public float turnSpeed = 12f;

    Transform shell;
    Vector3 previousPosition;
    Quaternion modelRotation;
    float fittedScale;

    void Start()
    {
        if (modelPrefab == null || shellMaterial == null || ballRenderer == null || flight == null)
            return;

        shell = new GameObject("Comet Shell (" + name + ")").transform;
        GameObject model = Instantiate(modelPrefab, shell, false);
        MeshFilter mesh = model.GetComponentInChildren<MeshFilter>();
        if (mesh == null || mesh.sharedMesh == null)
        {
            Destroy(shell.gameObject);
            return;
        }

        Bounds bounds = mesh.sharedMesh.bounds;
        Vector3 head = bounds.center;
        head.z = bounds.min.z + bounds.extents.x;
        Vector3 headInShell = shell.InverseTransformPoint(mesh.transform.TransformPoint(head));
        Vector3 tailInShell = shell.InverseTransformVector(mesh.transform.TransformVector(Vector3.forward));
        float modelRadius = shell.InverseTransformVector(
            mesh.transform.TransformVector(Vector3.right * bounds.extents.x)).magnitude;
        Vector3 ballExtents = ballRenderer.bounds.extents;
        float ballRadius = Mathf.Max(ballExtents.x, ballExtents.y, ballExtents.z);
        fittedScale = ballRadius / Mathf.Max(modelRadius, 0.0001f);
        model.transform.localPosition -= headInShell;
        modelRotation = Quaternion.FromToRotation(tailInShell.normalized, Vector3.forward);

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetFloat("_TailStart", head.z + bounds.extents.x * 0.5f);
        properties.SetFloat("_TailEnd", bounds.max.z);
        properties.SetFloat("_MeshRadius", bounds.extents.x);
        foreach (Renderer part in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = part.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = shellMaterial;
            part.sharedMaterials = materials;
            part.SetPropertyBlock(properties);
            part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            part.receiveShadows = false;
        }

        shell.rotation = Quaternion.LookRotation(Vector3.right) * modelRotation;
        previousPosition = transform.position;
        shell.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (shell == null) return;
        Vector3 movement = transform.position - previousPosition;
        previousPosition = transform.position;
        bool flying = flight.currentState == BallStrikeFlight.BallState.Flight;
        bool visible = flight.enabled && ballRenderer.enabled &&
            (rollingBall ? !flying : flying) &&
            flight.judgement != null && flight.judgement.Running;
        shell.gameObject.SetActive(visible);
        if (!visible) return;

        Vector3 direction = !rollingBall && movement.sqrMagnitude > 0.000001f
            ? -movement.normalized : Vector3.right;
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up) * modelRotation;
        shell.rotation = Quaternion.Slerp(shell.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        shell.position = ballRenderer.bounds.center;
        shell.localScale = Vector3.one * (fittedScale * size);
    }

    void OnDisable()
    {
        if (shell != null) shell.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (shell != null) Destroy(shell.gameObject);
    }
}
