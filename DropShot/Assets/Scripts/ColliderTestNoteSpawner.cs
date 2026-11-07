using System.Collections;
using System.Collections.Generic;
using DropShot.Judgement;
using FractureTool;
using UnityEngine;

public class ColliderTestNoteSpawner : MonoBehaviour
{
    [Header("判定系统")]
    public JudgementSystem judge;

    [Header("音符 Prefab")]
    public GameObject normalNotePrefab;
    public GameObject longNotePrefab;

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

    private GameObject[] blocks;
    private bool[] segmentedLongNotes;
    private Vector3[] normalRotationAxes;
    private float[] normalRotationFactors;
    private float[] normalRotationAngles;
    private Quaternion[] normalInitialRotations;
    private Rigidbody[] normalBodies;
    private RigidbodyInterpolation[] normalBodyInterpolation;

    class LongSegmentState
    {
        public readonly List<Transform> segments = new List<Transform>();
        public int next;
        public bool holding;
    }

    private readonly Dictionary<int, LongSegmentState> longSegments =
        new Dictionary<int, LongSegmentState>();

    [Header("短音彈飛")]
    public Vector3 hitImpulse = new Vector3(5f, 5f, 0f);
    public float cleanupDelay = 3f;

    void Start()
    {
        if (judge == null || judge.Chart == null)
        {
            Debug.LogError("BeatmapNoteSpawner: JudgementSystem 或 Chart 未设置！");
            return;
        }

        var notes = judge.Chart.notes;
        blocks = new GameObject[notes.Count];
        segmentedLongNotes = new bool[notes.Count];
        normalRotationAxes = new Vector3[notes.Count];
        normalRotationFactors = new float[notes.Count];
        normalRotationAngles = new float[notes.Count];
        normalInitialRotations = new Quaternion[notes.Count];
        normalBodies = new Rigidbody[notes.Count];
        normalBodyInterpolation = new RigidbodyInterpolation[notes.Count];

        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];

            if (note.type == "Normal")
            {
                GameObject prefab = PickRandomPrefab(normalNotePrefabs, normalNotePrefab);
                if (prefab == null)
                {
                    Debug.LogWarning("短音没有可用 Prefab，请设置 Normal Note Prefabs 或 Normal Note Prefab。", this);
                    continue;
                }
                blocks[i] = Instantiate(prefab);
                normalInitialRotations[i] = blocks[i].transform.rotation;
                normalBodies[i] = blocks[i].GetComponent<Rigidbody>();
                if (normalBodies[i] != null)
                {
                    normalBodyInterpolation[i] = normalBodies[i].interpolation;
                    // Approach movement is driven by the song clock, not physics.
                    normalBodies[i].interpolation = RigidbodyInterpolation.None;
                }
                normalRotationAxes[i] = Random.onUnitSphere;
                normalRotationFactors[i] = Random.value;
                blocks[i].SetActive(false);
            }
            else if (note.type == "Long")
            {
                blocks[i] = Instantiate(longNotePrefab);
                blocks[i].SetActive(false);

                // 长条长度 = 持续时间 × 音符移动速度
                float length = note.duration * approachSpeed;

                GameObject selectedSegment = PickRandomPrefab(segmentPrefabs, segmentPrefab);
                if (selectedSegment != null)
                {
                    CreateLongNoteSegments(blocks[i], length, i, selectedSegment);
                    segmentedLongNotes[i] = longSegments.ContainsKey(i);
                }
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

            Transform hitPoint = note.lane == 0 ? leftHitPoint : rightHitPoint;

            Vector3 headPosition = hitPoint.position - Vector3.right * remaining * approachSpeed;

            if (note.type == "Long")
            {
                float length = note.duration * approachSpeed;

                // 头部在正 X 端；尾部在 duration 秒后通过判定点。
                blocks[i].transform.position = headPosition - Vector3.right * length * 0.5f;

                // 长条沿 X 轴延伸
                if (!segmentedLongNotes[i])
                    blocks[i].transform.localScale = new Vector3(
                        length,
                        longNoteWidth,
                        longNoteWidth
                    );
            }
            else
            {
                blocks[i].transform.position = headPosition;
                if (rotateNormalNotes && note.type == "Normal")
                {
                    // Read the range live so UI changes also affect existing candy.
                    float minSpeed = Mathf.Max(0f, Mathf.Min(normalRotationSpeed.x, normalRotationSpeed.y));
                    float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(normalRotationSpeed.x, normalRotationSpeed.y));
                    float speed = Mathf.Lerp(minSpeed, maxSpeed, normalRotationFactors[i]);
                    // Start at the prefab's orientation and accumulate only
                    // while visible, avoiding a clock-based jump on appearance.
                    if (blocks[i].activeInHierarchy)
                        normalRotationAngles[i] = Mathf.Repeat(
                            normalRotationAngles[i] + speed * Time.deltaTime,
                            360f);
                    Quaternion rotation = normalInitialRotations[i] * Quaternion.AngleAxis(
                        normalRotationAngles[i], normalRotationAxes[i]);
                    if (normalBodies[i] != null && normalBodies[i].isKinematic)
                        normalBodies[i].rotation = rotation;
                    blocks[i].transform.rotation = rotation;
                }
            }

