using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropShot.UI
{
    // 甜甜圈加载条：面包圈底 + 糖霜按进度环绕填满，糖针随进度一颗颗冒出来；完成时弹一下炸开
    public class DonutLoader : MonoBehaviour
    {
        public RectTransform body;          // 整个甜甜圈（缩放、旋转用）
        public Image icingFill;             // Image Type = Filled / Radial 360
        public RectTransform sprinkleRoot;  // 糖针挂在这里
        public Sprite sprinkleSprite;
        public TMP_Text percentText;
        public TMP_Text label;
        public SprinkleBurst burst;

        public int sprinkleCount = 14;
        public float sprinkleRadius = 118f;
        public Color[] sprinkleColors =
        {
            new Color32(0xF2, 0xA7, 0xB5, 0xFF), new Color32(0x8F, 0xA9, 0xD9, 0xFF),
            new Color32(0xF5, 0xD2, 0x7A, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            new Color32(0xB9, 0xA3, 0xD9, 0xFF),
        };

        RectTransform[] sprinkles;
        bool[] shown;
        float progress;
        string labelBase;
        bool completed;

        void Awake()
        {
            labelBase = label != null ? label.text : "LOADING";
            sprinkles = new RectTransform[sprinkleCount];
            shown = new bool[sprinkleCount];
            for (int i = 0; i < sprinkleCount; i++)
            {
                // 顺时针从 12 点钟方向排布，和 Radial 填充方向一致
                float frac = (i + 0.5f) / sprinkleCount;
                float a = Mathf.PI / 2f - frac * Mathf.PI * 2f;
                float r = sprinkleRadius + ((i % 2 == 0) ? 10f : -12f);
                var go = new GameObject("Sprinkle" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(sprinkleRoot, false);
                rt.sizeDelta = new Vector2(30f, 11f);
                rt.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 180f));
                rt.localScale = Vector3.zero;
                var img = go.GetComponent<Image>();
                img.sprite = sprinkleSprite;
                img.color = sprinkleColors[i % sprinkleColors.Length];
                img.raycastTarget = false;
                sprinkles[i] = rt;
            }
            SetProgress(0f);
        }

        void Update()
        {
            if (completed || body == null) return;
            // 待机：轻轻摇摆 + 上下浮动
            float t = Time.unscaledTime;
            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.1f) * 4f);
            body.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 3.4f) * 6f);
            if (label != null) label.text = labelBase + new string('.', 1 + (int)(t * 3f) % 3);
        }

        public void SetProgress(float p)
        {
            progress = Mathf.Clamp01(p);
            if (icingFill != null) icingFill.fillAmount = progress;
            if (percentText != null) percentText.text = Mathf.FloorToInt(progress * 100f) + "%";
            for (int i = 0; i < sprinkleCount; i++)
            {
                if (!shown[i] && progress >= (i + 0.5f) / sprinkleCount)
                {
                    shown[i] = true;
                    StartCoroutine(PopSprinkle(sprinkles[i]));
                }
            }
        }

        IEnumerator PopSprinkle(RectTransform rt)
        {
            yield return UITween.Run(0.3f, t => rt.localScale = Vector3.one * UITween.OutBack(t, 3f));
        }

        // 加载完成动画：糖霜闪一下 → 甜甜圈蓄力压扁 → 弹起放大 + 糖针炸开 → 缩小消失
        public IEnumerator PlayComplete()
        {
            completed = true;
            SetProgress(1f);
            if (label != null) label.text = "READY!";
            body.localRotation = Quaternion.identity;
            Vector3 one = Vector3.one;

            yield return UITween.Run(0.18f, t =>
            {
                float s = Mathf.Lerp(1f, 0.82f, UITween.OutCubic(t));
                body.localScale = new Vector3(1f / s, s, 1f);
            });
            if (burst != null) burst.Play();
            yield return UITween.Run(0.35f, t =>
            {
                float s = Mathf.LerpUnclamped(0.82f, 1.25f, UITween.OutBack(t, 2f));
                body.localScale = new Vector3(s, s, 1f);
                body.localRotation = Quaternion.Euler(0, 0, -360f * UITween.OutCubic(t));
            });
            if (label != null) StartCoroutine(UITween.Jelly(label.transform, one, 0.25f, 0.5f));
            yield return UITween.Wait(0.25f);
            yield return UITween.Run(0.3f, t =>
            {
                float s = Mathf.Lerp(1.25f, 0f, UITween.InCubic(t));
                body.localScale = new Vector3(s, s, 1f);
                if (percentText != null) percentText.alpha = 1f - t;
                if (label != null) label.alpha = 1f - t;
            });
            body.gameObject.SetActive(false);
        }
    }
}
