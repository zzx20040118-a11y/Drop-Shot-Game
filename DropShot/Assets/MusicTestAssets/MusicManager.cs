using UnityEngine;
using UnityEngine.InputSystem;

public class MusicManager : MonoBehaviour
{
    public AudioSource musicSource;

    public bool IsPlaying { get; private set; }

    public float MusicTime => musicSource != null ? musicSource.time : 0f;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            !IsPlaying)
        {
            StartMusic();
        }
    }

    public void StartMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Play();
        IsPlaying = true;
    }
}
