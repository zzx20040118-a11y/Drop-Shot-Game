using System;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropShot.Judgement
{
    // 判定器：读谱面 JSON、读歌曲时间和 D / F / 空格，输出 Perfect / Good / Miss。
    // 挂在场景里任意物体上，拖入音乐 AudioSource 和谱面 JSON 即可运行。
    public class JudgementSystem : MonoBehaviour
    {
        [Serializable] public class JudgeUnityEvent : UnityEvent<JudgeEvent> { }

        [Header("谱面与音乐")]
        public TextAsset chartJson;
        public AudioSource music;
        [Tooltip("开始时自动播放音乐并开始判定")]
        public bool playOnStart = true;
        public bool readKeyboard = true;
        [Tooltip("音频延迟补偿（毫秒）。觉得总是按早了就调大")]
        public float audioOffsetMs = 0f;

        [Header("按键")]
        public KeyCode lane0Key = KeyCode.D;
        public KeyCode lane1Key = KeyCode.F;
        public KeyCode strikeKey = KeyCode.Space;

        [Header("判定参数")]
        public JudgeSettings settings = new JudgeSettings();

        [Header("事件（可在 Inspector 里连到别的脚本）")]
        public JudgeUnityEvent onJudged = new JudgeUnityEvent();

        public event Action<JudgeEvent> Judged;
        public event Action<int> StrikeChargeStarted;
        public event Action<int> StrikeChargeEnded;
        public event Action<int> StrikeAutoStarted;
        public event Action<int> LongHoldStarted;
        public event Action<int> LongHoldEnded;

        // 如果郑子昕那边已经有统一的歌曲时钟，把它赋给这里即可，判定就不再读 AudioSource
        public Func<double> externalSongTime;

        public JudgeCore Core { get; private set; }
        public ChartFile Chart { get; private set; }
        public bool Running { get; private set; }
        public JudgeEvent? LastEvent { get; private set; }

        public double SongTime
        {
            get
            {
                double t = externalSongTime != null ? externalSongTime()
                         : music != null && music.clip != null ? (double)music.timeSamples / music.clip.frequency
                         : 0.0;
                return t - audioOffsetMs / 1000.0;
            }
        }

        // Awake 里读谱面，别的脚本在 Start 里就能拿到 Chart / Core
        void Awake()
        {
            if (chartJson != null) Load(chartJson.text);
        }

        void Start()
        {
            if (playOnStart && Core != null) Begin();
        }

        public void Load(string json)
        {
            Chart = JsonUtility.FromJson<ChartFile>(json);
            if (Chart == null || Chart.notes == null || Chart.notes.Count == 0)
            {
                Debug.LogError("[Judgement] 谱面为空或格式不对，需要 {\"notes\":[...]}");
                Core = null;
                return;
            }

            Core = new JudgeCore(Chart.notes, settings);
            Core.Judged += e =>
            {
                LastEvent = e;
                Judged?.Invoke(e);
                onJudged.Invoke(e);
            };
            Core.StrikeChargeStarted += i => StrikeChargeStarted?.Invoke(i);
            Core.StrikeChargeEnded += i => StrikeChargeEnded?.Invoke(i);
            Core.StrikeAutoStarted += i => StrikeAutoStarted?.Invoke(i);
            Core.LongHoldStarted += i => LongHoldStarted?.Invoke(i);
            Core.LongHoldEnded += i => LongHoldEnded?.Invoke(i);
            Debug.Log($"[Judgement] 读入 {Chart.notes.Count} 个音符");
        }

        public void Begin()
        {
            if (Core == null) return;
            if (music != null && externalSongTime == null && !music.isPlaying) music.Play();
            Running = true;
        }

        public void Stop() => Running = false;

        void Update()
        {
            if (!Running || Core == null) return;

            double t = SongTime;
            if (readKeyboard)
            {
                HandleKey(lane0Key, 0, t);
                HandleKey(lane1Key, 1, t);
                HandleKey(strikeKey, JudgeCore.StrikeLane, t);
            }
            Core.Tick(t);

            if (Core.Finished)
            {
                Running = false;
                Debug.Log($"[Judgement] 结束：Perfect {Core.PerfectCount} / Good {Core.GoodCount} / Miss {Core.MissCount}，最大连击 {Core.MaxCombo}，得分 {Core.Score}");
            }
        }

        void HandleKey(KeyCode key, int lane, double t)
        {
            if (KeyInput.Down(key)) Core.Press(lane, t);
            if (KeyInput.Up(key)) Core.Release(lane, t);
        }
    }

    // 同时兼容旧版 Input Manager 和新版 Input System（Unity 6 新项目默认用新版）
    public static class KeyInput
    {
        public static bool Down(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Find(key);
            return k != null && k.wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool Up(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Find(key);
            return k != null && k.wasReleasedThisFrame;
#else
            return Input.GetKeyUp(key);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Find(KeyCode key)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            // 字母和 Space 的名字在 KeyCode 与 Key 两个枚举里一致
            return Enum.TryParse(key.ToString(), out Key k) ? kb[k] : null;
        }
#endif
    }
}
