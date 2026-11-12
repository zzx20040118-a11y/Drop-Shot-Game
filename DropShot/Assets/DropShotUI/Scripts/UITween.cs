using System;
using System.Collections;
using UnityEngine;

namespace DropShot.UI
{
    // 不依赖 DOTween 的小型缓动工具：所有界面动画都走这里，用 unscaled 时间，暂停也能动
    public static class UITween
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InCubic(float t) => t * t * t;
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        public static float OutBack(float t, float s = 1.70158f)
        {
            float c3 = s + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + s * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        // 掉落弹跳：Logo「Drop」进场用
        public static float OutBounce(float t)
        {
            const float n = 7.5625f, d = 2.75f;
            if (t < 1f / d) return n * t * t;
            if (t < 2f / d) return n * (t -= 1.5f / d) * t + 0.75f;
            if (t < 2.5f / d) return n * (t -= 2.25f / d) * t + 0.9375f;
            return n * (t -= 2.625f / d) * t + 0.984375f;
        }

        public static IEnumerator Run(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 果冻弹一下：x、y 反相衰减振动
        public static IEnumerator Jelly(Transform target, Vector3 baseScale, float strength = 0.18f, float duration = 0.5f, float freq = 3.2f)
        {
            yield return Run(duration, t =>
            {
                float decay = 1f - t;
                float w = Mathf.Sin(t * freq * Mathf.PI * 2f) * strength * decay * decay;
                target.localScale = new Vector3(baseScale.x * (1f + w), baseScale.y * (1f - w), baseScale.z);
            });
            target.localScale = baseScale;
        }
    }
}
