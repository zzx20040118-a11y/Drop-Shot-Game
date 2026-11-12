using System.Collections.Generic;
using DropShot.Judgement;
using FractureTool;
using UnityEngine;

// Start after the normal spawner so the replaced candy can be removed safely.
[DefaultExecutionOrder(100)]
public class StrikeWallSpawner : MonoBehaviour
{
    public JudgementSystem judge;
    public ColliderTestNoteSpawner noteSpawner;
    public Transform wallHitPoint;
    public GameObject wallPrefab;
    public BallCameraFollow ballCamera;
    public StageSwitcher stageSwitcher;

    [Header("Wall Appearance")]
    [Min(0f)] public float wallShowDelay = 0.2f;

    [Header("墙与碎片淡出")]
    [Min(0.01f)] public float wallFadeSeconds = 0.2f;
    [Min(0.01f)] public float fragmentFadeSeconds = 0.3f;

    class Wall
    {
        public int strikeIndex;
        public float hitTime;
        public GameObject model;
        public bool strikeSucceeded;
        public bool finished;
        public bool fading;
    }

    readonly List<Wall> walls = new List<Wall>();

    void Start()
    {
        if (ballCamera == null)
            ballCamera = FindFirstObjectByType<BallCameraFollow>();

        if (judge == null || judge.Core == null || noteSpawner == null || wallHitPoint == null
            || wallPrefab == null || wallPrefab.GetComponent<FractureController>() == null)
        {
            Debug.LogWarning("StrikeWallSpawner needs Judge, Note Spawner, Wall Hit Point and a Wall Prefab with Fracture Controller.", this);
            enabled = false;
            return;
        }

        var notes = judge.Chart.notes;
        var usedNotes = new HashSet<int>();
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].type != "Strike") continue;

            // Find the first normal candy after this Strike's release beat.
            int next = -1;
            for (int j = 0; j < notes.Count; j++)
            {
                if (notes[j].type != "Normal" || notes[j].time <= notes[i].targetTime
                    || usedNotes.Contains(j)) continue;
                if (next < 0 || notes[j].time < notes[next].time) next = j;
            }
            if (next < 0 || !judge.Core.UseAsStrikeWall(next)) continue;

            usedNotes.Add(next);
            noteSpawner.RemoveNoteVisual(next);

            GameObject model = Instantiate(wallPrefab, transform, false);
            model.name = $"StrikeWall_{i}";
            model.SetActive(false);
            walls.Add(new Wall { strikeIndex = i, hitTime = notes[next].time, model = model });
        }

        judge.Judged += OnJudged;
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.kind != NoteKind.Strike || result.phase != JudgePhase.Release) return;
        foreach (Wall wall in walls)
            if (wall.strikeIndex == result.noteIndex)
                wall.strikeSucceeded = result.result != JudgeResult.Miss;
    }

    void Update()
    {
        if (!judge.Running && !judge.Core.Finished) return;

        foreach (Wall wall in walls)
        {
            if (wall.finished || wall.model == null) continue;
            float remaining = (float)(wall.hitTime - judge.SongTime);
            float visibleTimeBeforeHit = noteSpawner.approachSeconds - wallShowDelay;
            visibleTimeBeforeHit = Mathf.Max(0f, visibleTimeBeforeHit);

            if (remaining > visibleTimeBeforeHit)
            {
                continue;
            }

            wall.model.SetActive(true);
            wall.model.transform.position = wallHitPoint.position
                - Vector3.right * remaining * noteSpawner.approachSpeed;

            if (remaining <= 0f && wall.strikeSucceeded)
            {
                Physics.SyncTransforms();
                FractureController fracture = wall.model.GetComponent<FractureController>();
                fracture.fragmentFadeSeconds = fragmentFadeSeconds;
                fracture.Fracture();
                if (ballCamera != null)
                    ballCamera.ShakeStrike();
                wall.finished = true;
                if (stageSwitcher != null)
                    stageSwitcher.NextStage();
                Destroy(wall.model, fracture.destroyAfterSeconds);
            }
            else if (remaining <= 0f && !wall.fading)
            {
                // Keep moving while fading after passing the ball.
                wall.fading = true;
                wall.model.AddComponent<WallVisualFade>().Begin(wallFadeSeconds);
            }
        }
    }

    void OnDestroy()
    {
        if (judge != null) judge.Judged -= OnJudged;
        foreach (Wall wall in walls)
            if (wall.model != null) Destroy(wall.model);
    }
}
