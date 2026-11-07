// Drop Shot · 糖果音符锁拍旋转
// 两种糖果现在是随机旋转的。挂上这个脚本后：旋转角度由节拍决定（每拍转固定角度），
// 每拍还会轻轻“弹”一下。旋转轴仍然随机，保留现在的随机感，但节奏和音乐对齐。
// 如果现有的旋转写在音符脚本里，把那段 transform.Rotate 换成这个组件即可。

using UnityEngine;

namespace DropShot.Environment
{
    public class CandyBeatSpin : MonoBehaviour
    {
        [Tooltip("每拍旋转的角度。90 = 每小节转一圈")]
        public float degreesPerBeat = 90f;
        [Tooltip("每拍弹一下的缩放幅度")]
        public float beatSquash = 0.06f;
        [Tooltip("Strike 提示 / 蓄力时转速倍率")]
        public float holdingBreathSpeed = 0.3f;
        public bool randomAxis = true;

        Vector3 axis = Vector3.up;
        Quaternion baseRotation;
        Vector3 baseScale;
        float angle;
        float lastBeat = float.NaN;

        void OnEnable()
        {
            baseRotation = transform.localRotation;
            baseScale = transform.localScale;
            if (randomAxis) axis = Random.onUnitSphere;
            angle = Random.Range(0f, 360f);
            lastBeat = float.NaN;
        }

        void Update()
        {
            var d = EnvironmentAudioDirector.Instance;
            if (d == null || !d.SongRunning) return;

            float beat = d.BeatFloat;
            if (float.IsNaN(lastBeat)) lastBeat = beat;
            float delta = Mathf.Max(0f, beat - lastBeat);   // 暂停时拍数不走，糖果也停
            lastBeat = beat;

            float speed = d.HoldingBreath ? holdingBreathSpeed : 1f;
            angle += delta * degreesPerBeat * speed;

            transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, axis);
            transform.localScale = baseScale * (1f + d.BeatPulse * beatSquash);
        }
    }
}
