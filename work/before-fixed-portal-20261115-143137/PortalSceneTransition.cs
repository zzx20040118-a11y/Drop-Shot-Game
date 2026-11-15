using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DropShot.Judgement;

public class PortalSceneTransition : MonoBehaviour
{
    [Header("传送门")]
    public GameObject portal;
    public BoxCollider portalCollider;

    [Header("出现动画")]
    [Min(0f)] public float portalPreviewSeconds = 0.8f;
    [Min(0.01f)] public float appearSeconds = 0.45f;
    [Range(0f, 0.25f)] public float appearOvershoot = 0.08f;

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
        approachStart = transform.position;
        if (portal != null)
        {
            portal.transform.localScale = Vector3.zero;
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
            portal.transform.localScale = portalBaseScale * size;
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
        finishStart = transform.position;
        finishStartedAt = Time.unscaledTime;
        if (portalCollider != null) portalCollider.enabled = true;
        if (!finalStrikeSucceeded && flight != null) flight.CancelCharge();
    }

    void Update()
    {
        if (!portalActivated && judgement != null && judgement.Running && finalStrike != null
            && judgement.SongTime >= finalStrike.time - portalPreviewSeconds)
            ActivatePortal();
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
