using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Scale Settings / 缩放设置")]
    public float hoverScale = 1.08f;
    public float pressedScale = 0.88f;
    public float animationSpeed = 18f;

    private RectTransform rectTransform;

    // 保存物体原始缩放，包括 -1 镜像
    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isPressed;
    private bool isHovering;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        if (!isPressed)
            SetScale(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        if (!isPressed)
            SetScale(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        SetScale(pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        SetScale(isHovering ? hoverScale : 1f);
    }

    private void SetScale(float multiplier)
    {
        targetScale = originalScale * multiplier;
    }
}
