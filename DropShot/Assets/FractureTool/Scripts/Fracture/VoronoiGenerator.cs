using System.Collections.Generic;
using UnityEngine;
using FractureTool.EzySlice;

namespace FractureTool
{
    public class VoronoiGenerator : MonoBehaviour, IFractureStrategy
    {
        public int pointCount = 10;
        public Material fragmentMaterial;
        public bool destroyOriginal = true;
        public bool previewMode = false;
        public Material interiorMaterial;
        public bool applyPhysics = true;
        [SerializeField, Range(0f, 0.1f)] private float fragmentSeparation = 0.02f;

        [Header("Fragment Lifetime")]
        public bool autoDestroy = true;
        public float destroyAfterSeconds = 10f;

        [Header("Physics Control")]
        public bool useExplosionForce = true;
        public float explosionForce = 2f;

        [HideInInspector] public FractureController fractureController;

        /// <summary>Scales pointCount at generation time (set by material presets).</summary>
        [System.NonSerialized] public float pointMultiplier = 1f;

        const int MaxPoints = 200;

        readonly List<Vector3> voronoiSeeds = new List<Vector3>();
        readonly Dictionary<Vector3, List<Vector3>> voronoiCells = new Dictionary<Vector3, List<Vector3>>();

        public void Fracture(GameObject target)
        {
            GenerateVoronoiSeeds();
            CalculateVoronoiCells();

            if (voronoiSeeds.Count == 0) return;

            FractureUtility.SetSourceVisible(target, false);

            int built = BuildFragments(target);
            if (built == 0)
            {
                FractureUtility.SetSourceVisible(target, true);
                return;
            }

            if (destroyOriginal)
                Destroy(target);
        }

        public void PreviewVoronoi(GameObject target)
        {
            GenerateVoronoiSeeds();
            CalculateVoronoiCells();
            // Data is visualized in OnDrawGizmos()
        }

        void GenerateVoronoiSeeds()
        {
            voronoiSeeds.Clear();

            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("VoronoiGenerator needs a Collider on the object.", this);
                return;
            }

            int targetCount = Mathf.Clamp(Mathf.RoundToInt(pointCount * pointMultiplier), 3, MaxPoints);

            Bounds objectBounds = collider.bounds;
            Vector3 boundsCenter = objectBounds.center;
            Vector3 boundsSize = objectBounds.size;
            float minDistance = Mathf.Min(boundsSize.x, boundsSize.y, boundsSize.z) * 0.15f;

            // ClosestPoint is only reliable on primitive and convex mesh colliders
            MeshCollider meshCollider = collider as MeshCollider;
            bool canTestInside = meshCollider == null || meshCollider.convex;

            int maxAttempts = targetCount * 15;
            int attempts = 0;

