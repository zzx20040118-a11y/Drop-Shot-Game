using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DropShot.Judgement;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    [Range(0f, 0.95f)] public float shrinkStart = 0.72f;
    [Range(0f, 0.85f)] public float cameraFollowAmount = 0.65f;

    [Header("收尾")]
    public float sceneLoadDelay = 0f;
    public bool waitForMusicEnd = true;
    [Min(0f)] public float afterEntryPause = 0.2f;
    [Min(0.01f)] public float fadeOutSeconds = 0.3f;

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
    Vector3 ballEntryDestination;
    Vector3 ballEntryScale;
    Collider enteringBall;
    bool enteringFlying;
    float finishStartedAt;
    Vector3 portalBaseScale;
    Camera entryCamera;
    Vector3 cameraEntryStart;
    Quaternion cameraEntryRotation;
    bool entryTrailCleared;

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
        entryCamera = flight != null && flight.ballCamera != null
            ? flight.ballCamera.GetComponent<Camera>() : Camera.main;
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
        CentrePortal();
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
        // Hand the final camera move to this script instead of the normal chase.
        if (flight != null && flight.ballCamera != null)
            flight.ballCamera.enabled = false;
        CentrePortal();
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
            ballEntryScale = enteringBall.transform.localScale;
            // Aim at the visible hole, not the offset collision box.
            ballEntryDestination = portal.transform.position;
            if (entryCamera != null)
            {
                cameraEntryStart = entryCamera.transform.position;
                cameraEntryRotation = entryCamera.transform.rotation;
            }
            if (enteringFlying) ClearTrails(rollingBallCollider);
            finishStartedAt = Time.unscaledTime;
            if (enteringFlying) flight.enabled = false;
            BallMove move = enteringBall.GetComponent<BallMove>();
            if (move != null) move.enabled = false;
            BallLaneControl lanes = enteringBall.GetComponent<BallLaneControl>();
            if (lanes != null) lanes.enabled = false;
            BallLaneFollow follow = enteringBall.GetComponent<BallLaneFollow>();
            if (follow != null) follow.enabled = false;
        }

        float t = Mathf.Clamp01((Time.unscaledTime - finishStartedAt) / Mathf.Max(0.01f, ballEnterSeconds));
        float travel = Mathf.Lerp(t, t * t, 0.5f);
        enteringBall.transform.position = Vector3.Lerp(ballEntryStart, ballEntryDestination, travel);
        float shrink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(shrinkStart, 1f, t));
        enteringBall.transform.localScale = ballEntryScale * Mathf.Max(0.001f, 1f - shrink);
        if (!entryTrailCleared && t >= shrinkStart)
        {
            ClearTrails(enteringBall);
            entryTrailCleared = true;
        }
        if (t < 1f) return;

        foreach (Renderer renderer in enteringBall.GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
        foreach (ParticleSystem particles in enteringBall.GetComponentsInChildren<ParticleSystem>())
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        sceneLoading = true;
        StartCoroutine(CompleteScene(enteringFlying));
    }

    void CentrePortal()
    {
        if (entryCamera == null || portal == null) return;
        Vector3 screen = entryCamera.WorldToViewportPoint(portal.transform.position);
        if (screen.z <= 0f) return;
        Vector3 centred = entryCamera.ViewportToWorldPoint(new Vector3(0.5f, screen.y, screen.z));
        transform.position += centred - portal.transform.position;
    }

    void LateUpdate()
    {
        if (enteringBall == null || entryCamera == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - finishStartedAt) / Mathf.Max(0.01f, ballEnterSeconds));
        float travel = Mathf.Lerp(t, t * t, 0.5f);
        // Follow part of the journey so the camera never crosses the hole.
        entryCamera.transform.position = cameraEntryStart
            + (ballEntryDestination - ballEntryStart) * (travel * cameraFollowAmount);
        Vector3 direction = portal.transform.position - entryCamera.transform.position;
        if (direction.sqrMagnitude > 0.001f)
            entryCamera.transform.rotation = Quaternion.Slerp(cameraEntryRotation,
                Quaternion.LookRotation(direction, Vector3.up), Mathf.SmoothStep(0f, 1f, t));
    }

    void ClearTrails(Collider ball)
    {
        if (ball == null) return;
        foreach (ParticleSystem particles in ball.GetComponentsInChildren<ParticleSystem>(true))
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (TrailRenderer trail in ball.GetComponentsInChildren<TrailRenderer>(true))
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    IEnumerator CompleteScene(bool flying)
    {
        if (afterEntryPause > 0f)
            yield return new WaitForSecondsRealtime(afterEntryPause);
        if (!flying && sceneLoadDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneLoadDelay);
        AudioSource music = judgement != null ? judgement.music : null;
        if (waitForMusicEnd && music != null && music.clip != null && !music.loop)
            while (music != null && music.isPlaying)
                yield return null;

        if (judgement != null && judgement.Core != null)
            DropShot.UI.ScoreBoard.Record(judgement.Core);

        GameObject overlay = new GameObject("Ending Fade", typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Color.black;
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.01f, fadeOutSeconds))
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeOutSeconds));
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        yield return SceneManager.LoadSceneAsync(nextSceneName);
    }

    void OnDestroy()
    {
        if (judgement == null) return;
        judgement.StrikeAutoStarted -= OnStrikeAutoStarted;
        judgement.Judged -= OnJudged;
    }
}
