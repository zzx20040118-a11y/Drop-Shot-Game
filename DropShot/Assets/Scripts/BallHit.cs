using DropShot.Judgement;
using UnityEngine;

public class BallHit : MonoBehaviour
{
    public Transform ballMesh;
    public JudgementSystem judge;
    public AudioSource hitAudio;

    public bool squashFeedback = true;
    [Range(0.5f, 1f)] public float normalSize = 0.9f;
    [Range(0.5f, 1f)] public float longSize = 0.85f;
    [Range(0.5f, 1f)] public float strikeSize = 0.7f;
    [Min(0.01f)] public float recoverSpeed = 1f;
    [Min(0.01f)] public float longReleaseRecoverSpeed = 5f;

    private Vector3 originalScale;
    private float currentSize = 1f;
    private int longNoteIndex = -1;
    private int strikeNoteIndex = -1;
    private bool recoveringFromLong;
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
        judge.StrikeChargeStarted += StartStrike;
        judge.StrikeChargeEnded += EndStrike;
    }

    void OnDisable()
    {
        if (judge != null)
        {
            judge.Judged -= OnJudged;
            judge.LongHoldStarted -= StartLong;
            judge.LongHoldEnded -= EndLong;
            judge.StrikeChargeStarted -= StartStrike;
            judge.StrikeChargeEnded -= EndStrike;
        }

        longNoteIndex = -1;
        strikeNoteIndex = -1;
        recoveringFromLong = false;
        currentSize = 1f;
        if (ballMesh != null)
            ballMesh.localScale = originalScale;
    }

    void Update()
    {
        if (ballMesh == null) return;

        if (strikeNoteIndex >= 0 && judge != null)
        {
            var note = judge.Chart.notes[strikeNoteIndex];
            float progress = Mathf.Clamp01((float)(judge.SongTime - note.time)
                / Mathf.Max(0.01f, note.targetTime - note.time));
            currentSize = Mathf.Lerp(1f, strikeSize, progress);
        }
        else if (longNoteIndex >= 0 && judge != null)
        {
            float progress = Mathf.Clamp01(
                (float)(judge.SongTime - longStart) / longDuration);
            currentSize = Mathf.Lerp(1f, longSize, progress);
        }
        else
        {
            float speed = recoveringFromLong ? longReleaseRecoverSpeed : recoverSpeed;
            currentSize = Mathf.MoveTowards(currentSize, 1f, speed * Time.deltaTime);
            if (currentSize >= 1f)
                recoveringFromLong = false;
        }

        float size = Mathf.Clamp(currentSize, 0.5f, 1f);
        if (squashFeedback)
        {
            float stretch = 1f / Mathf.Sqrt(size);
            ballMesh.localScale = Vector3.Scale(originalScale,
                new Vector3(stretch, size, stretch));
        }
        else
        {
            ballMesh.localScale = originalScale * size;
        }
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.kind == NoteKind.Normal && result.phase == JudgePhase.Head
            && result.result != JudgeResult.Miss && longNoteIndex < 0 && strikeNoteIndex < 0)
        {
            recoveringFromLong = false;
            currentSize = normalSize;
        }
    }

    void StartLong(int noteIndex)
    {
        recoveringFromLong = false;
        var note = judge.Chart.notes[noteIndex];
        longNoteIndex = noteIndex;
        longStart = note.time;
        longDuration = Mathf.Max(0.01f, note.duration);
    }

    void EndLong(int noteIndex)
    {
        if (longNoteIndex == noteIndex)
        {
            longNoteIndex = -1;
            recoveringFromLong = true;
        }
    }

    void StartStrike(int noteIndex)
    {
        recoveringFromLong = false;
        strikeNoteIndex = noteIndex;
    }

    void EndStrike(int noteIndex)
    {
        if (strikeNoteIndex == noteIndex)
            strikeNoteIndex = -1;
    }

    public void Hit()
    {
        if (hitAudio != null && hitAudio.clip != null)
        {
            hitAudio.PlayOneShot(hitAudio.clip);
        }
    }
}
