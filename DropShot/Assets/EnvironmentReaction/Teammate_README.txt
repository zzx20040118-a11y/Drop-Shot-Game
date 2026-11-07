Drop Shot · 任务 7 环境音频响应方案 · 交付包 v0.1（2026-11-07）

适配 BGM：Gameotoon（SlimeyFox），约 122 BPM、G 大调（从录屏估算，请以谱面 BPM 为准）

01_方案文档_环境音频响应方案.docx / .pdf
    完整方案：需求分析、信号源、环境响应映射表、按游戏状态分层、
    环境声音设计、Unity 实现、素材清单、排期与验收、音频交付说明（第 9 节）

02_环境音频/   48 kHz / 16-bit WAV，共 18 个
    Loops/     3 个循环素材（整小节长度，可直接开 Loop）
               env_amb_tunnel_loop_122bpm_16bar   管道氛围底噪（建议音量 0.18，Ambient 组）
               env_long_hold_hum_loop_2bar        Long 按住 hum（0.35，SFX）
               env_combo_L3_sparkle_loop_2bar     combo L3 高频闪烁层（0.3，Ambient）
    OneShots/  15 个单次音效
               env_strike_riser_1bar / _2bar      Strike 上升音，文件末尾 = 松开时间（0.6，SFX）
               env_strike_release_impact          Strike 成功“开闸”冲击（0.9，SFX）
               env_flight_whoosh                  起飞穿管道（0.5，SFX）
               env_land_thud                      落地（0.5，SFX）
               env_strike_miss_down               Strike 失败（0.6，SFX）
               env_combo_chime_10/30/60/100       combo 里程碑，量化到下一拍（0.45，UI）
               env_countin_tick / _accent         倒数（0.4，UI）
               env_pause_tapestop / env_resume_rewind  暂停 / 继续（0.5，UI）
               env_end_tail                       曲终前 2 小节开始播的结束尾音（0.6，Ambient）
    建议音量以音乐 AudioSource = 0.7 为基准。

03_试听/       preview_env_layers_122bpm（mp3 / wav，39 秒）
    用占位鼓点代替 BGM，按顺序演示各层，时间线见文档第 9 节。

04_Unity/
    Scripts/   EnvironmentAudioDirector.cs   环境音频导演（节拍时钟、频谱、状态机、Shader 全局参数、Mixer）
               TunnelRingPulse.cs            管道灯环心跳波（之后加灯环物体时用）
               CandyBeatSpin.cs              糖果音符锁拍旋转
               JellyNoteAudioDriver.cs       长音符果冻 Shader 驱动
    Shaders/   DS_TunnelPulse.hlsl           Shader Graph Custom Function：棋盘管道心跳波 + 配色合成
    注意：脚本尚未在 Unity 里编译测试，导入后先看 Console。

05_生成脚本/   envaudio.py（生成全部音频）、preview.py（生成试听版）
    需要 Python 3 + numpy + scipy + soundfile。改 envaudio.py 里的 BPM 后重新运行即可适配新曲子。
