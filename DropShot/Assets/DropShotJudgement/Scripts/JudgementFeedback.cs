using System.Collections;
using TMPro;
using UnityEngine;

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

        void OnEnable()
        {
            if (judge == null) judge = FindAnyObjectByType<JudgementSystem>();
            if (judge == null) return;
            judge.Judged += OnJudged;
            judge.StrikeChargeStarted += OnChargeStart;
            judge.StrikeChargeEnded += OnChargeEnd;
        }

        void OnDisable()
        {
            if (judge == null) return;
            judge.Judged -= OnJudged;
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
            if (comboText != null) comboText.text = e.combo >= 2 ? $"{e.combo} COMBO" : "";
            if (scoreText != null && judge.Core != null) scoreText.text = judge.Core.Score.ToString("N0");
            if (judgeText == null) return;

            judgeText.text = e.result == JudgeResult.Perfect ? "PERFECT" : e.result == JudgeResult.Good ? "GOOD" : "MISS";
            judgeText.color = e.result == JudgeResult.Perfect ? perfectColor : e.result == JudgeResult.Good ? goodColor : missColor;
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = StartCoroutine(PopText());
        }

        IEnumerator PopText()
        {
            Transform tr = judgeText.transform;
            judgeText.alpha = 1f;
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.one * Mathf.Lerp(textPopScale, 1f, t / 0.12f);
                yield return null;
            }
            tr.localScale = Vector3.one;
            yield return new WaitForSeconds(textHoldSeconds);
            t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                judgeText.alpha = 1f - t / 0.2f;
                yield return null;
            }
            judgeText.alpha = 0f;
        }

        void SpawnFx(JudgeEvent e)
        {
            GameObject prefab = e.result == JudgeResult.Perfect ? perfectFx : e.result == JudgeResult.Good ? goodFx : missFx;
            Transform at = e.lane == 0 ? lane0Anchor : e.lane == 1 ? lane1Anchor : strikeAnchor;
            if (prefab == null || at == null) return;
            GameObject fx = Instantiate(prefab, at.position, at.rotation);
            Destroy(fx, fxLifetime);
        }

        void OnChargeStart(int noteIndex)
        {
            if (strikeChargeLoop != null) strikeChargeLoop.Play();
        }

        void OnChargeEnd(int noteIndex)
        {
            if (strikeChargeLoop != null) strikeChargeLoop.Stop();
        }
    }
}
