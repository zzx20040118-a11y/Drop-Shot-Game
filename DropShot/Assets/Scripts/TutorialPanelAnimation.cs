using System.Collections;
using UnityEngine;

public class TutorialPanelAnimation : MonoBehaviour
{
    [Header("Pop-up Animation / 弹出动画")]
    public float duration = 0.35f;
    public float startScale = 0.7f;
    public float overshootScale = 1.2f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (canvasGroup == null || rectTransform == null)
            return;

        canvasGroup.alpha = 0;
        rectTransform.localScale = Vector3.one * startScale;

        animationCoroutine = StartCoroutine(OpenAnimation());
    }

    public void ClosePanel()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(CloseAnimation());
    }

    private IEnumerator OpenAnimation()
    {
        float halfDuration = duration * 0.65f;
        float time = 0;

        // 第一阶段：0.7 → 1.2
        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / halfDuration);
            t = 1 - Mathf.Pow(1 - t, 3);

            float scale = Mathf.Lerp(startScale, overshootScale, t);

            rectTransform.localScale = Vector3.one * scale;
            canvasGroup.alpha = t;

            yield return null;
        }

        time = 0;
        float secondDuration = duration * 0.35f;

        // 第二阶段：1.2 → 1.0
        while (time < secondDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / secondDuration);
            t = 1 - Mathf.Pow(1 - t, 3);

            float scale = Mathf.Lerp(overshootScale, 1f, t);

            rectTransform.localScale = Vector3.one * scale;
            canvasGroup.alpha = 1;

            yield return null;
        }

        rectTransform.localScale = Vector3.one;
        canvasGroup.alpha = 1;
    }

    private IEnumerator CloseAnimation()
    {
        float time = 0;
        float closeDuration = 0.15f;

        Vector3 start = rectTransform.localScale;

        while (time < closeDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / closeDuration);
            t = t * t;

            rectTransform.localScale =
                Vector3.Lerp(start, Vector3.one * 0.8f, t);

            canvasGroup.alpha = 1 - t;

            yield return null;
        }

        canvasGroup.alpha = 0;
        rectTransform.localScale = Vector3.one * startScale;

        gameObject.SetActive(false);
    }
}
