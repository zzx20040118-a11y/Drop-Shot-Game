using DropShot.Judgement;
using UnityEngine;

public class BallHit : MonoBehaviour
{
    public Transform ballMesh;
    public JudgementSystem judge;
    public AudioSource hitAudio;

    [Range(0.5f, 1f)] public float normalSize = 0.9f;
    [Range(0.5f, 1f)] public float longSize = 0.85f;
    [Min(0.01f)] public float recoverSpeed = 1f;

    private Vector3 originalScale;
    private float currentSize = 1f;
    private int longNoteIndex = -1;
    private float longStart;
    private float longDuration;

    void Awake()
    {
        if (ballMesh != null)
            originalScale = ballMesh.localScale;
    }

    void OnEnable()
    {
        if (judge == null) return;
        judge.Judged += OnJudged;
        judge.LongHoldStarted += StartLong;
        judge.LongHoldEnded += EndLong;
    }

    void OnDisable()
    {
        if (judge != null)
        {
            judge.Judged -= OnJudged;
            judge.LongHoldStarted -= StartLong;
            judge.LongHoldEnded -= EndLong;
        }

        longNoteIndex = -1;
        currentSize = 1f;
        if (ballMesh != null)
            ballMesh.localScale = originalScale;
    }

    void Update()
    {
        if (ballMesh == null) return;

        if (longNoteIndex >= 0 && judge != null)
        {
            float progress = Mathf.Clamp01(
                (float)(judge.SongTime - longStart) / longDuration);
            currentSize = Mathf.Lerp(1f, longSize, progress);
        }
        else
        {
            currentSize = Mathf.MoveTowards(currentSize, 1f, recoverSpeed * Time.deltaTime);
        }

        ballMesh.localScale = originalScale * currentSize;
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.kind == NoteKind.Normal && result.phase == JudgePhase.Head
            && result.result != JudgeResult.Miss && longNoteIndex < 0)
            currentSize = normalSize;
    }

    void StartLong(int noteIndex)
    {
        var note = judge.Chart.notes[noteIndex];
        longNoteIndex = noteIndex;
        longStart = note.time;
        longDuration = Mathf.Max(0.01f, note.duration);
    }

    void EndLong(int noteIndex)
    {
        if (longNoteIndex == noteIndex)
            longNoteIndex = -1;
    }

    public void Hit()
    {
        if (hitAudio != null && hitAudio.clip != null)
        {
            hitAudio.PlayOneShot(hitAudio.clip);
        }
    }
}
