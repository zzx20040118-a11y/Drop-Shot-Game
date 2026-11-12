using UnityEngine;

namespace DropShot.UI
{
    // 背景小装饰上下漂浮、轻轻摇摆
    public class Bobber : MonoBehaviour
    {
        public float height = 14f;
        public float speed = 1.3f;
        public float tilt = 4f;
        Vector2 home;
        float phase;
        RectTransform rt;

        void Awake()
        {
            rt = (RectTransform)transform;
            home = rt.anchoredPosition;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.unscaledTime * speed + phase;
            rt.anchoredPosition = home + Vector2.up * Mathf.Sin(t) * height;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.7f) * tilt);
        }
    }
}
