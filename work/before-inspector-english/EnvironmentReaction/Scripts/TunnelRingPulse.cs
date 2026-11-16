// Drop Shot · 管道灯环心跳波
// 每拍从球的位置向前发出一道亮度波，沿管道灯环传播；小节第一拍更强。
// 依赖 EnvironmentAudioDirector。灯环材质需开启 Emission。

using System.Collections.Generic;
using UnityEngine;

namespace DropShot.Environment
{
    public class TunnelRingPulse : MonoBehaviour
    {
        [Tooltip("波的起点，一般是 NormalBall")]
        public Transform origin;
        [Tooltip("管道前进方向的参照（不要用会滚动旋转的球本身），默认主摄像机")]
        public Transform forwardReference;
        public Renderer[] rings;

        [ColorUsage(false, true)] public Color baseEmission = new Color(0.1f, 0.3f, 0.6f);
        [ColorUsage(false, true)] public Color pulseEmission = new Color(0.4f, 1.2f, 3f);
        public string emissionProperty = "_EmissionColor";

        [Header("波形")]
        [Tooltip("每拍传播的世界距离")]
        public float waveSpeed = 12f;
        public float waveWidth = 2.5f;
        public float maxDistance = 80f;
        public float beatStrength = 0.5f;
        public float barStrength = 1f;
        [Tooltip("离球这么近的灯环不参与脉冲，保持判定区清爽")]
        public float skipNearDistance = 3f;
        [Range(0f, 1f)] public float maxPulse = 0.6f;

        struct Wave { public float startBeat; public float strength; }

        readonly List<Wave> waves = new List<Wave>();
        MaterialPropertyBlock mpb;
        int emissionId;
        EnvironmentAudioDirector director;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            emissionId = Shader.PropertyToID(emissionProperty);
            if (forwardReference == null && Camera.main != null) forwardReference = Camera.main.transform;

            director = EnvironmentAudioDirector.Instance;
            if (director != null) director.Beat += OnBeat;
        }

        void OnDestroy()
        {
            if (director != null) director.Beat -= OnBeat;
        }

        void OnBeat(int beatIndex)
        {
            if (director.HoldingBreath) return;   // Strike 提示 / 蓄力 / 暂停时屏住呼吸

            bool downbeat = beatIndex % director.beatsPerBar == 0;
            float s = downbeat ? barStrength : beatStrength;
            s *= 1f + 0.25f * director.ComboLevel;
            if (director.LongHolding) s *= 0.5f;

            waves.Add(new Wave { startBeat = beatIndex, strength = s });
        }

        void Update()
        {
            if (director == null || origin == null || rings == null) return;

            float beat = director.BeatFloat;
            float limit = maxDistance + waveWidth * 3f;
            waves.RemoveAll(w => (beat - w.startBeat) * waveSpeed > limit);

            Vector3 fwd = forwardReference != null ? forwardReference.forward : Vector3.forward;
            Vector3 o = origin.position;
            float invTwoW2 = 1f / (2f * waveWidth * waveWidth);
            float brightness = 1f - director.Dim * 0.8f;

            for (int i = 0; i < rings.Length; i++)
            {
                Renderer r = rings[i];
                if (r == null) continue;

                float along = Vector3.Dot(r.transform.position - o, fwd);
                float pulse = 0f;

                if (along > skipNearDistance)
                {
                    for (int w = 0; w < waves.Count; w++)
                    {
                        float front = (beat - waves[w].startBeat) * waveSpeed;
                        float d = along - front;
                        pulse += waves[w].strength * Mathf.Exp(-d * d * invTwoW2);
                    }
                }

                float t = Mathf.Min(pulse, 1f) * maxPulse;
                Color c = Color.Lerp(baseEmission, pulseEmission, t) * brightness;

                r.GetPropertyBlock(mpb);
                mpb.SetColor(emissionId, c);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
