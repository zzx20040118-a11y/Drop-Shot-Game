using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DropShot.Judgement;
using UnityEngine.Serialization;

public class PortalSceneTransition : MonoBehaviour
{
    [Header("传送门")]
    public GameObject portal;
    public BoxCollider portalCollider;

    [Header("出现动画")]
    [Min(0f)] public float portalPreviewSeconds = 0.8f;
    [Min(0.01f)] public float appearSeconds = 0.45f;
    [Range(0f, 0.25f)] public float appearOvershoot = 0.08f;

    [Header("球进入黑洞")]
    [FormerlySerializedAs("finishApproachSeconds")]
    [Min(0.01f)] public float ballEnterSeconds = 1.5f;

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
    Vector3 ballEntryStart;
    Collider enteringBall;
    bool enteringFlying;
    float finishStartedAt;
    Vector3 portalBaseScale;

    void Start()
    {
        if (portal != null)
        {
            portalBaseScale = portal.transform.localScale;
            foreach (ParticleSystem particles in portal.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.useUnscaledTime = true;
            }
            portal.SetActive(false);
        }
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
        if (portal != null)
        {
            portal.transform.localScale = portalBaseScale * 0.001f;
            portal.SetActive(true);
            StartCoroutine(PopPortal());
        }
    }

    IEnumerator PopPortal()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, appearSeconds);
        while (portal != null && elapsed < duration)
        {
            float t = elapsed / duration;
            float size = t < 0.7f
                ? Mathf.Lerp(0f, 1f + appearOvershoot, 1f - Mathf.Pow(1f - t / 0.7f, 3f))
                : Mathf.Lerp(1f + appearOvershoot, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f));
            portal.transform.localScale = portalBaseScale * Mathf.Max(0.001f, size);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (portal != null) portal.transform.localScale = portalBaseScale;
    }

    void OnJudged(JudgeEvent result)
    {
        if (result.noteIndex != finalStrikeIndex || result.kind != NoteKind.Strike
            || result.phase != JudgePhase.Release) return;
        finalStrikeResolved = true;
        finalStrikeSucceeded = result.result != JudgeResult.Miss;
        if (portalCollider != null) portalCollider.enabled = true;
        if (!finalStrikeSucceeded && flight != null) flight.CancelCharge();
    }

    void Update()
    {
        if (!portalActivated && judgement != null && judgement.Running && finalStrike != null
            && judgement.SongTime >= finalStrike.time - portalPreviewSeconds)
            ActivatePortal();
        if (!portalActivated || sceneLoading || judgement == null) return;
        if (!finalStrikeResolved) return;
        if (enteringBall == null)
        {
            enteringFlying = finalStrikeSucceeded && flight != null
                && flight.currentState == BallStrikeFlight.BallState.Flight;
            enteringBall = enteringFlying ? flyingBallCollider : rollingBallCollider;
            if (enteringBall == null || portalCollider == null) return;
            ballEntryStart = enteringBall.transform.position;
            finishStartedAt = Time.unscaledTime;
            if (enteringFlying) flight.enabled = false;
            BallMove move = enteringBall.GetComponent<BallMove>();
            if (move != null) move.enabled = false;
            BallLaneControl lanes = enteringBall.GetComponent<BallLaneControl>();
            if (lanes != null) lanes.enabled = false;
            BallLaneFollow follow = enteringBall.GetComponent<BallLaneFollow>();
            if (follow != null) follow.enabled = false;
        }

        Vector3 centreOffset = enteringBall.bounds.center - enteringBall.transform.position;
        Vector3 destination = portalCollider.transform.TransformPoint(portalCollider.center) - centreOffset;
        float t = Mathf.Clamp01((Time.unscaledTime - finishStartedAt) / Mathf.Max(0.01f, ballEnterSeconds));
        enteringBall.transform.position = Vector3.Lerp(ballEntryStart, destination, Mathf.SmoothStep(0f, 1f, t));
        if (t < 1f || !CheckBallContact(enteringBall)) return;

        sceneLoading = true;
        StartCoroutine(CompleteScene(enteringFlying));
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
