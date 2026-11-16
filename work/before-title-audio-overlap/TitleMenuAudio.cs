using UnityEngine;

public class TitleMenuAudio : MonoBehaviour
{
    public AudioClip highlightClip;
    public AudioClip clickClip;
    public AudioClip successClip;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Range(0f, 1f)] public float hoverVolume = 0.4f;
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

    public void PlayHover()
    {
        if (!starting && highlightClip != null)
            source.PlayOneShot(highlightClip, volume * hoverVolume);
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
        if (successClip == null) return;

        // Let the start sound finish even if the next scene loads quickly.
        GameObject sound = new GameObject("Title Start Sound");
        DontDestroyOnLoad(sound);
        AudioSource startSource = sound.AddComponent<AudioSource>();
        startSource.playOnAwake = false;
        startSource.spatialBlend = 0f;
        startSource.ignoreListenerPause = true;
        startSource.clip = successClip;
        startSource.volume = volume;
        float delay = Mathf.Max(0f, startSoundDelay);
        startSource.PlayDelayed(delay);
        Destroy(sound, delay + successClip.length + 0.1f);
    }
}
