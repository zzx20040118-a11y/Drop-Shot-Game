using System;
using System.Collections;
using TMPro;
using UnityEngine;
using DropShot.Judgement;

namespace DropShot.UI
{
    // 结局页流程：淡入 → RESULT 掉落 → 成绩面板升起 → 总分滚动计数 → Perfect/Good/Miss/最大连击逐行滑入计数
    // → 准确率 → 评级印章砸下 + 糖针炸开 → 返回 / 再来一次按钮弹出。播放中按空格或点击可快进
    public class ResultScreen : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public RectTransform row;
            public TMP_Text value;
        }

        [Header("场景")]
        public string titleSceneName = "Title";
        [Tooltip("留空则隐藏「再来一次」按钮")]
        public string gameSceneName = "HalftoneEffect";

        [Header("引用")]
        public SceneFader fader;
        public RectTransform header;
        public RectTransform panel;
        public TMP_Text scoreText;
        public Row perfectRow, goodRow, missRow, comboRow;
        public TMP_Text accuracyText;
        public RectTransform stamp;
        public TMP_Text stampLetter;
        public SprinkleBurst stampBurst;
        public JellyButton returnButton;
        public JellyButton retryButton;

        [Header("计数")]
        public float scoreCountSeconds = 1.6f;
        public float rowCountSeconds = 0.45f;

        [Header("没有成绩时（直接打开 End 场景预览）用的示例数据")]
        public int demoScore = 13626, demoPerfect = 40, demoGood = 12, demoMiss = 2, demoMaxCombo = 36;

        [Header("音效（可空）")]
        public AudioSource sfx;
        public AudioClip countTickClip;
        public AudioClip countDoneClip;
        public AudioClip rowClip;
        public AudioClip stampClip;
        public AudioClip rankChimeClip;

        float speed = 1f;
        bool sequenceDone;
        Vector2 headerHome, panelHome;

        void Awake()
        {
            headerHome = header.anchoredPosition;
            panelHome = panel.anchoredPosition;
            header.anchoredPosition = headerHome + Vector2.up * 700f;
            panel.anchoredPosition = panelHome + Vector2.down * 1100f;
            scoreText.text = "0";
            foreach (var r in Rows()) { r.value.text = "0"; r.row.localScale = new Vector3(0f, 1f, 1f); }
            if (accuracyText != null) accuracyText.alpha = 0f;
            stamp.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);

            returnButton.onClick.AddListener(() => fader.FadeToScene(titleSceneName));
            if (retryButton != null) retryButton.onClick.AddListener(() => fader.FadeToScene(gameSceneName));
        }

        Row[] Rows() => new[] { perfectRow, goodRow, missRow, comboRow };

        void Start() => StartCoroutine(Flow());

        void Update()
        {
            if (sequenceDone) return;
            if (KeyInput.Down(KeyCode.Space) || KeyInput.Down(KeyCode.Return) || UIInput.ClickDown()) speed = 6f;
        }

        IEnumerator Flow()
        {
            bool real = ScoreBoard.HasResult;
            int score = real ? ScoreBoard.Score : demoScore;
            int perfect = real ? ScoreBoard.Perfect : demoPerfect;
            int good = real ? ScoreBoard.Good : demoGood;
            int miss = real ? ScoreBoard.Miss : demoMiss;
            int combo = real ? ScoreBoard.MaxCombo : demoMaxCombo;
            int judged = perfect + good + miss;
            float acc = judged == 0 ? 0f : (perfect + 0.5f * good) / judged;
            string rank = acc >= 0.95f ? "S" : acc >= 0.85f ? "A" : acc >= 0.70f ? "B" : "C";

            yield return Wait(0.3f);
            yield return Tween(0.8f, t => header.anchoredPosition = Vector2.LerpUnclamped(headerHome + Vector2.up * 700f, headerHome, UITween.OutBounce(t)));
            StartCoroutine(UITween.Jelly(header, Vector3.one, 0.1f, 0.4f));
            yield return Tween(0.55f, t => panel.anchoredPosition = Vector2.LerpUnclamped(panelHome + Vector2.down * 1100f, panelHome, UITween.OutBack(t, 1.2f)));

            // 总分滚动
            yield return CountUp(scoreText, score, scoreCountSeconds, true);
            Play(countDoneClip);
            yield return UITween.Jelly(scoreText.transform, Vector3.one, 0.25f, 0.45f);

            // 明细逐行
            int[] values = { perfect, good, miss, combo };
            Row[] rows = Rows();
            for (int i = 0; i < rows.Length; i++)
            {
                Play(rowClip, 0.6f);
                RectTransform r = rows[i].row;
                yield return Tween(0.22f, t => r.localScale = new Vector3(UITween.OutBack(t, 2f), 1f, 1f));
                yield return CountUp(rows[i].value, values[i], rowCountSeconds, false);
            }

            if (accuracyText != null)
            {
                accuracyText.text = $"ACCURACY  {acc * 100f:0.0}%";
                yield return Tween(0.3f, t => accuracyText.alpha = t);
            }

            // 评级印章：放大 3 倍从天而降，砸下时面板一震
            yield return Wait(0.2f);
            stampLetter.text = rank;
            stamp.gameObject.SetActive(true);
            yield return Tween(0.28f, t =>
            {
                float s = Mathf.Lerp(3f, 1f, UITween.InCubic(t));
                stamp.localScale = new Vector3(s, s, 1f);
                stamp.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-40f, -12f, t));
                stampLetter.alpha = t;
            });
            Play(stampClip);
            Play(rankChimeClip, 0.8f);
            if (stampBurst != null) stampBurst.Play();
            StartCoroutine(Shake(panel, panelHome, 18f, 0.3f));
            yield return UITween.Jelly(stamp, Vector3.one, 0.15f, 0.4f);

            sequenceDone = true;
            speed = 1f;
            yield return Wait(0.15f);
            StartCoroutine(returnButton.PopIn());
            if (retryButton != null && !string.IsNullOrEmpty(gameSceneName))
            {
                yield return Wait(0.12f);
                StartCoroutine(retryButton.PopIn());
            }
        }

        IEnumerator CountUp(TMP_Text text, int target, float seconds, bool thousands)
        {
            int last = -1;
            float lastTick = -1f;
            yield return Tween(seconds, t =>
            {
                int v = Mathf.RoundToInt(target * UITween.OutCubic(t));
                if (v == last) return;
                last = v;
                text.text = thousands ? v.ToString("N0") : v.ToString();
                if (Time.unscaledTime - lastTick > 0.055f)
                {
                    lastTick = Time.unscaledTime;
                    Play(countTickClip, 0.3f);
                    float p = 1.06f;
                    text.transform.localScale = new Vector3(p, p, 1f);
                }
                else
                {
                    text.transform.localScale = Vector3.Lerp(text.transform.localScale, Vector3.one, 0.5f);
                }
            });
            text.transform.localScale = Vector3.one;
        }

        IEnumerator Shake(RectTransform rt, Vector2 home, float amount, float seconds)
        {
            yield return UITween.Run(seconds, t =>
            {
                float k = (1f - t) * amount;
                rt.anchoredPosition = home + new Vector2(UnityEngine.Random.Range(-k, k), UnityEngine.Random.Range(-k, k));
            });
            rt.anchoredPosition = home;
        }

        // 带快进倍率的缓动和等待
        IEnumerator Tween(float duration, Action<float> step) => UITween.Run(duration / speed, step);
        IEnumerator Wait(float seconds) => UITween.Wait(seconds / speed);

        void Play(AudioClip clip, float volume = 1f)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
        }
    }
}
