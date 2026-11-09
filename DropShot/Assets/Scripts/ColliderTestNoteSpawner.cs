using System.Collections;
using System.Collections.Generic;
using DropShot.Judgement;
using FractureTool;
using UnityEngine;
using UnityEngine.Serialization;

public class ColliderTestNoteSpawner : MonoBehaviour
{
    [Header("判定系统")]
    public JudgementSystem judge;

    [Header("音符 Prefab")]
    public GameObject normalNotePrefab;
    public GameObject longNotePrefab;

    [Header("Hit Effects")]
    public GameObject perfectHitEffectPrefab;
    public GameObject goodHitEffectPrefab;
    public GameObject longPerfectHitEffectPrefab;
    [FormerlySerializedAs("longHitEffectPrefab")]
    public GameObject longGoodHitEffectPrefab;
    [FormerlySerializedAs("perfectHitBall")]
    public Transform hitBall;

    [Header("短音随机模型（留空则使用 Normal Note Prefab）")]
    public GameObject[] normalNotePrefabs = new GameObject[0];

    [Header("短音缓慢旋转")]
    public bool rotateNormalNotes = true;
    [Tooltip("每秒旋转角度；每颗糖果随机选择速度和旋转轴。")]
    public Vector2 normalRotationSpeed = new Vector2(3f, 8f);

    [Header("判定点")]
    public Transform leftHitPoint;
    public Transform rightHitPoint;

    [Header("生成参数")]
    public float approachSeconds = 2f;
    public float approachSpeed = 40f;

    [Header("Long Note")]
    public float longNoteWidth = 1f;

    [Header("长音分段模型（留空则使用原长条）")]
    public GameObject segmentPrefab;

    [Tooltip("每条长音随机选择一款，整条保持相同模型；留空则使用 Segment Prefab。")]
    public GameObject[] segmentPrefabs = new GameObject[0];

    [Min(0f)]
    public float segmentSpacing = 0.02f;
    public Vector3 rotationCompensation = Vector3.zero;

    [Min(0.01f)]
    public float segmentScale = 1f;

    [Header("短音彈飛")]
    public Vector3 hitImpulse = new Vector3(5f, 5f, 0f);
    public float cleanupDelay = 3f;

    private GameObject[] noteObjects;
    private bool[] segmentedLongNotes;
    private Vector3[] normalRotationAxes;
    private float[] normalRotationFactors;
    private float[] normalRotationAngles;
    private Quaternion[] normalInitialRotations;
    private Rigidbody[] normalBodies;
    private RigidbodyInterpolation[] normalBodyInterpolation;
    private double noteMoveTime;

    class LongSegmentState
    {
        public readonly List<Transform> segments = new List<Transform>();
        public int nextSegmentIndex;
        public bool isHolding;
        public float halfSegmentLength;
        public GameObject hitEffect;
    }

    private readonly Dictionary<int, LongSegmentState> longSegments =
        new Dictionary<int, LongSegmentState>();

    void Start()
    {
        if (judge == null || judge.Chart == null)
        {
            Debug.LogError("ColliderTestNoteSpawner needs a JudgementSystem and chart.", this);
            return;
        }

        var notes = judge.Chart.notes;
        noteMoveTime = judge.SongTime;
        noteObjects = new GameObject[notes.Count];
        segmentedLongNotes = new bool[notes.Count];
        normalRotationAxes = new Vector3[notes.Count];
        normalRotationFactors = new float[notes.Count];
        normalRotationAngles = new float[notes.Count];
        normalInitialRotations = new Quaternion[notes.Count];
        normalBodies = new Rigidbody[notes.Count];
        normalBodyInterpolation = new RigidbodyInterpolation[notes.Count];

        for (int noteIndex = 0; noteIndex < notes.Count; noteIndex++)
        {
            var note = notes[noteIndex];

            if (note.type == "Normal")
            {
                CreateNormalNote(noteIndex);
            }
            else if (note.type == "Long")
            {
                CreateLongNote(noteIndex);
            }
        }

        judge.Judged += OnJudged;
    }

    void CreateNormalNote(int noteIndex)
    {
        GameObject prefab = PickRandomPrefab(normalNotePrefabs, normalNotePrefab);
        if (prefab == null)
        {
            Debug.LogWarning("Normal note prefab is not assigned.", this);
            return;
        }

        noteObjects[noteIndex] = Instantiate(prefab);
        normalInitialRotations[noteIndex] = noteObjects[noteIndex].transform.rotation;
        normalBodies[noteIndex] = noteObjects[noteIndex].GetComponent<Rigidbody>();
        if (normalBodies[noteIndex] != null)
        {
            normalBodyInterpolation[noteIndex] = normalBodies[noteIndex].interpolation;
            // The song clock controls approach movement.
            normalBodies[noteIndex].interpolation = RigidbodyInterpolation.None;
        }

        normalRotationAxes[noteIndex] = Random.onUnitSphere;
        normalRotationFactors[noteIndex] = Random.value;
        noteObjects[noteIndex].SetActive(false);
    }