            blocks[i].SetActive(true);
        }
    }

    // Ignore empty slots and keep existing scene references as a fallback.
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
            if (choice != null && selected-- == 0)
                return choice;

        return fallback;
    }

    void CreateLongNoteSegments(GameObject note, float length, int noteIndex, GameObject selectedPrefab)
    {
        note.transform.localScale = Vector3.one;
        note.transform.localRotation = Quaternion.identity;

        // 用包装物体旋转模型，保留导入模型内部的缩放和坐标。
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

        // 按旋转后的实际边界计算，补偿模型中心偏移，避免头尾超出判定时间。
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
        state.segments.Add(fitted.transform);

        for (int i = 1; i < count; i++)
        {
            GameObject segment = Instantiate(fitted, note.transform, false);
            segment.name = $"LongNote_Segment_{i}";
            segment.transform.localPosition =
                fitted.transform.localPosition + Vector3.right * i * (fittedLength + gap);
            state.segments.Add(segment.transform);
        }

        state.next = count - 1;
        longSegments[noteIndex] = state;

        // 根物体只负责移动；分段经过判定点后各自弹飞。
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

        while (note != null && state.holding && state.next >= 0)
        {
            Transform segment = state.segments[state.next];

            // Local X already includes each segment's fitted width and spacing.
            // Its center reaches HitPoint at this song time.
            double releaseTime =
                chartNote.time + chartNote.duration * 0.5 - segment.localPosition.x / approachSpeed;

            if (!judge.Running || judge.SongTime < releaseTime)
            {
                yield return null;
                continue;
            }

            state.next--;
            LaunchLongSegment(segment, noteIndex);
            // Spread overdue segments across frames instead of bursting at once.
            yield return null;
        }
    }

    void LaunchLongSegment(Transform segment, int noteIndex)
    {
        var chartNote = judge.Chart.notes[noteIndex];
        Transform hitPoint = chartNote.lane == 0 ? leftHitPoint : rightHitPoint;

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

            Vector3 debrisVelocity = hitImpulse;
            debrisVelocity.z =
                chartNote.lane == 0 ? -Mathf.Abs(hitImpulse.z) : Mathf.Abs(hitImpulse.z);


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

        Vector3 impulse = hitImpulse;
        impulse.z = chartNote.lane == 0 ? -Mathf.Abs(hitImpulse.z) : Mathf.Abs(hitImpulse.z);
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
        if (blocks == null)
            return;

        if (result.kind == NoteKind.Normal && result.phase == JudgePhase.Head)
        {
            int i = result.noteIndex;
            GameObject note = blocks[i];

            if (note == null)
                return;

            if (result.result != JudgeResult.Miss)
            {
                FractureController fracture = note.GetComponent<FractureController>();
                if (fracture != null)
                {
                    note.SetActive(true);
                    // Release the note from movement before replacing it with debris.
                    blocks[i] = null;
                    Physics.SyncTransforms();
                    fracture.Fracture();
                    Destroy(note, cleanupDelay);
                    return;
                }

                Rigidbody rb = note.GetComponent<Rigidbody>();
                if (rb == null)
                    return;

                note.SetActive(true);
                blocks[i] = null;

                rb.isKinematic = false;
                rb.interpolation = normalBodyInterpolation[i];
                Vector3 impulse = hitImpulse;

                if (result.lane == 0)
                {
                    impulse.z = -Mathf.Abs(hitImpulse.z);
                }
                else
                {
                    impulse.z = Mathf.Abs(hitImpulse.z);
                }

                rb.AddForce(impulse, ForceMode.Impulse);
            }

            Destroy(note, cleanupDelay);
        }

        if (result.kind == NoteKind.Long)
        {
            int i = result.noteIndex;
            GameObject note = blocks[i];

            if (note == null)
                return;

            if (longSegments.TryGetValue(i, out LongSegmentState state))
            {
                if (result.phase == JudgePhase.Head)
                {
                    if (result.result != JudgeResult.Miss && !state.holding)
                    {
                        note.SetActive(true);
                        state.holding = true;
                        StartCoroutine(FlyLongSegments(i, note, state));
                    }
                    else if (result.result == JudgeResult.Miss)
                    {
                        longSegments.Remove(i);
                        Destroy(note, cleanupDelay);
                    }
                }
                else if (result.phase == JudgePhase.Tail)
                {
                    state.holding = false;

                    longSegments.Remove(i);
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

                blocks[i] = null;
                rb.isKinematic = false;

                Vector3 impulse = hitImpulse;
                impulse.z = Mathf.Abs(hitImpulse.z);

                if (result.lane == 0)
                    impulse.z = -impulse.z;

                rb.AddForce(impulse, ForceMode.Impulse);
            }

            Destroy(note, cleanupDelay);
        }
    }

    void OnDestroy()
    {
        if (judge != null)
            judge.Judged -= OnJudged;
    }
}
