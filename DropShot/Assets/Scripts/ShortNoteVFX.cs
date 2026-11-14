using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShortNoteVFX : MonoBehaviour
{
    public enum ShakeTarget { Candy, Burst }

    public Renderer candy;
    public SpriteRenderer burstPrefab;
    public Animator animatorSlot;
    public bool enableOutlineGlow = false;
    [Range(0.005f, 0.3f)] public float ringWidth = 0.05f;

    [Min(0.01f)] public float burstScale = 1f;
    [Range(0f, 1f)] public float burstEndScale = 0f;
    [Min(0.01f)] public float previewTime = 0.6f;
    [Min(0f)] public float outlineBrightness = 1f;
    [Min(0f)] public float shakePreviewTime = 0.5f;
    public float shakeAmount = 0.015f;
    public ShakeTarget shakeTarget = ShakeTarget.Burst;

    SpriteRenderer burst;
    Camera cam;
    MaterialPropertyBlock properties;
    MaterialPropertyBlock ringProperties;
    Color originalOutline;
    Color burstColor;
    Vector3 burstBaseScale;
    float progress;
    float shake;
    bool ringSized;
    bool isRing;

    public void Initialize()
    {
        if (candy == null)
            candy = GetComponent<Renderer>();
        if (candy == null || candy.sharedMaterial == null)
            return;

        cam = Camera.main;
        properties = new MaterialPropertyBlock();
        ringProperties = new MaterialPropertyBlock();
        originalOutline = candy.sharedMaterial.GetColor("_RimColor");

        if (burstPrefab != null)
        {
            burst = Instantiate(burstPrefab);
            burstBaseScale = burst.transform.localScale * Mathf.Max(0.01f, burstScale);
            burst.transform.localScale = burstBaseScale;
            burstColor = burst.color;
            isRing = burst.sharedMaterial != null && burst.sharedMaterial.HasProperty("_RingWidth");
            burst.gameObject.SetActive(false);
        }
    }

    public void UpdateVisual(double songTime, double hitTime, float goodWindow)
    {
        float remaining = (float)(hitTime - songTime);
        progress = Mathf.Clamp01(1f - remaining / Mathf.Max(previewTime, 0.01f));
        shake = remaining <= Mathf.Max(0f, shakePreviewTime) && remaining >= -goodWindow
            ? Mathf.Sin((float)songTime * 150f) * shakeAmount : 0f;
    }

    void LateUpdate()
    {
        if (properties == null || candy == null || cam == null)
            return;

        Vector3 worldOffset = cam.transform.right * shake;
        Vector3 offset = shakeTarget == ShakeTarget.Candy
            ? candy.transform.InverseTransformVector(worldOffset) : Vector3.zero;
        candy.GetPropertyBlock(properties);
        Color outline = enableOutlineGlow
            ? Color.Lerp(originalOutline, Color.white * Mathf.Max(0f, outlineBrightness), progress)
            : originalOutline;
        outline.a = originalOutline.a;
        properties.SetColor("_RimColor", outline);
        properties.SetVector("_NoteVisualOffset", new Vector4(offset.x, offset.y, offset.z, 0f));
        candy.SetPropertyBlock(properties);

        if (burst != null)
        {
            if (isRing)
                SetRingWidth(burst);
            if (isRing && !ringSized && burst.sprite != null)
            {
                Vector3 e = candy.bounds.extents;
                Vector3 right = cam.transform.right;
                Vector3 up = cam.transform.up;
                float width = Mathf.Abs(right.x) * e.x + Mathf.Abs(right.y) * e.y + Mathf.Abs(right.z) * e.z;
                float height = Mathf.Abs(up.x) * e.x + Mathf.Abs(up.y) * e.y + Mathf.Abs(up.z) * e.z;
                float diameter = Mathf.Max(width, height) * 2.3f;
                burstBaseScale = Vector3.one * diameter * 1.8f
                    / Mathf.Max(0.01f, burst.sprite.bounds.size.x) * Mathf.Max(0.01f, burstScale);
                ringSized = true;
            }
            burst.gameObject.SetActive(progress > 0f);
            burst.transform.localScale = burstBaseScale
                * Mathf.Lerp(1f, Mathf.Clamp01(burstEndScale), progress);
            Color color = isRing ? Color.Lerp(Color.white, burstColor, progress) : burstColor;
            color.a = burstColor.a * (isRing ? 1f : progress);
            burst.color = color;
            burst.transform.position = candy.bounds.center
                + (shakeTarget == ShakeTarget.Burst ? worldOffset : Vector3.zero);
            burst.transform.rotation = cam.transform.rotation;
        }
    }

    public void Finish()
    {
        ResetVisual();
        enabled = false;
    }

    void SetRingWidth(SpriteRenderer ring)
    {
        ring.GetPropertyBlock(ringProperties);
        ringProperties.SetFloat("_RingWidth", Mathf.Clamp(ringWidth, 0.005f, 0.3f));
        ring.SetPropertyBlock(ringProperties);
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
