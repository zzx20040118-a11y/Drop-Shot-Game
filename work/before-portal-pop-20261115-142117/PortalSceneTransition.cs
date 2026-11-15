using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DropShot.Judgement;

public class PortalSceneTransition : MonoBehaviour
{
    [Header("传送门")]
    public GameObject portal;
    public BoxCollider portalCollider;

    [Header("传送门移动")]
    public float portalMoveSpeed = 10f;
    [Min(0.01f)] public float finishApproachSeconds = 0.35f;

    [Header("收尾")]
    public float sceneLoadDelay = 0f;
    public bool waitForMusicEnd = true;

    [Header("球")]
    public Collider flyingBallCollider;
    public Collider rollingBallCollider;

    [Header("判定系统")]
    public JudgementSystem judgement;

    [Header("场景")]
    public string nextSceneName;

    bool portalActivated;
    bool sceneLoading;
    bool finalStrikeResolved;
    bool finalStrikeSucceeded;
    int finalStrikeIndex = -1;
    ChartNote finalStrike;
    BallStrikeFlight flight;
    Vector3 approachStart;
    Vector3 finishStart;
    float finishStartedAt;

    void Start()
    {
        if (portal != null) portal.SetActive(false);
        if (portalCollider != null) portalCollider.enabled = false;
        if (flyingBallCollider != null)
            flight = flyingBallCollider.GetComponent<BallStrikeFlight>();
        if (judgement == null || judgement.Chart == null) return;

        for (int i = 0; i < judgement.Chart.notes.Count; i++)
        {
            ChartNote note = judgement.Chart.notes[i];
            if (NoteKindParser.Parse(note.type) == NoteKind.Strike
                && (finalStrike == null || note.time > finalStrike.time))
            {
                finalStrike = note;
                finalStrikeIndex = i;
            }
        }
        judgement.StrikeAutoStarted += OnStrikeAutoStarted;
        judgement.Judged += OnJudged;
    }

    void OnStrikeAutoStarted(int noteIndex)
    {
        if (noteIndex == finalStrikeIndex) ActivatePortal();
    }

    public void ActivatePortal()
    {
        if (portalActivated) return;
        portalActivated = true;
        approachStart = transform.position;
        if (portal != null) portal.SetActive(true);
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.noteIndex != finalStrikeIndex || result.kind != NoteKind.Strike
            || result.phase != JudgePhase.Release) return;
        finalStrikeResolved = true;
        finalStrikeSucceeded = result.result != JudgeResult.Miss;
        finishStart = transform.position;
        finishStartedAt = Time.unscaledTime;
        if (portalCollider != null) portalCollider.enabled = true;
        if (!finalStrikeSucceeded && flight != null) flight.CancelCharge();
    }

    void Update()
    {
        if (!portalActivated || sceneLoading || judgement == null) return;
        if (!finalStrikeResolved)
        {
            // Approach on the same song clock as the shrinking ring, even in slow motion.
            if (finalStrike != null && rollingBallCollider != null)
            {
                float duration = Mathf.Max(0.01f, finalStrike.targetTime - finalStrike.time);
                float progress = Mathf.Clamp01((float)(judgement.SongTime - finalStrike.time) / duration);
                transform.position = Vector3.Lerp(approachStart,
                    ContactPosition(rollingBallCollider), progress);
            }
            else
                transform.position += Vector3.right * portalMoveSpeed * Time.deltaTime;
            return;
        }

        bool flying = finalStrikeSucceeded && flight != null
            && flight.currentState == BallStrikeFlight.BallState.Flight;
        Collider ball = flying ? flyingBallCollider : rollingBallCollider;
        if (ball == null || portalCollider == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - finishStartedAt)
            / Mathf.Max(0.01f, finishApproachSeconds));
        transform.position = Vector3.Lerp(finishStart, ContactPosition(ball), Mathf.SmoothStep(0f, 1f, t));
        if (!CheckBallContact(ball)) return;

        sceneLoading = true;
        StartCoroutine(CompleteScene(flying));
    }

    Vector3 ContactPosition(Collider ball)
    {
        if (portalCollider == null) return transform.position;
        Vector3 centreOffset = portalCollider.transform.TransformPoint(portalCollider.center)
            - transform.position;
        return ball.bounds.center - centreOffset;
    }

    bool CheckBallContact(Collider ball)
    {
        if (ball == null || !ball.enabled || !ball.gameObject.activeInHierarchy
            || portalCollider == null || !portalCollider.enabled) return false;
        Physics.SyncTransforms();
        return Physics.ComputePenetration(portalCollider, portalCollider.transform.position,
            portalCollider.transform.rotation, ball, ball.transform.position,
            ball.transform.rotation, out _, out _);
    }

    IEnumerator CompleteScene(bool flying)
    {
        if (!flying && sceneLoadDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneLoadDelay);
        AudioSource music = judgement != null ? judgement.music : null;
        if (waitForMusicEnd && music != null && music.clip != null && !music.loop)
            while (music != null && music.isPlaying)
                yield return null;

        if (judgement != null && judgement.Core != null)
            DropShot.UI.ScoreBoard.Record(judgement.Core);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        if (judgement == null) return;
        judgement.StrikeAutoStarted -= OnStrikeAutoStarted;
        judgement.Judged -= OnJudged;
    }
}