    void CreateLongNote(int noteIndex)
    {
        noteObjects[noteIndex] = Instantiate(longNotePrefab);
        noteObjects[noteIndex].SetActive(false);
        float length = judge.Chart.notes[noteIndex].duration * approachSpeed;
        GameObject selectedSegment = PickRandomPrefab(segmentPrefabs, segmentPrefab);

        if (selectedSegment != null)
        {
            CreateLongNoteSegments(noteObjects[noteIndex], length, noteIndex, selectedSegment);
            segmentedLongNotes[noteIndex] = longSegments.ContainsKey(noteIndex);
        }
    }

    // Called after Start by StrikeWallSpawner to replace a candy with a wall.
    public void RemoveNoteVisual(int noteIndex)
    {
        if (noteObjects == null || noteIndex < 0 || noteIndex >= noteObjects.Length)
            return;

        if (noteObjects[noteIndex] != null)
            Destroy(noteObjects[noteIndex]);
        noteObjects[noteIndex] = null;
    }

    void Update()
    {
        if (judge == null || judge.Core == null || noteObjects == null)
            return;

        if (judge.Core.Finished)
            noteMoveTime = System.Math.Max(noteMoveTime, judge.SongTime) + Time.deltaTime;
        else if (judge.Running)
            noteMoveTime = judge.SongTime;
        else
            return;

        for (int noteIndex = 0; noteIndex < noteObjects.Length; noteIndex++)
        {
            if (noteObjects[noteIndex] == null)
                continue;
            var note = judge.Chart.notes[noteIndex];

            float remaining = (float)(note.time - noteMoveTime);

            if (remaining > approachSeconds)
                continue;

            Transform hitPoint = GetHitPoint(note.lane);

            Vector3 headPosition = hitPoint.position - Vector3.right * remaining * approachSpeed;

            if (note.type == "Long")
            {
                float length = note.duration * approachSpeed;
                noteObjects[noteIndex].transform.position = headPosition - Vector3.right * length * 0.5f;
                if (!segmentedLongNotes[noteIndex])
                    noteObjects[noteIndex].transform.localScale = new Vector3(
                        length,
                        longNoteWidth,
                        longNoteWidth
                    );
            }
            else
            {
                noteObjects[noteIndex].transform.position = headPosition;
                if (rotateNormalNotes && note.type == "Normal")
                    RotateNormalNote(noteIndex);
            }

            noteObjects[noteIndex].SetActive(true);
        }
    }

    void RotateNormalNote(int noteIndex)
    {
        float minSpeed = Mathf.Max(0f, Mathf.Min(normalRotationSpeed.x, normalRotationSpeed.y));
        float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(normalRotationSpeed.x, normalRotationSpeed.y));
        float speed = Mathf.Lerp(minSpeed, maxSpeed, normalRotationFactors[noteIndex]);

        if (noteObjects[noteIndex].activeInHierarchy)
        {
            normalRotationAngles[noteIndex] = Mathf.Repeat(
                normalRotationAngles[noteIndex] + speed * Time.deltaTime, 360f);
        }

        Quaternion rotation = normalInitialRotations[noteIndex] * Quaternion.AngleAxis(
            normalRotationAngles[noteIndex], normalRotationAxes[noteIndex]);

