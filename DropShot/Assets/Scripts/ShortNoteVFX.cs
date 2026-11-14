using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShortNoteVFX : MonoBehaviour
{
    public Renderer candy;
    public SpriteRenderer burstPrefab;
    public Animator animatorSlot;

    [Min(0.01f)] public float previewTime = 0.6f;
    public float shakeAmount = 0.015f;

    SpriteRenderer burst;
    Camera cam;
    MaterialPropertyBlock properties;
    Color originalOutline;
    Color burstColor;
    float progress;
    float shake;

    public void Initialize()
    {
        if (candy == null)
            candy = GetComponent<Renderer>();
        if (candy == null || candy.sharedMaterial == null)
            return;

        cam = Camera.main;
        properties = new MaterialPropertyBlock();
        originalOutline = candy.sharedMaterial.GetColor("_RimColor");

        if (burstPrefab != null)
        {
            burst = Instantiate(burstPrefab);
            burstColor = burst.color;
            burst.gameObject.SetActive(false);
        }
    }

    public void UpdateVisual(double songTime, double hitTime, float goodWindow)
    {
        float remaining = (float)(hitTime - songTime);
        progress = Mathf.Clamp01(1f - remaining / Mathf.Max(previewTime, 0.01f));
        shake = Mathf.Abs(remaining) <= goodWindow
            ? Mathf.Sin((float)songTime * 150f) * shakeAmount : 0f;
    }

    void LateUpdate()
    {
        if (properties == null || candy == null || cam == null)
            return;

        Vector3 offset = candy.transform.InverseTransformVector(cam.transform.right * shake);
        candy.GetPropertyBlock(properties);
        properties.SetColor("_RimColor", Color.Lerp(originalOutline, Color.white, progress));
        properties.SetVector("_NoteVisualOffset", new Vector4(offset.x, offset.y, offset.z, 0f));
        candy.SetPropertyBlock(properties);

        if (burst != null)
        {
            burst.gameObject.SetActive(progress > 0f);
            Color color = burstColor;
            color.a *= progress;
            burst.color = color;
            burst.transform.position = candy.bounds.center;
            burst.transform.rotation = cam.transform.rotation;
        }
    }

    public void Finish()
    {
        ResetVisual();
        enabled = false;
    }

    void ResetVisual()
    {
        if (candy != null && properties != null)
        {
            candy.GetPropertyBlock(properties);
            properties.SetColor("_RimColor", originalOutline);
            properties.SetVector("_NoteVisualOffset", Vector4.zero);
            candy.SetPropertyBlock(properties);
        }
        if (burst != null)
            burst.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        ResetVisual();
    }

    void OnDestroy()
    {
        if (burst != null)
            Destroy(burst.gameObject);
    }
}
