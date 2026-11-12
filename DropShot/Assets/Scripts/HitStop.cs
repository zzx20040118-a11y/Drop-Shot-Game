using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine hitStopCoroutine;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Stop(float duration = 0.1f)
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        else
        {
            previousTimeScale = Time.timeScale;
        }

        Time.timeScale = 0f;
        hitStopCoroutine = StartCoroutine(ResumeAfterStop(duration));
    }

    private IEnumerator ResumeAfterStop(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = previousTimeScale;
        hitStopCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