        Rigidbody body = normalBodies[noteIndex];
        if (body != null && body.isKinematic)
        {
            body.rotation = rotation;
        }
        noteObjects[noteIndex].transform.rotation = rotation;
    }

    Transform GetHitPoint(int lane)
    {
        if (lane == 0)
        {
            return leftHitPoint;
        }
        return rightHitPoint;
    }

    Vector3 GetHitImpulse(int lane)
    {
        Vector3 impulse = hitImpulse;
        if (lane == 0)
        {
            impulse.z = -Mathf.Abs(hitImpulse.z);
        }
        else
        {
            impulse.z = Mathf.Abs(hitImpulse.z);
        }
        return impulse;
    }

    static GameObject PickRandomPrefab(GameObject[] choices, GameObject fallback)
    {
        if (choices == null)
            return fallback;

        int count = 0;
        foreach (GameObject choice in choices)
            if (choice != null)
                count++;

        if (count == 0)
            return fallback;

        int selected = Random.Range(0, count);
        foreach (GameObject choice in choices)
        {
            if (choice == null)
                continue;

            if (selected == 0)
                return choice;

            selected--;
        }

        return fallback;
    }

    void CreateLongNoteSegments(GameObject note, float length, int noteIndex, GameObject selectedPrefab)
    {
        note.transform.localScale = Vector3.one;
        note.transform.localRotation = Quaternion.identity;
        GameObject template = new GameObject("SegmentTemplate");
        template.transform.SetParent(note.transform, false);
        GameObject model = Instantiate(selectedPrefab, template.transform, false);
        model.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.Euler(rotationCompensation);
        template.transform.localScale = Vector3.one * segmentScale;

        if (
            !TryGetModelBounds(template, note.transform, out Bounds bounds)
            || bounds.size.x < 0.0001f
            || length <= 0f
        )
        {
            Debug.LogError("长音模型没有有效长度，请检查 Segment Prefab 和旋转设置。", note);
            Destroy(template);
            return;
        }
        float gap = Mathf.Clamp(segmentSpacing, 0f, bounds.size.x);
        int count = Mathf.Max(1, Mathf.CeilToInt((length + gap) / (bounds.size.x + gap)));
        float fittedLength = (length - gap * (count - 1)) / count;
        GameObject fitted = new GameObject("LongNote_Segment_0");
        fitted.transform.SetParent(note.transform, false);
        template.transform.SetParent(fitted.transform, false);
        template.transform.localPosition = -bounds.center;
        fitted.transform.localScale = new Vector3(fittedLength / bounds.size.x, 1f, 1f);
        fitted.transform.localPosition = new Vector3(-length * 0.5f + fittedLength * 0.5f, 0f, 0f);

        var state = new LongSegmentState();
        state.halfSegmentLength = fittedLength * 0.5f;
        state.segments.Add(fitted.transform);

        for (int i = 1; i < count; i++)
        {
            GameObject segment = Instantiate(fitted, note.transform, false);
            segment.name = $"LongNote_Segment_{i}";
            segment.transform.localPosition =
                fitted.transform.localPosition + Vector3.right * i * (fittedLength + gap);
            state.segments.Add(segment.transform);
        }

        state.nextSegmentIndex = count - 1;
        longSegments[noteIndex] = state;
        Renderer rootRenderer = note.GetComponent<Renderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;
        foreach (Collider collider in note.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in note.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            Destroy(body);
        }
    }

    IEnumerator FlyLongSegments(int noteIndex, GameObject note, LongSegmentState state)
    {
        var chartNote = judge.Chart.notes[noteIndex];

        while (note != null && state.isHolding && state.nextSegmentIndex >= 0)
        {
            Transform segment = state.segments[state.nextSegmentIndex];

            // Break when the leading edge reaches HitPoint.
            double releaseTime =
                chartNote.time + chartNote.duration * 0.5
                - (segment.localPosition.x + state.halfSegmentLength) / approachSpeed;

            if (!judge.Running || judge.SongTime < releaseTime)
            {
                yield return null;
                continue;
            }

            state.nextSegmentIndex--;
            LaunchLongSegment(segment, noteIndex);
            // Spread overdue segments across frames instead of bursting at once.
            yield return null;
        }
    }

    void LaunchLongSegment(Transform segment, int noteIndex)
    {
        var chartNote = judge.Chart.notes[noteIndex];
        Transform hitPoint = GetHitPoint(chartNote.lane);

        // Judgement and spawner Update can run in either order. Synchronize the
        // root before detaching so debris does not start at last frame's position.
        Transform root = segment.parent;
        float remaining = (float)(chartNote.time - judge.SongTime);
        root.position =
            hitPoint.position
            - Vector3.right
                * (remaining * approachSpeed + chartNote.duration * approachSpeed * 0.5f);

        bool hasBounds = TryGetModelBounds(segment.gameObject, segment, out Bounds bounds);
        segment.SetParent(null, true);

        FractureController fracture = segment.GetComponentInChildren<FractureController>();

        if (fracture != null)
        {
            Collider modelCollider = fracture.GetComponent<Collider>();

            if (modelCollider != null)
                modelCollider.enabled = true;

            Physics.SyncTransforms();
            fracture.Fracture();

            Vector3 debrisVelocity = GetHitImpulse(chartNote.lane);

            foreach (Rigidbody debris in segment.GetComponentsInChildren<Rigidbody>())
            {
                debris.AddForce(debrisVelocity, ForceMode.VelocityChange);
            }
            Destroy(segment.gameObject, cleanupDelay);
            return;
        }
        if (hasBounds)
        {
            BoxCollider box = segment.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        Rigidbody rb = segment.gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Vector3 impulse = GetHitImpulse(chartNote.lane);
        rb.AddForce(impulse, ForceMode.Impulse);
        Destroy(segment.gameObject, cleanupDelay);
    }

    static bool TryGetModelBounds(GameObject model, Transform container, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = container.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point =
                    mesh.center
                    + Vector3.Scale(
                        mesh.extents,
                        new Vector3(
                            (corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f,
                            (corner & 4) == 0 ? -1f : 1f
                        )
                    );
                point = matrix.MultiplyPoint3x4(point);
                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                    bounds.Encapsulate(point);
            }
        }
        return found;
    }

    void OnJudged(JudgeEvent result)
    {
        if (noteObjects == null || result.noteIndex < 0 || result.noteIndex >= noteObjects.Length)
            return;

        if (result.kind == NoteKind.Normal && result.phase == JudgePhase.Head)
        {
            HandleNormalNote(result);
        }
        else if (result.kind == NoteKind.Long)
        {
            HandleLongNote(result);
        }
    }

    void HandleNormalNote(JudgeEvent result)
    {
        int noteIndex = result.noteIndex;
        GameObject note = noteObjects[noteIndex];

        if (note == null)
            return;

        if (result.result != JudgeResult.Miss)
        {
            Transform hitPoint = GetHitPoint(result.lane);

            float remaining = (float)(judge.Chart.notes[noteIndex].time - judge.SongTime);
            note.transform.position = hitPoint.position - Vector3.right * remaining * approachSpeed;

            if (result.result == JudgeResult.Perfect)
            {
                PlayHitEffect(perfectHitEffectPrefab);
            }
            else if (result.result == JudgeResult.Good)
            {
                PlayHitEffect(goodHitEffectPrefab);
            }

            FractureController fracture = note.GetComponent<FractureController>();
            if (fracture != null)
            {
                note.SetActive(true);
                // Release the note from movement before replacing it with debris.
                noteObjects[noteIndex] = null;
                Physics.SyncTransforms();
                fracture.Fracture();
                Destroy(note, cleanupDelay);
                return;
            }

            Rigidbody rb = note.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            note.SetActive(true);
            noteObjects[noteIndex] = null;

            rb.isKinematic = false;
            rb.interpolation = normalBodyInterpolation[noteIndex];
            rb.AddForce(GetHitImpulse(result.lane), ForceMode.Impulse);
        }

        Destroy(note, cleanupDelay);
    }

    void HandleLongNote(JudgeEvent result)
    {
        int noteIndex = result.noteIndex;
        GameObject note = noteObjects[noteIndex];

        if (note == null)
            return;

        if (longSegments.TryGetValue(noteIndex, out LongSegmentState state))
        {
            if (result.phase == JudgePhase.Head)
            {
                if (result.result != JudgeResult.Miss && !state.isHolding)
                {
                    note.SetActive(true);
                    state.isHolding = true;
                    if (result.result == JudgeResult.Perfect)
                    {
                        state.hitEffect = PlayHitEffect(longPerfectHitEffectPrefab, 0.1f);
                    }
                    else if (result.result == JudgeResult.Good)
                    {
                        state.hitEffect = PlayHitEffect(longGoodHitEffectPrefab, 0.1f);
                    }
                    StartCoroutine(FlyLongSegments(noteIndex, note, state));
                }
                else if (result.result == JudgeResult.Miss)
                {
                    longSegments.Remove(noteIndex);
                    Destroy(note, cleanupDelay);
                }
            }
            else if (result.phase == JudgePhase.Tail)
            {
                state.isHolding = false;
                Destroy(state.hitEffect);

                if (result.result != JudgeResult.Miss)
                {
                    while (state.nextSegmentIndex >= 0)
                    {
                        Transform segment = state.segments[state.nextSegmentIndex];
                        state.nextSegmentIndex--;

                        if (segment != null)
                        {
                            LaunchLongSegment(segment, noteIndex);
                        }
                    }
                }

                longSegments.Remove(noteIndex);
                Destroy(note, cleanupDelay);
            }

            return;
        }

        if (result.phase == JudgePhase.Head)
        {
            if (result.result == JudgeResult.Miss)
                Destroy(note, cleanupDelay);

            return;
        }

        if (result.phase != JudgePhase.Tail)
            return;

        if (result.result != JudgeResult.Miss)
        {
            Rigidbody rb = note.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            noteObjects[noteIndex] = null;
            rb.isKinematic = false;

            rb.AddForce(GetHitImpulse(result.lane), ForceMode.Impulse);
        }

        Destroy(note, cleanupDelay);
    }

    GameObject PlayHitEffect(GameObject effectPrefab, float distanceBehindBall = 0.65f)
    {
        if (effectPrefab == null || hitBall == null)
        {
            return null;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        Vector3 effectPosition = hitBall.position;
        effectPosition += mainCamera.transform.forward * distanceBehindBall;

        GameObject effect = Instantiate(
            effectPrefab,
            effectPosition,
            mainCamera.transform.rotation);

        effect.transform.SetParent(hitBall, true);
        return effect;
    }

    void OnDestroy()
    {
        if (judge != null)
            judge.Judged -= OnJudged;

        foreach (LongSegmentState state in longSegments.Values)
        {
            Destroy(state.hitEffect);
        }
    }
}
