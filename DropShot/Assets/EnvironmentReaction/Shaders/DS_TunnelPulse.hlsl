// Drop Shot · 棋盘管道的“心跳波”
// 用法：Shader Graph 里加 Custom Function 节点，Type = File，Source = 本文件，
//       Name 分别填 DS_TunnelPulse / DS_EnvColor（精度选 Float）。
// 输入里的 _DS_* 全局参数在 Shader Graph 中建成同名 Float 属性，
// Node Settings → Shader Declaration 设为 Global（旧版取消 Exposed），由 EnvironmentAudioDirector 每帧写入。

// Depth      : 该像素沿管道方向到球的距离（世界单位，前方为正）。
//              例：dot(WorldPos - BallPos, TunnelForward)，BallPos / TunnelForward 也可以做成全局参数。
// BeatFloat  : _DS_BeatFloat（从第一拍开始的拍数，含小数）
// Breath     : _DS_Breath（1 正常随拍；Strike 提示 / 蓄力 / 暂停时为 0）
// Bass, Dim  : _DS_Bass, _DS_Dim
// WaveSpeed  : 每拍传播多少世界单位（建议 10–14）
// WaveWidth  : 波的半宽（建议 2–3）
// SkipNear   : 球前方多近的区域不参与（建议 3），保持判定区清爽
void DS_TunnelPulse_float(float Depth, float BeatFloat, float Breath, float Bass, float Dim,
                          float WaveSpeed, float WaveWidth, float SkipNear, out float Pulse)
{
    float idle = saturate(Bass * 0.15);               // 低频带来的缓慢“呼吸”底色

    if (BeatFloat < 0.0)
    {
        Pulse = idle * (1.0 - Dim * 0.8);
        return;
    }

    float beatIndex = floor(BeatFloat);
    float phase = BeatFloat - beatIndex;
    float inv2w2 = 1.0 / (2.0 * WaveWidth * WaveWidth);

    // 这一拍发出的波，和上一拍还在往前跑的波
    float d0 = Depth - phase * WaveSpeed;
    float d1 = Depth - (phase + 1.0) * WaveSpeed;
    float s0 = (fmod(beatIndex, 4.0) < 0.5) ? 1.0 : 0.5;          // 小节第一拍更强
    float s1 = (fmod(beatIndex + 3.0, 4.0) < 0.5) ? 1.0 : 0.5;
    float wave = s0 * exp(-d0 * d0 * inv2w2) + 0.6 * s1 * exp(-d1 * d1 * inv2w2);

    float nearMask = smoothstep(SkipNear, SkipNear * 2.0, Depth);
    Pulse = saturate(wave * nearMask * Breath * 0.6 + idle) * (1.0 - Dim * 0.8);
}

// 把 Pulse / 去饱和 / 白闪合成到棋盘格的颜色上。
// BaseColor  : 原来的棋盘格颜色
// PulseColor : 当前配色的高亮色（可按 _DS_PaletteIndex 从 3–4 套配色里选）
// Desat, Flash : _DS_Desat, _DS_Flash
void DS_EnvColor_float(float3 BaseColor, float3 PulseColor, float Pulse, float Desat, float Flash,
                       out float3 Color, out float3 Emission)
{
    float3 c = lerp(BaseColor, PulseColor, Pulse * 0.5);
    float grey = dot(c, float3(0.299, 0.587, 0.114));
    c = lerp(c, grey.xxx, saturate(Desat));
    Color = c;
    Emission = PulseColor * Pulse + Flash * 0.6;
}
