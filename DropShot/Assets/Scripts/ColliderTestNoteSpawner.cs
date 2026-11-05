using DropShot.Judgement;
using UnityEngine;

public class ColliderTestNoteSpawner : MonoBehaviour
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

    [Header("长音分段模型（留空则使用原长条）")]
    public GameObject segmentPrefab;
    [Min(0f)] public float segmentSpacing = 0.02f;
    public Vector3 rotationCompensation = Vector3.zero;
    [Min(0.01f)] public float segmentScale = 1f;

    private GameObject[] blocks;

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

                if (segmentPrefab != null)
                    CreateLongNoteSegments(blocks[i], length);
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
                if (segmentPrefab == null)
                    blocks[i].transform.localScale = new Vector3(length, longNoteWidth, longNoteWidth);
            }
            else
            {
                blocks[i].transform.position = headPosition;
            }

            blocks[i].SetActive(true);
        }
    }

    void CreateLongNoteSegments(GameObject note, float length)
    {
        note.transform.localScale = Vector3.one;
        note.transform.localRotation = Quaternion.identity;

        // 用包装物体旋转模型，保留导入模型内部的缩放和坐标。
        GameObject template = new GameObject("SegmentTemplate");
        template.transform.SetParent(note.transform, false);
        GameObject model = Instantiate(segmentPrefab, template.transform, false);
        model.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.Euler(rotationCompensation);
        template.transform.localScale = Vector3.one * segmentScale;

        if (!TryGetModelBounds(template, note.transform, out Bounds bounds) || bounds.size.x < 0.0001f || length <= 0f)
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

        for (int i = 1; i < count; i++)
        {
            GameObject segment = Instantiate(fitted, note.transform, false);
            segment.name = $"LongNote_Segment_{i}";
            segment.transform.localPosition = fitted.transform.localPosition
                + Vector3.right * i * (fittedLength + gap);
        }

        // 根物体负责整条长音的碰撞与弹飞，外观不再整体拉伸。
        Renderer rootRenderer = note.GetComponent<Renderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;
        BoxCollider box = note.GetComponent<BoxCollider>();
        if (box == null) box = note.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(length, bounds.size.y, bounds.size.z);

        Rigidbody rb = note.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = note.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    static bool TryGetModelBounds(GameObject model, Transform container, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = container.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = mesh.center + Vector3.Scale(mesh.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                point = matrix.MultiplyPoint3x4(point);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
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
                Rigidbody rb = note.GetComponent<Rigidbody>();
                if (rb == null)
                    return;

                note.SetActive(true);
                blocks[i] = null;

                rb.isKinematic = false;
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
