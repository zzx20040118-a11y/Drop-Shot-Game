using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MenuButtonHitArea : MonoBehaviour, ICanvasRaycastFilter
{
    public Vector2 hitSize = new Vector2(400f, 150f);
    RectTransform area;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!isActiveAndEnabled) return true;
        if (area == null) area = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, eventCamera, out Vector2 local))
            return false;
        Vector2 delta = local - area.rect.center;
        return Mathf.Abs(delta.x) <= hitSize.x * 0.5f && Mathf.Abs(delta.y) <= hitSize.y * 0.5f;
    }
}
