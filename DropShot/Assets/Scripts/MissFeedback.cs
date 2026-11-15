using DropShot.Judgement;
using UnityEngine;
using UnityEngine.UI;

public class MissFeedback : MonoBehaviour
{
    public JudgementSystem judge;
    public Renderer[] ballRenderers;
    public bool screenFeedback = true;
    public bool ballFeedback = true;
    [Min(0.01f)] public float fadeSeconds = 0.24f;
    public Color missColor = new Color(0.72f, 0.16f, 0.35f);
    [Range(0f, 1f)] public float screenOpacity = 0.22f;
    [Range(0.01f, 0.3f)] public float edgeWidth = 0.1f;
    [Range(0f, 1f)] public float ballTintStrength = 0.65f;

    MissEdgeGraphic edge;
    GameObject overlay;
    MaterialPropertyBlock[] originalBlocks;
    MaterialPropertyBlock properties;
    Color[] originalColors;
    float remaining;
    bool flashing;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (ballRenderers == null) ballRenderers = new Renderer[0];
        originalBlocks = new MaterialPropertyBlock[ballRenderers.Length];
        originalColors = new Color[ballRenderers.Length];
        properties = new MaterialPropertyBlock();
        for (int i = 0; i < ballRenderers.Length; i++)
            originalBlocks[i] = new MaterialPropertyBlock();

        overlay = new GameObject("Miss Screen Edge", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9;
        GameObject graphic = new GameObject("Edge Tint", typeof(RectTransform), typeof(CanvasRenderer));
        graphic.transform.SetParent(overlay.transform, false);
        RectTransform rect = graphic.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        edge = graphic.AddComponent<MissEdgeGraphic>();
        edge.raycastTarget = false;
        edge.enabled = false;
    }

    void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    [ContextMenu("Preview Miss (Play Mode)")]
    void PreviewMiss()
    {
        if (Application.isPlaying)
            OnJudged(new JudgeEvent { result = JudgeResult.Miss });
    }

    void OnEnable()
    {
        if (judge != null) judge.Judged += OnJudged;
    }

    void OnDisable()
    {
        if (judge != null) judge.Judged -= OnJudged;
        RestoreBall();
        if (edge != null) edge.enabled = false;
        flashing = false;
        remaining = 0f;
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.result != JudgeResult.Miss) return;
        if (!flashing)
        {
            for (int i = 0; i < ballRenderers.Length; i++)
            {
                Renderer ball = ballRenderers[i];
                if (ball == null) continue;
                ball.GetPropertyBlock(originalBlocks[i]);
                Material material = ball.sharedMaterial;
                originalColors[i] = originalBlocks[i].HasColor(BaseColor)
                    ? originalBlocks[i].GetColor(BaseColor)
                    : material != null && material.HasProperty(BaseColor)
                        ? material.GetColor(BaseColor) : Color.white;
            }
        }
        remaining = Mathf.Max(0.01f, fadeSeconds);
        flashing = true;
        Draw(1f);
    }

    void Update()
    {
        if (!flashing) return;
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        Draw(remaining / Mathf.Max(0.01f, fadeSeconds));
        if (remaining > 0f) return;
        RestoreBall();
        edge.enabled = false;
        flashing = false;
    }

    void Draw(float strength)
    {
        edge.enabled = screenFeedback && strength > 0f;
        edge.width = edgeWidth;
        edge.SetVerticesDirty();
        Color color = missColor;
        color.a = screenOpacity * strength;
        edge.color = color;
        for (int i = 0; i < ballRenderers.Length; i++)
        {
            Renderer ball = ballRenderers[i];
            if (ball == null) continue;
            ball.GetPropertyBlock(properties);
            Color tint = Color.Lerp(originalColors[i], missColor,
                ballFeedback ? ballTintStrength * strength : 0f);
            tint.a = originalColors[i].a;
            properties.SetColor(BaseColor, tint);
            ball.SetPropertyBlock(properties);
        }
    }

    void RestoreBall()
    {
        if (!flashing || originalBlocks == null) return;
        for (int i = 0; i < ballRenderers.Length; i++)
            if (ballRenderers[i] != null)
                ballRenderers[i].SetPropertyBlock(originalBlocks[i]);
    }
}

