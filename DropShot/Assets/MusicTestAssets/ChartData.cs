using System;

[Serializable]
public class ChartData
{
    // 音乐 BPM
    public float bpm;

    // 音乐起始偏移
    public float musicOffset;

    // 整首音乐的音符列表
    public NoteData[] notes;
}
