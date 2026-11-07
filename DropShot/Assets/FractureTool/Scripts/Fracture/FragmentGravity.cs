using UnityEngine;

namespace FractureTool
{
    /// <summary>
    /// Applies a custom gravity scale to a fragment. Lives on the fragment itself,
    /// so it keeps working after the original object has been destroyed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FragmentGravity : MonoBehaviour
    {
        public float gravityScale = 1f;

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (body.useGravity && !body.isKinematic)
                body.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
        }
    }
}