            while (voronoiSeeds.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;

                Vector3 randomPoint = boundsCenter + new Vector3(
                    Random.Range(-boundsSize.x * 0.4f, boundsSize.x * 0.4f),
                    Random.Range(-boundsSize.y * 0.4f, boundsSize.y * 0.4f),
                    Random.Range(-boundsSize.z * 0.4f, boundsSize.z * 0.4f)
                );

                // Check that the point is inside the object
                if (canTestInside && (collider.ClosestPoint(randomPoint) - randomPoint).sqrMagnitude > 0.0001f)
                    continue;

                // Keep seeds apart so cells don't end up too thin
                bool tooClose = false;
                foreach (Vector3 existingSeed in voronoiSeeds)
                {
                    if (Vector3.Distance(randomPoint, existingSeed) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    voronoiSeeds.Add(randomPoint);
            }
        }

        void CalculateVoronoiCells()
        {
            voronoiCells.Clear();

            // Store the neighbors used to cut each cell (also used by the gizmos)
            foreach (Vector3 seed in voronoiSeeds)
                voronoiCells[seed] = GetNearestNeighbors(seed, 8);
        }

        List<Vector3> GetNearestNeighbors(Vector3 point, int maxNeighbors)
        {
            List<Vector3> neighbors = new List<Vector3>(voronoiSeeds.Count);
            foreach (Vector3 p in voronoiSeeds)
                if (p != point) neighbors.Add(p);

            neighbors.Sort((a, b) => (a - point).sqrMagnitude.CompareTo((b - point).sqrMagnitude));

            if (neighbors.Count > maxNeighbors)
                neighbors.RemoveRange(maxNeighbors, neighbors.Count - maxNeighbors);

            return neighbors;
        }

        int BuildFragments(GameObject target)
        {
            MeshFilter originalMF = target.GetComponent<MeshFilter>();
            if (originalMF == null || originalMF.sharedMesh == null) return 0;

            Material interior = FractureUtility.ResolveInteriorMaterial(interiorMaterial, target);
            int built = 0;

            for (int s = 0; s < voronoiSeeds.Count; s++)
            {
                Vector3 seed = voronoiSeeds[s];

                // Start from the original and keep the side of each bisector plane that contains the seed
                GameObject cell = target;

                foreach (Vector3 neighbor in voronoiCells[seed])
                {
                    Vector3 midPoint = (seed + neighbor) * 0.5f;
                    Vector3 normal = (seed - neighbor).normalized;

                    SlicedHull hull = cell.Slice(midPoint, normal, interior);
                    if (hull == null) continue;

                    // The normal points toward the seed, so the seed is always on the upper side
                    GameObject piece = hull.CreateUpperHull(cell, interior);
                    if (piece == null) continue;

                    if (cell != target)
                        FractureUtility.DestroyFragment(cell);
                    cell = piece;
                }

                // A cell that was never cut would be a copy of the whole object
                if (cell == target) continue;

                cell.name = $"Fragment_Voronoi_{s}";
                ConfigureFragment(cell, seed);
                built++;
            }

            return built;
        }

        void ConfigureFragment(GameObject fragment, Vector3 seedPosition)
        {
            if (fragment == null) return;

            if (fractureController != null)
            {
                fractureController.ConfigureUniversalFragment(fragment, seedPosition);
                return;
            }

            // Check minimum size
            if (!FractureUtility.HasGeometry(fragment, 0.05f, 4))
            {
                FractureUtility.DestroyFragment(fragment);
                return;
            }

            // Configure material
            MeshRenderer mr = fragment.GetComponent<MeshRenderer>();
            if (mr != null && fragmentMaterial != null)
            {
                Material[] mats = mr.sharedMaterials;
                mats[0] = fragmentMaterial;
                mr.sharedMaterials = mats;
            }

            // Configure collider
            MeshCollider mc = fragment.AddComponent<MeshCollider>();
            mc.convex = true;

            // Small displacement toward the seed
            Vector3 displacement = (seedPosition - fragment.transform.position).normalized * fragmentSeparation;
            fragment.transform.position += displacement;

            // Configure physics
            if (applyPhysics)
            {
                Rigidbody rb = fragment.AddComponent<Rigidbody>();
                rb.mass = 0.2f;

                if (useExplosionForce)
                {
                    // Impulse toward the seed direction
                    Vector3 forceDirection = (seedPosition - transform.position).normalized;
                    Vector3 randomForce = forceDirection + new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        Random.Range(0f, 0.5f),
                        Random.Range(-0.5f, 0.5f)
                    );

                    rb.AddForce(randomForce * explosionForce, ForceMode.Impulse);
                }
            }

            if (autoDestroy)
            {
                FragmentDestroyer destroyer = fragment.AddComponent<FragmentDestroyer>();
                destroyer.destroyAfter = destroyAfterSeconds;
            }
        }

        void OnDrawGizmos()
        {
            if (voronoiSeeds.Count == 0) return;

            Collider collider = GetComponent<Collider>();
            if (collider == null) return;

            Bounds objectBounds = collider.bounds;

            Gizmos.color = Color.red;
            foreach (Vector3 seed in voronoiSeeds)
                Gizmos.DrawSphere(seed, 0.08f);

            foreach (KeyValuePair<Vector3, List<Vector3>> cell in voronoiCells)
            {
                Vector3 seed = cell.Key;
                List<Vector3> neighbors = cell.Value;

                // Only the closest neighbors, to keep the view readable
                int count = Mathf.Min(4, neighbors.Count);
                for (int i = 0; i < count; i++)
                {
                    Vector3 midPoint = (seed + neighbors[i]) * 0.5f;
                    if (!objectBounds.Contains(midPoint)) continue;

                    Vector3 normal = (seed - neighbors[i]).normalized;

                    if (i < 3)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(midPoint, 0.03f);
                    }

                    // Short line representing the bisector plane
                    Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                    if (tangent.magnitude < 0.1f) tangent = Vector3.Cross(normal, Vector3.right);
                    tangent = tangent.normalized * 0.2f;

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(midPoint - tangent, midPoint + tangent);
                }
            }
        }
    }
}
