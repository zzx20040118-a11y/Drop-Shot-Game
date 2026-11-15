using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropShot.Judgement
{
    // 命中反馈：按判定播放音效、显示文字和连击、在对应赛道生成特效。
    // 所有字段都可以留空，留空的部分直接跳过。
    public class JudgementFeedback : MonoBehaviour
    {
        public JudgementSystem judge;

        [Header("音效（素材库里的建议对应见 README）")]
        public AudioSource sfxSource;
        public AudioClip perfectClip;
        public AudioClip goodClip;
        public AudioClip missClip;
        [Header("Strike Audio")]
        [Tooltip("Uses the regular Perfect clip when empty.")]
        public AudioClip strikePerfectClip;
        [Tooltip("Uses the regular Good clip when empty.")]
        public AudioClip strikeGoodClip;
        [Tooltip("Perfect 时叠加的碎裂声，可留空")]
        public AudioClip perfectLayerClip;
        [Tooltip("按住空格蓄力时循环播放")]
        public AudioSource strikeChargeLoop;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Header("Judgement Images")]
        public Image judgeImage;
        public Image comboImage;
        public Image comboBackdrop;
        public Sprite perfectSprite;
        public Sprite goodSprite;
        public Sprite missSprite;
        public Sprite comboSprite;

        [Header("Strike UI Focus")]
        public bool focusStrike = true;
        public Vector2 strikeComboOffset = new Vector2(0f, 175f);
        [Range(0f, 1f)] public float strikeComboAlpha = 0.65f;

        [Header("文字")]
        public TMP_Text judgeText;
        public TMP_Text comboText;
        public TMP_Text scoreText;
        public Color perfectColor = new Color(1f, 0.84f, 0.2f);
        public Color goodColor = new Color(0.4f, 0.85f, 1f);
        public Color missColor = new Color(0.6f, 0.6f, 0.6f);
        public float textPopScale = 1.3f;
        public float textHoldSeconds = 0.4f;

        [Header("特效 Prefab（粒子等），生成在对应赛道位置")]
        public GameObject perfectFx;
        public GameObject goodFx;
        public GameObject missFx;
        public Transform lane0Anchor;
        public Transform lane1Anchor;
        public Transform strikeAnchor;
        public float fxLifetime = 1.5f;

        Coroutine textRoutine;
        bool useImage;
        bool strikeActive;
        Vector2 comboImagePosition;
        Vector2 comboTextPosition;
        Vector3 comboImageScale;
        Vector3 comboTextScale;
        Vector3 imageBaseScale;
        Vector3 textBaseScale;
        Vector2 comboBackdropPosition;
        Vector3 comboBackdropScale;

        void Awake()
        {
            imageBaseScale = judgeImage != null ? judgeImage.transform.localScale : Vector3.one;
            textBaseScale = judgeText != null ? judgeText.transform.localScale : Vector3.one;
            if (judgeImage != null) judgeImage.enabled = false;
            if (comboBackdrop != null)
            {
                comboBackdrop.enabled = false;
                comboBackdropPosition = comboBackdrop.rectTransform.anchoredPosition;
                comboBackdropScale = comboBackdrop.transform.localScale;
            }
            if (comboImage != null)
            {
                comboImage.enabled = false;
                comboImagePosition = comboImage.rectTransform.anchoredPosition;
                comboImageScale = comboImage.transform.localScale;
            }
            if (comboText != null)
            {
                comboTextPosition = comboText.rectTransform.anchoredPosition;
                comboTextScale = comboText.transform.localScale;
            }
            if (judgeText != null) judgeText.text = "";
            if (comboText != null) comboText.text = "";
        }

        void OnEnable()
        {
            if (judge == null) judge = FindAnyObjectByType<JudgementSystem>();
            if (judge == null) return;
            judge.Judged += OnJudged;
            judge.StrikeAutoStarted += OnStrikeShown;
            judge.StrikeChargeStarted += OnChargeStart;
            judge.StrikeChargeEnded += OnChargeEnd;
        }

        void OnDisable()
        {
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = null;
            SetJudgeAlpha(0f);
            SetStrikeFocus(false);
            if (judge == null) return;
            judge.Judged -= OnJudged;
            judge.StrikeAutoStarted -= OnStrikeShown;
            judge.StrikeChargeStarted -= OnChargeStart;
            judge.StrikeChargeEnded -= OnChargeEnd;
        }

        void OnJudged(JudgeEvent e)
        {
            PlaySfx(e);
            ShowText(e);
            SpawnFx(e);
        }

        void PlaySfx(JudgeEvent e)
        {
            if (sfxSource == null) return;
            JudgeResult r = e.result;
            AudioClip clip = r == JudgeResult.Perfect ? perfectClip : r == JudgeResult.Good ? goodClip : missClip;
            if (e.kind == NoteKind.Strike)
            {
                if (r == JudgeResult.Perfect && strikePerfectClip != null)
                    clip = strikePerfectClip;
                else if (r == JudgeResult.Good && strikeGoodClip != null)
                    clip = strikeGoodClip;
            }
            if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
            if (r == JudgeResult.Perfect && perfectLayerClip != null) sfxSource.PlayOneShot(perfectLayerClip, sfxVolume * 0.6f);
        }

        void ShowText(JudgeEvent e)
        {
            bool hasComboImage = comboImage != null && comboSprite != null;
            if (comboBackdrop != null) comboBackdrop.enabled = e.combo >= 2;
            if (comboImage != null)
            {
                comboImage.sprite = comboSprite;
                comboImage.enabled = hasComboImage && e.combo >= 2;
            }
            if (comboText != null)
                comboText.text = e.combo >= 2 ? (hasComboImage ? e.combo.ToString() : $"{e.combo} COMBO") : "";
            if (scoreText != null && judge.Core != null) scoreText.text = judge.Core.Score.ToString("N0");
            if (strikeActive && focusStrike && e.result != JudgeResult.Miss && e.kind != NoteKind.Strike)
                return;
            Sprite sprite = e.result == JudgeResult.Perfect ? perfectSprite
                : e.result == JudgeResult.Good ? goodSprite : missSprite;
            useImage = judgeImage != null && sprite != null;
            if (judgeImage != null)
            {
                judgeImage.sprite = sprite;
                judgeImage.enabled = useImage;
                judgeImage.color = Color.white;
            }
            if (judgeText != null) judgeText.enabled = !useImage;
            if (!useImage && judgeText == null) return;

            if (judgeText != null)
            {
                judgeText.text = e.result == JudgeResult.Perfect ? "PERFECT" : e.result == JudgeResult.Good ? "GOOD" : "MISS";
                judgeText.color = e.result == JudgeResult.Perfect ? perfectColor : e.result == JudgeResult.Good ? goodColor : missColor;
            }
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = StartCoroutine(PopText());
        }

        IEnumerator PopText()
        {
            Transform tr = useImage ? judgeImage.transform : judgeText.transform;
            Vector3 baseScale = useImage ? imageBaseScale : textBaseScale;
            SetJudgeAlpha(1f);
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                tr.localScale = baseScale * Mathf.Lerp(textPopScale, 1f, t / 0.12f);
                yield return null;
            }
            tr.localScale = baseScale;
            yield return new WaitForSeconds(textHoldSeconds);
            t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                SetJudgeAlpha(1f - t / 0.2f);
                yield return null;
            }
            SetJudgeAlpha(0f);
        }

        void SetJudgeAlpha(float alpha)
        {
            if (useImage)
            {
                Color color = judgeImage.color;
                color.a = Mathf.Clamp01(alpha);
                judgeImage.color = color;
            }
            else if (judgeText != null)
                judgeText.alpha = Mathf.Clamp01(alpha);
        }

        void SpawnFx(JudgeEvent e)
        {
            GameObject prefab = e.result == JudgeResult.Perfect ? perfectFx : e.result == JudgeResult.Good ? goodFx : missFx;
            Transform at = e.lane == 0 ? lane0Anchor : e.lane == 1 ? lane1Anchor : strikeAnchor;
            if (prefab == null || at == null) return;
            GameObject fx = Instantiate(prefab, at.position, at.rotation);
            Destroy(fx, fxLifetime);
        }

        void OnStrikeShown(int noteIndex)
        {
            if (!focusStrike) return;
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = null;
            SetJudgeAlpha(0f);
            SetStrikeFocus(true);
        }

        void SetStrikeFocus(bool active)
        {
            strikeActive = active;
            Vector2 offset = active ? strikeComboOffset : Vector2.zero;
            float scale = active ? 0.85f : 1f;
            float alpha = active ? strikeComboAlpha : 1f;
            if (comboBackdrop != null)
            {
                comboBackdrop.rectTransform.anchoredPosition = comboBackdropPosition + offset;
                comboBackdrop.transform.localScale = comboBackdropScale * scale;
                Color color = comboBackdrop.color;
                color.a = alpha;
                comboBackdrop.color = color;
            }
            if (comboImage != null)
            {
                comboImage.rectTransform.anchoredPosition = comboImagePosition + offset;
                comboImage.transform.localScale = comboImageScale * scale;
                Color color = comboImage.color;
                color.a = alpha;
                comboImage.color = color;
            }
            if (comboText != null)
            {
                comboText.rectTransform.anchoredPosition = comboTextPosition + offset;
                comboText.transform.localScale = comboTextScale * scale;
                comboText.alpha = alpha;
            }
        }

        void OnChargeStart(int noteIndex)
        {
            if (strikeChargeLoop != null) strikeChargeLoop.Play();
        }

        void OnChargeEnd(int noteIndex)
        {
            SetStrikeFocus(false);
            if (strikeChargeLoop != null) strikeChargeLoop.Stop();
        }
    }
}
