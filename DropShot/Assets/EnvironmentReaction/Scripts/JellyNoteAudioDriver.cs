// Drop Shot · 长音符果冻 Shader 音频驱动
// 挂在使用 SG_LongNote_Jelly 材质的长音符上：
//   _JellyFrequency 锁到 BPM，_JellyAmplitude 跟低频，按住时 _SquashAmount 随拍轻压。
// 注意：Shader 里用 Time * Frequency 做正弦时，频率在一首歌内保持不变即可；
// 想做到严格锁拍，把 Time 节点换成全局 _DS_BeatFloat（见方案文档第 6 节）。

using UnityEngine;

namespace DropShot.Environment
{
    [RequireComponent(typeof(Renderer))]
    public class JellyNoteAudioDriver : MonoBehaviour
    {
        public float baseAmplitude = 0.5f;   // 与材质当前值一致
        public float bassAmplitude = 0.3f;
        [Tooltip("每拍摆动几次")]
        public float wobblesPerBeat = 1f;
        public float holdSquash = 0.15f;
        [Tooltip("由音符脚本在这根长条被按住时设为 true；为空时用导演的全局 LongHolding")]
        public bool held;
        public bool useGlobalHold = true;

        static readonly int ID_Amp = Shader.PropertyToID("_JellyAmplitude");
        static readonly int ID_Freq = Shader.PropertyToID("_JellyFrequency");
        static readonly int ID_Squash = Shader.PropertyToID("_SquashAmount");

        Renderer rend;
        MaterialPropertyBlock mpb;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            var d = EnvironmentAudioDirector.Instance;
            if (d == null) return;

            bool isHeld = held || (useGlobalHold && d.LongHolding);

            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(ID_Freq, d.bpm / 60f * wobblesPerBeat);
            mpb.SetFloat(ID_Amp, baseAmplitude + d.Bass * bassAmplitude);
            mpb.SetFloat(ID_Squash, isHeld ? d.BeatPulse * holdSquash : 0f);
            rend.SetPropertyBlock(mpb);
        }
    }
}
