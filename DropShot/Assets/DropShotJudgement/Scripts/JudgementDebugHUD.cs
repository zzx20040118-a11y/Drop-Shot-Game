using UnityEngine;

namespace DropShot.Judgement
{
    // 调试面板：不用做任何 UI 就能看到歌曲时间、最近一次判定和偏差毫秒，方便调判定窗口和音频偏移。
    public class JudgementDebugHUD : MonoBehaviour
    {
        public JudgementSystem judge;
        public bool show = true;
        public KeyCode toggleKey = KeyCode.F1;

        GUIStyle style;

        void Awake()
        {
            if (judge == null) judge = FindAnyObjectByType<JudgementSystem>();
        }

        void Update()
        {
            if (KeyInput.Down(toggleKey)) show = !show;
        }

        void OnGUI()
        {
            if (!show || judge == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16 };

            JudgeCore c = judge.Core;
            string last = "—";
            if (judge.LastEvent.HasValue)
            {
                JudgeEvent e = judge.LastEvent.Value;
                string lane = e.lane == 0 ? "D" : e.lane == 1 ? "F" : "空格";
                last = $"{e.result}  {e.kind}/{e.phase}  通道 {lane}  偏差 {e.offsetMs:+0;-0;0} ms";
            }

            string text = c == null
                ? "判定器：未读入谱面"
                : $"歌曲时间  {judge.SongTime:0.000} s   音频偏移 {judge.audioOffsetMs:0} ms\n" +
                  $"最近判定  {last}\n" +
                  $"Perfect {c.PerfectCount}   Good {c.GoodCount}   Miss {c.MissCount}   / 共 {c.NoteCount}\n" +
                  $"连击 {c.Combo}（最大 {c.MaxCombo}）   得分 {c.Score}\n" +
                  $"窗口 Perfect ±{judge.settings.perfectWindow * 1000:0} / Good ±{judge.settings.goodWindow * 1000:0} ms   [{toggleKey} 隐藏]";

            GUI.Box(new Rect(12, 12, 560, 128), text, style);
        }
    }
}
