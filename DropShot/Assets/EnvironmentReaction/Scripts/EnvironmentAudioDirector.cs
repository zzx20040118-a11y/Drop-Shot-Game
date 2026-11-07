// Drop Shot · 环境音频导演
// 把「节拍时钟 + 实时频谱 + 游戏事件」合成为 0–1 的强度值，
// 写入 Shader 全局参数、驱动环境灯光/雾/摄像机，并控制 AudioMixer。
// 场景里放一个即可；判定、Long、Strike 等逻辑调用下方 “游戏事件入口” 的公开方法。

using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DropShot.Environment
{
    public enum EnvJudge { Perfect, Good, Miss }

    public enum EnvState
    {
        Idle,          // 菜单 / 加载 / 开场前
        Flow,          // 正常滚动
        StrikePrompt,  // Strike 提示已出现，还没按下
        StrikeCharge,  // 按住 Space 蓄力
        StrikeFlight,  // Strike 成功，球在飞
        Paused,
        Ended
    }

    [DefaultExecutionOrder(-50)]
    public class EnvironmentAudioDirector : MonoBehaviour
    {
        public static EnvironmentAudioDirector Instance { get; private set; }

        /// <summary>每拍触发一次，参数为拍序号（从 0 开始）。</summary>
        public event Action<int> Beat;
        /// <summary>每小节第一拍触发，参数为小节序号。</summary>
        public event Action<int> Bar;

        // ───────────── 配置 ─────────────
        [Header("音乐 / 节拍")]
        public AudioSource music;
        public float bpm = 120f;
        public int beatsPerBar = 4;
        [Tooltip("曲子第一拍距离音频开头的秒数")]
        public float firstBeatOffset = 0f;
        [Tooltip("与 JudgementSystem 的 audioOffsetMs 保持一致")]
        public float audioOffsetMs = 0f;
        [Tooltip("每拍脉冲的衰减速度，越大越干脆")]
        public float pulseSharpness = 6f;

        [Header("频谱")]
        public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
        public float attackTime = 0.03f;
        public float releaseTime = 0.25f;
        [Tooltip("自动增益峰值每秒保留的比例（越小，换段落后适应越快）")]
        [Range(0.1f, 0.99f)] public float peakKeepPerSecond = 0.7f;

        [Header("Combo")]
        [Tooltip("L1 / L2 / L3 的 combo 门槛")]
        public int[] comboThresholds = { 10, 30, 60 };
        [Tooltip("到达这些 combo 时在下一拍播放提示音")]
        public int[] comboMilestones = { 10, 30, 60, 100 };

        [Header("混音（参数需在 AudioMixer 里 Expose）")]
        public AudioMixer mixer;
        [Tooltip("Music 组 Lowpass Simple 的 Cutoff freq")]
        public string musicCutoffParam = "MusicCutoff";
        [Tooltip("Music 组音量（dB）")]
        public string musicVolumeParam = "MusicVolume";
        public float chargeMinCutoff = 800f;
        public float musicDuckDb = -2f;
        public AudioMixerSnapshot normalSnapshot;
        public AudioMixerSnapshot pauseSnapshot;

        [Header("环境声")]
        public AudioSource sfx;            // 一次性音效（冲击、落地、miss）
        public AudioSource riserSource;    // Strike riser，需要 PlayScheduled
        public AudioSource chimeSource;    // combo 里程碑，量化到下一拍
        public AudioSource longHumSource;  // Long 按住时的持续 hum，loop
        public AudioClip impactClip, strikeMissClip, landClip;

        [Header("场景")]
        public Light[] envLights;
        public bool driveFog = true;
        public float fogBassAmount = 0.3f;
        public Camera targetCamera;
        public float barFovKick = 0.5f;
        public float strikeFovKick = 4f;

        [Header("可访问性")]
        [Tooltip("减弱闪烁：关闭白闪和频闪，只保留平滑变化")]
        public bool reduceFlashing = false;

        // ───────────── 只读输出（其他脚本读取） ─────────────
        public EnvState State { get; private set; } = EnvState.Idle;
        public bool SongRunning { get; private set; }
        public double SongTime { get; private set; }
        public float BeatFloat { get; private set; }
        public float BeatPhase { get; private set; }
        public float BeatPulse { get; private set; }
        public float BarPulse { get; private set; }
        public float Bass { get; private set; }
        public float Mid { get; private set; }
        public float High { get; private set; }
        public float Energy { get; private set; }
        public int ComboLevel { get; private set; }
        public bool LongHolding { get; private set; }
        public float Dim { get; private set; }
        public float Desaturate { get; private set; }
        public float Flash { get; private set; }
        public float StrikeCharge01 { get; private set; }
        public int PaletteIndex { get; private set; }

        /// <summary>灯环等是否应该“屏住呼吸”（不随拍脉冲）。</summary>
        public bool HoldingBreath =>
            State == EnvState.StrikePrompt || State == EnvState.StrikeCharge || State == EnvState.Paused;

        // ───────────── Shader 全局参数 ID ─────────────
        static readonly int ID_BeatPulse  = Shader.PropertyToID("_DS_BeatPulse");
        static readonly int ID_BarPulse   = Shader.PropertyToID("_DS_BarPulse");
        static readonly int ID_BeatFloat  = Shader.PropertyToID("_DS_BeatFloat");
        static readonly int ID_Bass       = Shader.PropertyToID("_DS_Bass");
        static readonly int ID_Mid        = Shader.PropertyToID("_DS_Mid");
        static readonly int ID_High       = Shader.PropertyToID("_DS_High");
        static readonly int ID_Energy     = Shader.PropertyToID("_DS_Energy");
        static readonly int ID_Combo      = Shader.PropertyToID("_DS_ComboLevel");
        static readonly int ID_Dim        = Shader.PropertyToID("_DS_Dim");
        static readonly int ID_Desat      = Shader.PropertyToID("_DS_Desat");
        static readonly int ID_Flash      = Shader.PropertyToID("_DS_Flash");
        static readonly int ID_Charge     = Shader.PropertyToID("_DS_StrikeCharge");
        static readonly int ID_Palette    = Shader.PropertyToID("_DS_PaletteIndex");
        static readonly int ID_Breath     = Shader.PropertyToID("_DS_Breath");

        // ───────────── 内部状态 ─────────────
        const int SpectrumSize = 512;
        const float FullCutoff = 22000f;

        readonly float[] spectrum = new float[SpectrumSize];
        readonly float[] bandPeak = { 1e-4f, 1e-4f, 1e-4f };

        double dspSongStart;
        double pauseDsp;
        EnvState stateBeforePause;
        int lastBeat = int.MinValue;
        int pendingComboLevel;

        double strikeReleaseSongTime;
        double chargeStartSongTime;
        float cutoff = FullCutoff;
        float cutoffResponse = 0.05f;   // 秒
        float missDesat;
        float duck;
        float strikeMissFlicker = -1f;  // <0 表示没在闪
        float fovOffset;
        float longHoldTime;
        float breath = 1f;              // 1 = 正常随拍呼吸，0 = 屏住呼吸

        float[] lightBase;
        float fogBase;
        float fovBase;

        // ───────────── 生命周期 ─────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            lightBase = new float[envLights != null ? envLights.Length : 0];
            for (int i = 0; i < lightBase.Length; i++)
                lightBase[i] = envLights[i] != null ? envLights[i].intensity : 0f;
            fogBase = RenderSettings.fogDensity;
            if (targetCamera != null) fovBase = targetCamera.fieldOfView;
            if (longHumSource != null) { longHumSource.loop = true; longHumSource.volume = 0f; }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // 如果 MusicManager 没有调用 BeginSong，就在音乐开始播放时自动估计起点
            if (!SongRunning && music != null && music.isPlaying)
                BeginSong(AudioSettings.dspTime - music.time);

            UpdateClock();
            UpdateSpectrum(dt);
            UpdateStateValues(dt);
            ApplyOutputs();
        }

        // ───────────── 节拍时钟 ─────────────
        /// <summary>由 MusicManager 在 PlayScheduled 时调用，传入音乐真正开始的 dspTime（最准）。</summary>
        public void BeginSong(double dspStartTime)
        {
            dspSongStart = dspStartTime;
            SongRunning = true;
            lastBeat = int.MinValue;
            if (State == EnvState.Idle || State == EnvState.Ended) State = EnvState.Flow;
        }

        double SongTimeToDsp(double songTime) => dspSongStart + audioOffsetMs / 1000.0 + songTime;

        void UpdateClock()
        {
            if (!SongRunning)
            {
                BeatPulse = BarPulse = 0f;
                return;
            }

            double now = State == EnvState.Paused ? pauseDsp : AudioSettings.dspTime;
            SongTime = now - dspSongStart - audioOffsetMs / 1000.0;

            double beat = (SongTime - firstBeatOffset) * bpm / 60.0;
            BeatFloat = (float)beat;

            if (beat < 0)
            {
                BeatPhase = 0f;
                BeatPulse = BarPulse = 0f;
                return;
            }

            int idx = (int)Math.Floor(beat);
            BeatPhase = (float)(beat - idx);

            double barPos = beat / beatsPerBar;
            float barPhase = (float)(barPos - Math.Floor(barPos));

            BeatPulse = Mathf.Exp(-pulseSharpness * BeatPhase);
            BarPulse = Mathf.Exp(-pulseSharpness * barPhase * beatsPerBar);

            if (idx != lastBeat && State != EnvState.Paused)
            {
                lastBeat = idx;
                OnBeatInternal(idx);
            }
        }

        void OnBeatInternal(int idx)
        {
            bool downbeat = idx % beatsPerBar == 0;
            if (downbeat)
            {
                // Combo 升级统一在小节第一拍生效
                if (pendingComboLevel > ComboLevel) ComboLevel = pendingComboLevel;
                if (State == EnvState.Flow) fovOffset += barFovKick;
            }

            Beat?.Invoke(idx);
            if (downbeat) Bar?.Invoke(idx / beatsPerBar);
        }

        /// <summary>下一拍对应的 dspTime，用于把环境音效量化到拍上。</summary>
        public double NextBeatDsp()
        {
            double beat = (SongTime - firstBeatOffset) * bpm / 60.0;
            double next = Math.Floor(beat) + 1.0;
            return SongTimeToDsp(firstBeatOffset + next * 60.0 / bpm);
        }

        // ───────────── 频谱 ─────────────
        void UpdateSpectrum(float dt)
        {
            float rawBass = 0f, rawMid = 0f, rawHigh = 0f;

            if (music != null && music.isPlaying)
            {
                music.GetSpectrumData(spectrum, 0, fftWindow);
                float binHz = AudioSettings.outputSampleRate * 0.5f / SpectrumSize;
                for (int i = 1; i < SpectrumSize; i++)
                {
                    float f = i * binHz;
                    float v = spectrum[i];
                    if (f < 150f) rawBass += v;
                    else if (f < 2000f) rawMid += v;
                    else if (f < 10000f) rawHigh += v;
                    else break;
                }
            }

            float keep = Mathf.Pow(peakKeepPerSecond, dt);
            Bass = SmoothBand(0, rawBass, keep, dt, Bass);
            Mid  = SmoothBand(1, rawMid,  keep, dt, Mid);
            High = SmoothBand(2, rawHigh, keep, dt, High);
            Energy = 0.5f * Bass + 0.3f * Mid + 0.2f * High;
        }

        float SmoothBand(int band, float raw, float keep, float dt, float current)
        {
            bandPeak[band] = Mathf.Max(bandPeak[band] * keep, raw, 1e-4f);
            float target = Mathf.Clamp01(raw / bandPeak[band]);
            float tau = target > current ? attackTime : releaseTime;
            return current + (target - current) * (1f - Mathf.Exp(-dt / Mathf.Max(tau, 1e-4f)));
        }

        // ───────────── 状态值 ─────────────
        void UpdateStateValues(float dt)
        {
            Flash = Mathf.MoveTowards(Flash, 0f, dt * 6f);
            missDesat = Mathf.MoveTowards(missDesat, 0f, dt / 0.2f);
            duck = Mathf.MoveTowards(duck, 0f, dt / 0.3f);
            fovOffset = Mathf.Lerp(fovOffset, 0f, 1f - Mathf.Exp(-dt / 0.1f));

            // 蓄力进度：从按下到谱面规定的释放时间
            if (State == EnvState.StrikeCharge)
            {
                double span = strikeReleaseSongTime - chargeStartSongTime;
                StrikeCharge01 = span > 1e-3
                    ? Mathf.Clamp01((float)((SongTime - chargeStartSongTime) / span))
                    : 1f;
            }
            else
            {
                StrikeCharge01 = Mathf.MoveTowards(StrikeCharge01, 0f, dt * 4f);
            }

            float dimTarget;
            switch (State)
            {
                case EnvState.Idle:         dimTarget = 0.3f;  break;
                case EnvState.StrikePrompt: dimTarget = 0.25f; break;
                case EnvState.StrikeCharge: dimTarget = 0.35f; break;
                case EnvState.Paused:       dimTarget = 0.5f;  break;
                case EnvState.Ended:        dimTarget = 1f;    break;
                default:                    dimTarget = 0f;    break;
            }
            float dimSpeed = State == EnvState.Ended ? 0.5f : 4f;
            Dim = Mathf.MoveTowards(Dim, dimTarget, dt * dimSpeed);

            // Strike Miss：“电压不稳”闪两下
            if (strikeMissFlicker >= 0f)
            {
                strikeMissFlicker += dt;
                if (strikeMissFlicker > 0.3f) strikeMissFlicker = -1f;
                else if (!reduceFlashing && Mathf.Repeat(strikeMissFlicker, 0.15f) < 0.06f)
                    Dim = Mathf.Max(Dim, 0.5f);
            }

            Desaturate = Mathf.Max(missDesat, StrikeCharge01 * 0.3f);
            breath = Mathf.MoveTowards(breath, HoldingBreath ? 0f : 1f, dt * 8f);

            // 音乐低通：蓄力时按进度扫低，其余时间回到全频（对数空间插值）
            float cutoffTarget = State == EnvState.StrikeCharge
                ? Mathf.Exp(Mathf.Lerp(Mathf.Log(FullCutoff), Mathf.Log(chargeMinCutoff), StrikeCharge01))
                : FullCutoff;
            float resp = State == EnvState.StrikeCharge ? 0.05f : cutoffResponse;
            cutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(cutoff), Mathf.Log(cutoffTarget),
                                          1f - Mathf.Exp(-dt / Mathf.Max(resp, 1e-3f))));

            // Long hum：按住越久越响一点，松开 120 ms 淡出
            if (longHumSource != null)
            {
                if (LongHolding) longHoldTime += dt;
                float humTarget = LongHolding ? 0.15f + 0.1f * Mathf.Clamp01(longHoldTime / 2f) : 0f;
                float humSpeed = LongHolding ? 1f : 1f / 0.12f;
                longHumSource.volume = Mathf.MoveTowards(longHumSource.volume, humTarget, dt * humSpeed);
                if (longHumSource.volume <= 0f && longHumSource.isPlaying && !LongHolding) longHumSource.Stop();
            }
        }

        // ───────────── 写出 ─────────────
        void ApplyOutputs()
        {
            float pulse = HoldingBreath ? 0f : BeatPulse;
            float bar = HoldingBreath ? 0f : BarPulse;
            float flash = reduceFlashing ? Mathf.Min(Flash, 0.3f) : Flash;

            Shader.SetGlobalFloat(ID_BeatPulse, pulse);
            Shader.SetGlobalFloat(ID_BarPulse, bar);
            Shader.SetGlobalFloat(ID_BeatFloat, BeatFloat);
            Shader.SetGlobalFloat(ID_Bass, Bass);
            Shader.SetGlobalFloat(ID_Mid, Mid);
            Shader.SetGlobalFloat(ID_High, High);
            Shader.SetGlobalFloat(ID_Energy, Energy);
            Shader.SetGlobalFloat(ID_Combo, ComboLevel / 3f);
            Shader.SetGlobalFloat(ID_Dim, Dim);
            Shader.SetGlobalFloat(ID_Desat, Desaturate);
            Shader.SetGlobalFloat(ID_Flash, flash);
            Shader.SetGlobalFloat(ID_Charge, StrikeCharge01);
            Shader.SetGlobalFloat(ID_Palette, PaletteIndex);
            Shader.SetGlobalFloat(ID_Breath, breath);

            if (mixer != null)
            {
                mixer.SetFloat(musicCutoffParam, cutoff);
                mixer.SetFloat(musicVolumeParam, musicDuckDb * duck);
            }

            float lightScale = (1f - Dim * 0.8f) * (1f + Bass * 0.15f + bar * 0.1f * ComboLevel) + flash * 0.5f;
            for (int i = 0; i < lightBase.Length; i++)
                if (envLights[i] != null) envLights[i].intensity = lightBase[i] * lightScale;

            if (driveFog && RenderSettings.fog)
                RenderSettings.fogDensity = fogBase * (1f + Bass * fogBassAmount);

            if (targetCamera != null)
                targetCamera.fieldOfView = fovBase + fovOffset;
        }

        // ═════════════ 游戏事件入口 ═════════════

        /// <summary>每次普通音符 / Long 判定后调用。combo 为判定后的连击数。</summary>
        public void ReportJudgement(EnvJudge judge, int combo)
        {
            if (judge == EnvJudge.Perfect) Flash = Mathf.Max(Flash, 0.2f);
            if (judge == EnvJudge.Miss) missDesat = 1f;

            int level = 0;
            for (int i = 0; i < comboThresholds.Length; i++)
                if (combo >= comboThresholds[i]) level = i + 1;

            if (level < ComboLevel) ComboLevel = level;   // 降级立即生效
            pendingComboLevel = level;                      // 升级等下一个小节

            if (judge != EnvJudge.Miss && chimeSource != null && chimeSource.clip != null
                && Array.IndexOf(comboMilestones, combo) >= 0)
                chimeSource.PlayScheduled(NextBeatDsp());
        }

        public void LongStart()
        {
            LongHolding = true;
            longHoldTime = 0f;
            if (longHumSource != null && !longHumSource.isPlaying) longHumSource.Play();
        }

        public void LongEnd()
        {
            LongHolding = false;
        }

        /// <summary>Strike 提示出现时调用。releaseSongTime = 谱面里应当松开 Space 的时间（秒，曲内时间）。</summary>
        public void StrikePrompt(double releaseSongTime)
        {
            State = EnvState.StrikePrompt;
            strikeReleaseSongTime = releaseSongTime;
            ScheduleRiser(releaseSongTime);
        }

        /// <summary>玩家按下 Space 开始蓄力。</summary>
        public void StrikeChargeBegin()
        {
            State = EnvState.StrikeCharge;
            chargeStartSongTime = SongTime;
        }

        /// <summary>Strike 最终判定结果（只在判定逻辑给出最终结果后调用一次）。</summary>
        public void StrikeResult(bool success)
        {
            if (riserSource != null) riserSource.Stop();

            if (success)
            {
                State = EnvState.StrikeFlight;
                Flash = reduceFlashing ? 0.3f : 1f;
                cutoffResponse = 0.05f;          // 50 ms “开闸”
                duck = 1f;
                fovOffset += strikeFovKick;
                PaletteIndex++;
                if (sfx != null && impactClip != null) sfx.PlayOneShot(impactClip);
            }
            else
            {
                State = EnvState.Flow;
                cutoffResponse = 0.4f;           // 慢慢恢复
                strikeMissFlicker = 0f;
                if (sfx != null && strikeMissClip != null) sfx.PlayOneShot(strikeMissClip);
            }
        }

        /// <summary>FlightBall 落地、NormalBall 恢复时调用。</summary>
        public void BallLanded()
        {
            if (State == EnvState.StrikeFlight) State = EnvState.Flow;
            if (sfx != null && landClip != null) sfx.PlayOneShot(landClip);
        }

        /// <summary>游戏暂停。音乐本身的 Pause 仍由 MusicManager 负责。</summary>
        public void PauseGame()
        {
            if (State == EnvState.Paused) return;
            stateBeforePause = State;
            State = EnvState.Paused;
            pauseDsp = AudioSettings.dspTime;
            if (riserSource != null) riserSource.Pause();
            if (pauseSnapshot != null) pauseSnapshot.TransitionTo(0.15f);
        }

        public void ResumeGame()
        {
            if (State != EnvState.Paused) return;
            dspSongStart += AudioSettings.dspTime - pauseDsp;   // 补上暂停时长
            State = stateBeforePause;
            if (riserSource != null) riserSource.UnPause();
            if (normalSnapshot != null) normalSnapshot.TransitionTo(0.15f);
        }

        public void SongEnded()
        {
            State = EnvState.Ended;
            SongRunning = false;
            LongHolding = false;
        }

        // ───────────── 工具 ─────────────
        void ScheduleRiser(double releaseSongTime)
        {
            if (riserSource == null || riserSource.clip == null) return;

            double releaseDsp = SongTimeToDsp(releaseSongTime);
            double startDsp = releaseDsp - riserSource.clip.length;
            double now = AudioSettings.dspTime;

            riserSource.Stop();
            if (startDsp > now + 0.01)
            {
                riserSource.time = 0f;
                riserSource.PlayScheduled(startDsp);
            }
            else
            {
                // 提示出现得比 riser 短：从中间开始播，保证顶点仍然落在释放时间
                riserSource.time = Mathf.Clamp((float)(now - startDsp), 0f, riserSource.clip.length - 0.01f);
                riserSource.Play();
            }
        }
    }
}
