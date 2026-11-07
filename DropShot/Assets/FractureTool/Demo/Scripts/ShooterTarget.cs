using System.Collections;
using UnityEngine;

namespace FractureTool.Demo
{
    /// <summary>
    /// A breakable target for the shooting range. Pops in, can slide sideways and escapes after a while.
    /// </summary>
    [RequireComponent(typeof(FractureController))]
    public class ShooterTarget : MonoBehaviour
    {
        public int points = 10;
        [Tooltip("Height from the pivot to the bottom of the target, used to place it on a shelf.")]
        public float baseOffset = 0.5f;
        public float moveAmplitude = 0f;
        public float moveSpeed = 1.2f;
        public float lifetime = 7f;

        [HideInInspector] public ShooterGame game;

        FractureController controller;
        Vector3 origin;
        Vector3 fullScale;
        float phase;
        float spawnTime;
        bool leaving;

        void Awake()
        {
            controller = GetComponent<FractureController>();
            fullScale = transform.localScale;
        }

        void Start()
        {
            origin = transform.position;
            phase = Random.value * Mathf.PI * 2f;
            spawnTime = Time.time;
            StartCoroutine(Scale(0.01f, 1f, 0.2f, false));
        }

        void Update()
        {
            if (controller.HasFractured || leaving) return;

            if (moveAmplitude > 0f)
                transform.position = origin + transform.right * Mathf.Sin(Time.time * moveSpeed + phase) * moveAmplitude;

            if (Time.time - spawnTime > lifetime)
                Expire();
        }

        /// <summary>Breaks the target and reports the score.</summary>
        public void Hit(Vector3 point)
        {
            if (controller.HasFractured || leaving) return;

            controller.Fracture();
            if (game != null) game.OnTargetBroken(this, point);
        }

        /// <summary>Removes the target without breaking it.</summary>
        public void Expire()
        {
            if (leaving || controller.HasFractured) return;

            leaving = true;
            if (game != null) game.OnTargetExpired(this);
            StartCoroutine(Scale(1f, 0.01f, 0.2f, true));
        }

        IEnumerator Scale(float from, float to, float duration, bool destroyAtEnd)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (controller.HasFractured) yield break;
                transform.localScale = fullScale * Mathf.SmoothStep(from, to, t);
                yield return null;
            }

            transform.localScale = fullScale * to;
            if (destroyAtEnd) Destroy(gameObject);
        }
    }
}
