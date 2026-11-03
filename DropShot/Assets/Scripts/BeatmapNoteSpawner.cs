using UnityEngine;
using DropShot.Judgement;

public class BeatmapNoteSpawner : MonoBehaviour
{
    public JudgementSystem judge;
    public GameObject normalNotePrefab;
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    public float approachSeconds = 2f;
    public float approachSpeed = 40f;

    private GameObject[] blocks;

    void Start()
    {
        var notes = judge.Chart.notes;
        blocks = new GameObject[notes.Count];

        
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].type != "Normal")
                continue;

            blocks[i] = Instantiate(normalNotePrefab);
            blocks[i].SetActive(false);
        }

        judge.Judged += OnJudged;
    }

    void Update()
    {
        if (!judge.Running)
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

            blocks[i].transform.position = hitPoint.position
                - Vector3.right * remaining * approachSpeed;

            blocks[i].SetActive(true);
        }
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.kind == NoteKind.Normal)
            Destroy(blocks[result.noteIndex]);
    }

    void OnDestroy()
    {
        if (judge != null)
            judge.Judged -= OnJudged;
    }
}