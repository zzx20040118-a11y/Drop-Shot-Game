using System.Collections;
using UnityEngine;

namespace FractureTool.Demo
{
    /// <summary>
    /// Simple automatic blaster that fires physical projectiles toward the crosshair.
    /// </summary>
    public class ShooterWeapon : MonoBehaviour
    {
        public Camera aimCamera;
        public Transform muzzle;
        public Light muzzleFlash;
        public Material projectileMaterial;

        [Header("Firing")]
        public float fireRate = 0.14f;
        public float projectileSpeed = 70f;
        public float projectileSize = 0.09f;
        public float maxRange = 100f;
        public float recoilDistance = 0.06f;

        [HideInInspector] public ShooterGame game;
        [HideInInspector] public Collider ownerCollider;

        float nextShot;
        float kick;
        Vector3 restPosition;
        Material trailMaterial;

        void Awake()
        {
            restPosition = transform.localPosition;
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        void OnDisable()
        {
            if (muzzleFlash != null) muzzleFlash.enabled = false;
            kick = 0f;
            transform.localPosition = restPosition;
        }

        void Update()
        {
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 0.6f);
            transform.localPosition = restPosition + new Vector3(0f, 0f, -kick);

            if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0) && Time.time >= nextShot)
                Fire();
        }

        public void Fire()
        {
            nextShot = Time.time + fireRate;
            kick = recoilDistance;

            // Aim at whatever is under the crosshair
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = Physics.Raycast(ray, out RaycastHit hit, maxRange, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point
                : ray.GetPoint(maxRange);
            Vector3 direction = (aimPoint - muzzle.position).normalized;

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";
            projectile.transform.position = muzzle.position;
            projectile.transform.localScale = Vector3.one * projectileSize;
            projectile.GetComponent<MeshRenderer>().sharedMaterial = projectileMaterial;

            Collider col = projectile.GetComponent<Collider>();
            if (ownerCollider != null) Physics.IgnoreCollision(col, ownerCollider);

            Rigidbody rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 0.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = direction * projectileSpeed;

            projectile.AddComponent<ShooterProjectile>();

            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.08f;
            trail.startWidth = projectileSize * 0.8f;
            trail.endWidth = 0f;
            trail.sharedMaterial = GetTrailMaterial();
            trail.startColor = new Color(1f, 0.65f, 0.2f, 1f);
            trail.endColor = new Color(1f, 0.35f, 0.05f, 0f);

            Destroy(projectile, 3f);

            if (game != null) game.OnShotFired();
            if (muzzleFlash != null) StartCoroutine(Flash());
        }

        Material GetTrailMaterial()
        {
            if (trailMaterial == null)
                trailMaterial = new Material(Shader.Find("Sprites/Default"));
            return trailMaterial;
        }

        IEnumerator Flash()
        {
            muzzleFlash.enabled = true;
            yield return new WaitForSeconds(0.04f);
            muzzleFlash.enabled = false;
        }

        void OnDestroy()
        {
            if (trailMaterial != null) Destroy(trailMaterial);
        }
    }
}
