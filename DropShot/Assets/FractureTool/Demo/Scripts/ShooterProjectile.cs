using UnityEngine;

namespace FractureTool.Demo
{
    /// <summary>
    /// Physical projectile: breaks what it hits through the FractureController API and pushes nearby debris.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShooterProjectile : MonoBehaviour
    {
        public float impactForce = 3f;
        public float impactRadius = 0.8f;

        bool used;

        void OnCollisionEnter(Collision collision)
        {
            if (used) return;
            used = true;

            Vector3 point = collision.GetContact(0).point;

            ShooterTarget target = collision.collider.GetComponentInParent<ShooterTarget>();
            if (target != null)
            {
                target.Hit(point);
            }
            else
            {
                // Any other breakable object in the scene works too
                FractureController fc = collision.collider.GetComponentInParent<FractureController>();
                if (fc != null && !fc.HasFractured)
                    fc.Fracture();
            }

            // Kick the fragments around the impact point
            Rigidbody own = GetComponent<Rigidbody>();
            foreach (Collider col in Physics.OverlapSphere(point, impactRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null && rb != own && !rb.isKinematic)
                    rb.AddExplosionForce(impactForce, point, impactRadius, 0.2f, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
    }
}
