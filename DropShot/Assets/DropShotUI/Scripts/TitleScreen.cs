using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DropShot.UI
{
    // 标题页流程：淡入 → Logo 掉落弹跳 → 甜甜圈 loading（后台真加载游戏场景）→ 完成动画
    // → 球座弹出、球掉到球座上 → START / QUIT 弹出 → 点 START：球被击飞进螺旋、螺旋放大 → 进入游戏（按标题动画草稿）
    public class TitleScreen : MonoBehaviour
    {
        [Header("场景")]
        [Tooltip("游戏关卡场景名，需要加进 Build Settings")]
        public string gameSceneName = "HalftoneEffect";
        [Tooltip("loading 至少显示多久（秒），真加载太快时也能看到完整动画")]
        public float minLoadingSeconds = 2.4f;

        [Header("引用")]
        public SceneFader fader;
        public RectTransform logo;
        [Tooltip("标语整组（文字 + 投影）用 CanvasGroup 淡入")]
        public CanvasGroup tagline;
        public DonutLoader loader;
        public JellyButton startButton;
        [Tooltip("可空：按开始页面草图加的 QUIT 按钮")]
        public JellyButton quitButton;
        public TMP_Text startHint;

        [Header("开场击球（可空，按标题动画草稿）")]
        public RectTransform ball;
        public RectTransform tee;
        [Tooltip("螺旋的父物体：转场时整体放大，冲进螺旋")]
        public RectTransform spiralZoom;

        [Header("音效（可空）")]
        public AudioSource sfx;
        public AudioClip logoLandClip;
        public AudioClip loadTickClip;
        public AudioClip loadCompleteClip;
        public AudioClip startClip;

        AsyncOperation loadOp;
        Vector2 logoHome;
        bool logoIdle;

        void Awake()
        {
            logoHome = logo.anchoredPosition;
            logo.anchoredPosition = logoHome + Vector2.up * 900f;
            if (tagline != null) tagline.alpha = 0f;
            startButton.gameObject.SetActive(false);
            if (startHint != null) startHint.alpha = 0f;
            startButton.onClick.AddListener(OnStart);
            if (ball != null) ball.gameObject.SetActive(false);
            if (tee != null) tee.localScale = Vector3.zero;
            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(false);
                quitButton.onClick.AddListener(OnQuit);
            }
        }

        void Start() => StartCoroutine(Flow());

        void Update()
        {
            if (!logoIdle) return;
            float t = Time.unscaledTime;
            logo.anchoredPosition = logoHome + Vector2.up * Mathf.Sin(t * 1.6f) * 10f;
            logo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * 1.5f);
            if (startHint != null && startButton.gameObject.activeSelf)
                startHint.alpha = 0.55f + 0.45f * Mathf.Sin(t * 3f);
        }

        IEnumerator Flow()
        {
            bool canLoad = !string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName);
            if (canLoad)
            {
                loadOp = SceneManager.LoadSceneAsync(gameSceneName);
                loadOp.allowSceneActivation = false;
            }
            else
            {
                Debug.LogWarning($"[DropShot UI] 游戏场景「{gameSceneName}」不在 Build Settings 里，loading 只做演示");
            }

            yield return UITween.Wait(0.35f);
            yield return LogoDrop();
            StartCoroutine(UITween.Run(0.5f, t => { if (tagline != null) tagline.alpha = t; }));

            // 显示进度 = min(真实进度, 最短时长进度)，再做平滑，避免一下跳满
            float shown = 0f, elapsed = 0f;
            int lastTick = 0;
            while (shown < 0.999f)
            {
                elapsed += Time.unscaledDeltaTime;
                float real = loadOp != null ? Mathf.Clamp01(loadOp.progress / 0.9f) : 1f;
                float target = Mathf.Min(real, elapsed / minLoadingSeconds);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.2f);
                loader.SetProgress(shown);
                int tick = Mathf.FloorToInt(shown * 10f);
                if (tick > lastTick) { lastTick = tick; Play(loadTickClip, 0.35f); }
                yield return null;
            }

            Play(loadCompleteClip);
            yield return loader.PlayComplete();
            logoIdle = true;
            if (tee != null && ball != null) yield return BallOnTee();
            if (quitButton != null) StartCoroutine(DelayedPop(quitButton, 0.12f));
            yield return startButton.PopIn();
            if (startHint != null) startHint.alpha = 1f;
        }

        // 球座弹出，球从上方掉下来弹两下落在球座上（呼应「Drop」）
        IEnumerator BallOnTee()
        {
            yield return UITween.Run(0.35f, t => tee.localScale = Vector3.one * UITween.OutBack(t, 2.2f));
            Vector2 rest = ball.anchoredPosition;
            ball.gameObject.SetActive(true);
            bool landed = false;
            yield return UITween.Run(0.75f, t =>
            {
                ball.anchoredPosition = Vector2.LerpUnclamped(rest + Vector2.up * 700f, rest, UITween.OutBounce(t));
                if (!landed && t > 0.36f) { landed = true; Play(logoLandClip, 0.6f); }
            });
            StartCoroutine(UITween.Jelly(ball, Vector3.one, 0.18f, 0.4f));
        }

        IEnumerator DelayedPop(JellyButton b, float delay)
        {
            yield return UITween.Wait(delay);
            yield return b.PopIn();
        }

        IEnumerator LogoDrop()
        {
            Vector2 from = logoHome + Vector2.up * 900f;
            bool landed = false;
            yield return UITween.Run(1.0f, t =>
            {
                logo.anchoredPosition = Vector2.LerpUnclamped(from, logoHome, UITween.OutBounce(t));
                if (!landed && t > 0.36f) { landed = true; Play(logoLandClip); }
            });
            yield return UITween.Jelly(logo, Vector3.one, 0.12f, 0.45f);
        }

        void OnStart()
        {
            Play(startClip);
            StartCoroutine(EnterGame());
        }

        void OnQuit()
        {
            if (startButton != null) startButton.interactable = false;
            StartCoroutine(QuitGame());
        }

        IEnumerator QuitGame()
        {
            yield return fader.FadeOut();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator EnterGame()
        {
            if (quitButton != null) quitButton.interactable = false;
            if (ball != null && ball.gameObject.activeSelf && spiralZoom != null) yield return ShootIntoSpiral();
            else yield return fader.FadeOut();
            if (loadOp != null) loadOp.allowSceneActivation = true;
            else yield return fader.FadeIn();
        }

        // 球被击飞：先压扁蓄力，沿弧线飞向螺旋中心边飞边变小，螺旋随即放大冲进去，同时淡出
        IEnumerator ShootIntoSpiral()
        {
            yield return UITween.Run(0.1f, t => ball.localScale = new Vector3(1f + 0.25f * t, 1f - 0.3f * t, 1f));
            var parent = (RectTransform)ball.parent;
            Vector2 p0 = ball.anchoredPosition;
            Vector2 p2 = parent.InverseTransformPoint(spiralZoom.position);
            Vector2 p1 = (p0 + p2) * 0.5f + Vector2.up * 520f;
            yield return UITween.Run(0.6f, t =>
            {
                float e = UITween.InOutSine(t);
                ball.anchoredPosition = (1 - e) * (1 - e) * p0 + 2 * (1 - e) * e * p1 + e * e * p2;
                ball.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.15f, e);
                ball.localRotation = Quaternion.Euler(0, 0, -720f * e);
            });
            ball.gameObject.SetActive(false);
            var spin = spiralZoom.GetComponentInChildren<Spinner>();
            if (spin != null) spin.degreesPerSecond *= 10f;
            StartCoroutine(UITween.Run(0.7f, t => spiralZoom.localScale = Vector3.one * Mathf.Lerp(1f, 7f, UITween.InCubic(t))));
            yield return UITween.Wait(0.3f);
            yield return fader.FadeOut();
        }

        void Play(AudioClip clip, float volume = 1f)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
        }
    }
}
