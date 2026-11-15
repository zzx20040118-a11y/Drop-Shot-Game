using UnityEngine;
using DropShot.Judgement;

[DefaultExecutionOrder(100)]
public class LongNoteVFX : MonoBehaviour
{
    public SpriteRenderer ringPrefab;
    [Min(0.01f)] public float previewTime = 1.2f;
    [Min(0.01f)] public float releasePreviewTime = 0.6f;
    [Min(0.01f)] public float ringScale = 1f;
    [Min(1f)] public float approachScale = 1.8f;
    [Range(0.005f, 0.3f)] public float ringWidth = 0.1f;
    [Min(0.01f)] public float resultFadeTime = 0.15f;

    JudgementSystem judge;
    ChartNote note;
    Transform hitPoint;
    Renderer headRenderer;
    Camera cam;
    SpriteRenderer cueRing;
    SpriteRenderer holdRing;
    MaterialPropertyBlock properties;
    Color gold;
    Vector3 baseScale;
    float speed;
    float fadeTime;
    bool sized;
    bool holding;
    bool finishing;
    bool success;

    public void Initialize(JudgementSystem system, ChartNote chartNote,
        Transform point, float approachSpeed, Renderer visualHead)
    {
        judge = system;
        note = chartNote;
        hitPoint = point;
        speed = approachSpeed;
        headRenderer = visualHead;
        cam = Camera.main;
        properties = new MaterialPropertyBlock();
        if (ringPrefab == null)
            return;

        cueRing = Instantiate(ringPrefab);
        holdRing = Instantiate(ringPrefab);
        gold = ringPrefab.color;
        cueRing.gameObject.SetActive(false);
        holdRing.gameObject.SetActive(false);
    }

    public void BeginHold()
    {
        holding = true;
    }

    public void Finish(bool wasSuccessful)
    {
        if (finishing)
            return;
        finishing = true;
        success = wasSuccessful;
        fadeTime = 0f;
        if (cueRing != null)
            cueRing.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (judge == null || note == null || hitPoint == null || cam == null
            || cueRing == null || holdRing == null)
            return;
        if (!judge.Running && !finishing)
            return;

        if (!sized)
        {
            float diameter = 1.4f;
            if (headRenderer != null)
            {
                Vector3 e = headRenderer.bounds.extents;
                Vector3 right = cam.transform.right;
                Vector3 up = cam.transform.up;
                float width = Mathf.Abs(right.x) * e.x + Mathf.Abs(right.y) * e.y + Mathf.Abs(right.z) * e.z;
                float height = Mathf.Abs(up.x) * e.x + Mathf.Abs(up.y) * e.y + Mathf.Abs(up.z) * e.z;
                diameter = Mathf.Max(width, height) * 2.3f;
            }
            float spriteSize = cueRing.sprite != null ? cueRing.sprite.bounds.size.x : 1f;
            baseScale = Vector3.one * Mathf.Max(0.01f, diameter)
                / Mathf.Max(0.01f, spriteSize) * Mathf.Max(0.01f, ringScale);
            sized = true;
        }

        double time = judge.SongTime;
        Vector3 position = holding ? hitPoint.position
            : hitPoint.position - Vector3.right * (float)(note.time - time) * speed;

        if (finishing)
        {
            fadeTime += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(fadeTime / Mathf.Max(0.01f, resultFadeTime));
            Color color = success ? Color.white : Color.gray;
            color.a = alpha * (success ? 1f : 0.35f);
            DrawRing(holdRing, alpha > 0f, position, 1f, color);
            if (alpha <= 0f)
                enabled = false;
            return;
        }

        if (!holding)
        {
            float progress = Mathf.Clamp01(1f - (float)(note.time - time) / Mathf.Max(0.01f, previewTime));
            DrawRing(cueRing, progress > 0f, position,
                Mathf.Lerp(Mathf.Max(1f, approachScale), 1f, progress), Color.Lerp(Color.white, gold, progress));
            holdRing.gameObject.SetActive(false);
        }
        else
        {
            DrawRing(holdRing, true, position, 1f, gold);
            float leadTime = Mathf.Min(Mathf.Max(0.01f, releasePreviewTime), Mathf.Max(0.01f, note.duration));
            float progress = Mathf.Clamp01(1f - (float)(note.time + note.duration - time) / leadTime);
            DrawRing(cueRing, progress > 0f, position,
                Mathf.Lerp(Mathf.Max(1f, approachScale), 1f, progress), Color.Lerp(Color.white, gold, progress));
        }
    }

    void DrawRing(SpriteRenderer ring, bool visible, Vector3 position, float scale, Color color)
    {
        ring.gameObject.SetActive(visible);
        if (!visible)
            return;
        ring.transform.position = position;
        ring.transform.rotation = cam.transform.rotation;
        ring.transform.localScale = baseScale * scale;
        ring.color = color;
        ring.GetPropertyBlock(properties);
        properties.SetFloat("_RingWidth", Mathf.Clamp(ringWidth, 0.005f, 0.3f));
        ring.SetPropertyBlock(properties);
    }

    void OnDisable()
    {
        if (cueRing != null) cueRing.gameObject.SetActive(false);
        if (holdRing != null) holdRing.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (cueRing != null) Destroy(cueRing.gameObject);
        if (holdRing != null) Destroy(holdRing.gameObject);
    }
}
