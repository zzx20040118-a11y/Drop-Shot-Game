using UnityEngine;

public class PerfectHitEffect : MonoBehaviour
{
    [Range(0f, 1f)] public float opacity = 0.55f;
    [Min(0f)] public float fadeStart = 0.08f;
    [Min(0.01f)] public float fadeDuration = 0.22f;

    private SpriteRenderer mainSprite;
    private Color originalColor;
    private float elapsed;

    void Start()
    {
        mainSprite = GetComponent<SpriteRenderer>();
        originalColor = mainSprite.color;
        Color color = originalColor;
        color.a *= opacity;
        mainSprite.color = color;
    }

    void LateUpdate()
    {
        elapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01((elapsed - fadeStart) / Mathf.Max(0.01f, fadeDuration));
        Color color = originalColor;
        color.a *= opacity * fade;
        mainSprite.color = color;
        if (fade <= 0f)
            Destroy(gameObject);
    }
}
