using UnityEngine;
using DropShot.Judgement;

public class BeatmapNoteSpawner : MonoBehaviour
{
    [Header("判定系统")]
    public JudgementSystem judge;

    [Header("音符 Prefab")]
    public GameObject normalNotePrefab;
    public GameObject longNotePrefab;

    [Header("判定点")]
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    [Header("生成参数")]
    public float approachSeconds = 2f;
    public float approachSpeed = 40f;

    [Header("Long Note")]
    public float longNoteWidth = 1f;

    private GameObject[] blocks;

    void Start()
    {
        if (judge == null || judge.Chart == null)
        {
            Debug.LogError("BeatmapNoteSpawner: JudgementSystem 或 Chart 未设置！");
            return;
        }

        var notes = judge.Chart.notes;
        blocks = new GameObject[notes.Count];

        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];

            if (note.type == "Normal")
            {
                blocks[i] = Instantiate(normalNotePrefab);
                blocks[i].SetActive(false);
            }
            else if (note.type == "Long")
            {
                blocks[i] = Instantiate(longNotePrefab);
                blocks[i].SetActive(false);

                // 长条长度 = 持续时间 × 音符移动速度
                float length = note.duration * approachSpeed;

                // Cube 默认沿 Z 轴延伸
                blocks[i].transform.localScale =
                    new Vector3(longNoteWidth, 1f, length);
            }
        }

        judge.Judged += OnJudged;
    }

    void Update()
    {
        if (!judge.Running || blocks == null)
            return;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null)
                continue;

            var note = judge.Chart.notes[i];

            float remaining = (float)(note.time - judge.SongTime);

            if (remaining > approachSeconds)
                continue;

            Transform hitPoint =
                note.lane == 0 ? leftHitPoint : rightHitPoint;

            Vector3 headPosition = hitPoint.position
                - Vector3.right * remaining * approachSpeed;

            if (note.type == "Long")
            {
                float length = note.duration * approachSpeed;

                // 长条中心位置：头部位置 + 长度的一半
                blocks[i].transform.position =
                    headPosition + Vector3.right * length * 0.5f;

                // 长条沿 X 轴延伸
                blocks[i].transform.localScale =
                    new Vector3(length, longNoteWidth, longNoteWidth);
            }
            else
            {
                blocks[i].transform.position = headPosition;
            }

            blocks[i].SetActive(true);
        }
    }

    void OnJudged(JudgeEvent result)
    {
        if (blocks == null)
            return;

        if (result.noteIndex < 0 || result.noteIndex >= blocks.Length)
            return;

        if (result.kind == NoteKind.Normal &&
            result.phase == JudgePhase.Head)
        {
            Destroy(blocks[result.noteIndex]);
        }

        if (result.kind == NoteKind.Long &&
            result.phase == JudgePhase.Tail)
        {
            Destroy(blocks[result.noteIndex]);
        }
    }

    void OnDestroy()
    {
        if (judge != null)
            judge.Judged -= OnJudged;
    }
}
