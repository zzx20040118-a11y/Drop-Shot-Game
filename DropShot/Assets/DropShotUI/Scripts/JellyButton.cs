using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DropShot.Judgement;

namespace DropShot.UI
{
    // 果冻按钮动效：待机呼吸、悬停放大倾斜、按下压扁下沉、松开果冻回弹，点击后再触发 onClick。
    // 结构：本物体（带 Image，负责点击区域，不动） → visual 子物体（真正缩放的按钮外观）
    public class JellyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform visual;
        [Tooltip("按钮下方的投影，按下时会被按钮盖住")]
        public RectTransform shadow;
        public KeyCode hotkey = KeyCode.None;
        public bool interactable = true;

        [Header("动效参数")]
        public float idleBreath = 0.025f;
        public float idleSpeed = 2.2f;
        public float hoverScale = 1.08f;
        public float hoverTilt = -3f;
        public Vector2 pressSquash = new Vector2(1.08f, 0.88f);
        public float pressSink = 8f;
        [Tooltip("点击后等果冻动画播一会儿再触发 onClick")]
        public float clickDelay = 0.22f;

        [Header("音效（可空）")]
        public AudioSource sfx;
        public AudioClip hoverClip;
        public AudioClip clickClip;

        public UnityEvent onClick = new UnityEvent();

        Vector2 visualBasePos, shadowBasePos;
        float hover, press;          // 0..1，平滑过渡
        bool hovering, pressing, busy;
        float idlePhase;

        void Awake()
        {
            if (visual == null) visual = (RectTransform)transform;
            visualBasePos = visual.anchoredPosition;
            if (shadow != null) shadowBasePos = shadow.anchoredPosition;
            idlePhase = Random.value * 10f;
        }

        void Update()
        {
            if (busy) return;
            float dt = Time.unscaledDeltaTime;
            hover = Mathf.MoveTowards(hover, hovering && interactable ? 1f : 0f, dt * 8f);
            press = Mathf.MoveTowards(press, pressing && interactable ? 1f : 0f, dt * 14f);

            float breath = 1f + Mathf.Sin((Time.unscaledTime + idlePhase) * idleSpeed) * idleBreath * (1f - hover);
            float s = Mathf.Lerp(1f, hoverScale, UITween.OutCubic(hover)) * breath;
            Vector2 squash = Vector2.Lerp(Vector2.one, pressSquash, press);
            visual.localScale = new Vector3(s * squash.x, s * squash.y, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, hoverTilt * hover * (1f - press));
            visual.anchoredPosition = visualBasePos + Vector2.down * pressSink * press;

            if (interactable && hotkey != KeyCode.None && KeyInput.Down(hotkey)) Click();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hovering = true;
            if (interactable && sfx != null && hoverClip != null) sfx.PlayOneShot(hoverClip, 0.5f);
        }

        public void OnPointerExit(PointerEventData e) { hovering = false; pressing = false; }
        public void OnPointerDown(PointerEventData e) { pressing = true; }
        public void OnPointerUp(PointerEventData e) { pressing = false; }
        public void OnPointerClick(PointerEventData e) { Click(); }

        public void Click()
        {
            if (!interactable || busy) return;
            StartCoroutine(ClickRoutine());
        }

        IEnumerator ClickRoutine()
        {
            busy = true;
            interactable = false;
            if (sfx != null && clickClip != null) sfx.PlayOneShot(clickClip);
            visual.localRotation = Quaternion.identity;
            visual.anchoredPosition = visualBasePos;
            Vector3 baseScale = Vector3.one * hoverScale;
            StartCoroutine(UITween.Jelly(visual, baseScale, 0.22f, 0.55f));
            yield return UITween.Wait(clickDelay);
            onClick.Invoke();
        }

        // 弹出出场：从 0 放大并回弹
        public IEnumerator PopIn(float duration = 0.55f)
        {
            busy = true;
            gameObject.SetActive(true);
            yield return UITween.Run(duration, t =>
            {
                float s = UITween.OutBack(t, 2.4f);
                visual.localScale = new Vector3(s, s, 1f);
                if (shadow != null) shadow.localScale = new Vector3(s, s, 1f);
            });
            busy = false;
        }
    }
}
