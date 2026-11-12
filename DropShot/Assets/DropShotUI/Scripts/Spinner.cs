using UnityEngine;

namespace DropShot.UI
{
    // 匀速旋转（标题页的巧克力螺旋），可选跟着心跳轻微缩放
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = -12f;
        [Tooltip("每分钟脉动次数，0 = 不脉动；默认对齐 Gameotoon 的 122 BPM")]
        public float pulseBpm = 122f;
        public float pulseAmount = 0.025f;

        void Update()
        {
            float t = Time.unscaledTime;
            transform.localRotation = Quaternion.Euler(0, 0, t * degreesPerSecond);
            if (pulseBpm > 0f)
            {
                float beat = t * pulseBpm / 60f;
                float k = Mathf.Pow(1f - (beat - Mathf.Floor(beat)), 4f);
                transform.localScale = Vector3.one * (1f + pulseAmount * k);
            }
        }
    }
}
