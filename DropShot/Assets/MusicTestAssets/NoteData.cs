using System;
using UnityEngine;

[Serializable]
public class NoteData
{
    [Header("事件类型")]
    public string type;

    [Header("轨道")]
    public int lane;

    [Header("开始时间")]
    public float time;

    [Header("持续时间")]
    public float duration;

    [Header("Strike释放目标时间")]
    public float targetTime;
}
