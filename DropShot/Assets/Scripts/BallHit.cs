using UnityEngine;
using UnityEngine.InputSystem;

public class BallHit : MonoBehaviour
{
    public AudioSource hitAudio;
    public bool readKeyboard = true;
    public float hitSize = 1.2f;
    public float recoverSpeed = 8f;

    private Vector3 normalScale;

    void Start()
    {
        normalScale = transform.localScale;
    }

    void Update()
    {
       
        transform.localScale = Vector3.MoveTowards(
            transform.localScale,
            normalScale,
            recoverSpeed * Time.deltaTime
        );

        if (Keyboard.current == null)
        {
            return;
        }

        if (readKeyboard && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Hit();
        }
    }

    public void Hit()
    {
        transform.localScale = normalScale * hitSize;
        if (hitAudio != null && hitAudio.clip != null)
        {
            hitAudio.PlayOneShot(hitAudio.clip);
        }
    }
}
