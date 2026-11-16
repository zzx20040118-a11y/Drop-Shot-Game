using UnityEngine;
using System.Collections;

public class TitleMenuAudio : MonoBehaviour
{
    public AudioClip clickClip;
    public AudioClip successClip;
    public AudioClip openingClip;
    public AudioSource backgroundMusic;
    [Range(0f, 1f)] public float animationMusicVolume = 0.08f;
    [Min(0f)] public float musicFadeSeconds = 0.2f;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Range(0f, 1f)] public float successVolume = 1f;
    [Min(0f)] public float startSoundDelay = 0.2f;

    AudioSource source;
    bool starting;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
    }

    public void PlayClick(bool startButton)
    {
        if (starting) return;
        if (!startButton)
        {
            if (clickClip != null) source.PlayOneShot(clickClip, volume);
            return;
        }

        starting = true;
        source.Stop();
        StartCoroutine(LowerMusic());
        PlayStartSound(successClip, 0f, successVolume);
        PlayStartSound(openingClip, startSoundDelay, volume);
    }

    IEnumerator LowerMusic()
    {
        if (backgroundMusic == null) yield break;
        float originalVolume = backgroundMusic.volume;
        float targetVolume = Mathf.Min(originalVolume, animationMusicVolume);
        float elapsed = 0f;
        while (elapsed < musicFadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            backgroundMusic.volume = Mathf.Lerp(originalVolume, targetVolume,
                Mathf.Clamp01(elapsed / musicFadeSeconds));
            yield return null;
        }
        backgroundMusic.volume = targetVolume;
    }

    void PlayStartSound(AudioClip clip, float soundDelay, float soundVolume)
    {
        if (clip == null) return;

        // Let the start sound finish even if the next scene loads quickly.
        GameObject sound = new GameObject("Title Sound (" + clip.name + ")");
        DontDestroyOnLoad(sound);
        AudioSource startSource = sound.AddComponent<AudioSource>();
        startSource.playOnAwake = false;
        startSource.spatialBlend = 0f;
        startSource.ignoreListenerPause = true;
        startSource.clip = clip;
        startSource.volume = soundVolume;
        float delay = Mathf.Max(0f, soundDelay);
        if (delay > 0f) startSource.PlayDelayed(delay);
        else startSource.Play();
        Destroy(sound, delay + clip.length + 0.1f);
    }
}
