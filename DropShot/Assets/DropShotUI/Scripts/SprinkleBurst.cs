using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DropShot.UI
{
    // 彩色糖针飞散：加载完成、评级盖章时用。挂在要喷发的位置（UI 物体）上
    public class SprinkleBurst : MonoBehaviour
    {
        public Sprite sprinkleSprite;
        public Color[] colors =
        {
            new Color32(0xF2, 0xA7, 0xB5, 0xFF), new Color32(0x8F, 0xA9, 0xD9, 0xFF),
            new Color32(0xF5, 0xD2, 0x7A, 0xFF), new Color32(0x8C, 0xC9, 0xA0, 0xFF),
            new Color32(0xB9, 0xA3, 0xD9, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0xFF),
        };
        public int count = 18;
        public float speed = 900f;
        public float gravity = 1800f;
        public float lifetime = 0.9f;
        public Vector2 size = new Vector2(34f, 12f);

        public void Play() => StartCoroutine(PlayRoutine());

        IEnumerator PlayRoutine()
        {
            var pieces = new RectTransform[count];
            var vel = new Vector2[count];
            var spin = new float[count];
            var images = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Sprinkle", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.sizeDelta = size;
                var img = go.GetComponent<Image>();
                img.sprite = sprinkleSprite;
                img.color = colors[i % colors.Length];
                img.raycastTarget = false;
                float a = (i / (float)count + Random.Range(-0.03f, 0.03f)) * Mathf.PI * 2f;
                vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.6f, 1.1f) + Vector2.up * 250f;
                spin[i] = Random.Range(-720f, 720f);
                rt.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                pieces[i] = rt;
                images[i] = img;
            }

            float t = 0f;
            while (t < lifetime)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float k = t / lifetime;
                for (int i = 0; i < count; i++)
                {
                    vel[i] += Vector2.down * gravity * dt;
                    vel[i] *= 1f - 1.8f * dt;
                    pieces[i].anchoredPosition += vel[i] * dt;
                    pieces[i].Rotate(0, 0, spin[i] * dt);
                    var c = images[i].color;
                    c.a = 1f - k * k;
                    images[i].color = c;
                }
                yield return null;
            }
            foreach (var p in pieces) Destroy(p.gameObject);
        }
    }
}
