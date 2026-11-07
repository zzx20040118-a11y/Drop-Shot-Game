using System.Collections.Generic;
using UnityEngine;

namespace FractureTool
{
    public class FractureController : MonoBehaviour
    {
        public enum FractureMode
        {
            Voronoi,
            Simple,
        }

        [System.Serializable]
        public enum MaterialType
        {
            Custom,
        }

        [System.Serializable]
        public class MaterialPreset
        {
            public string name;
            public float mass = 0.3f;
            public float explosionForce = 2f;
            public float fragmentMultiplier = 1f;
            public bool isFragile = false;

            [Header("Fragment Multiplier Info")]
            [TextArea(2, 3)]
            public string info = "Controls fragment density:\n1.0 = Normal\n>1.0 = More fragments\n<1.0 = Fewer fragments";
        }

        [System.Serializable]
        public class CustomPhysicsSettings
        {
            [Header("Basic Physics Properties")]
            [Range(0.1f, 3f)] public float mass = 0.3f;
            [Range(0f, 2f)] public float drag = 0f;
            [Range(0f, 2f)] public float angularDrag = 0.05f;
            [Range(0.1f, 2f)] public float gravityScale = 1f;

            [Header("Material Properties")]
            [Range(0f, 1f)] public float bounciness = 0.3f;
            [Range(0f, 2f)] public float friction = 0.6f;

            [Header("Fracture Behavior")]
            [Range(0.1f, 5f)] public float explosionForceMultiplier = 1f;
            [Range(0.1f, 3f)] public float fragmentMultiplier = 1f;

            [Header("Physics Info")]
            [TextArea(3, 4)]
            public string info = "Basic Physics Settings:\n" +
                                "- Mass: Object weight\n" +
                                "- Drag: Air resistance\n" +
                                "- Angular Drag: Rotation resistance\n" +
                                "- Gravity Scale: How gravity affects the object";
        }

        [Header("Material Presets")]
        public MaterialType materialType = MaterialType.Custom;

        [Header("Main Configuration")]
        public FractureMode mode = FractureMode.Voronoi;
        public bool previewMode = false;

        [Header("Materials")]
        public Material interiorMaterial;
        public Material fragmentMaterial;

        [Header("General Physics")]
        public bool applyPhysics = true;
        public bool destroyOriginal = true;
        [Range(0f, 0.1f)] public float fragmentSeparation = 0.02f;

        [Header("Physics Control")]
        public bool useExplosionForce = true;
        [Range(0f, 10f)] public float globalExplosionForce = 2f;

        [Header("Fragment Lifetime")]
        public bool autoDestroy = true;
        [Range(1f, 30f)] public float destroyAfterSeconds = 10f;

        [Header("Custom Physics (Only for Custom Material)")]
        public CustomPhysicsSettings customPhysics = new CustomPhysicsSettings();

        static readonly Dictionary<MaterialType, MaterialPreset> materialPresets = new Dictionary<MaterialType, MaterialPreset>
        {
        };

        /// <summary>Returns the built-in preset for a material type, or null for Custom.</summary>
        public static MaterialPreset GetPreset(MaterialType type)
        {
            return materialPresets.TryGetValue(type, out MaterialPreset preset) ? preset : null;
        }

        /// <summary>True once this object has been broken.</summary>
        public bool HasFractured => hasFractured;

        bool hasFractured = false;
        PhysicsMaterial fragmentPhysicMaterial;

        void Awake()
        {
            InitializeCustomPhysicsDefaults();
        }

        void InitializeCustomPhysicsDefaults()
        {
            if (customPhysics.mass == 0f)
            {
                customPhysics.mass = 0.3f;
                customPhysics.drag = 0f;
                customPhysics.angularDrag = 0.05f;
                customPhysics.gravityScale = 1f;
                customPhysics.bounciness = 0.3f;
                customPhysics.friction = 0.6f;
                customPhysics.explosionForceMultiplier = 1f;
                customPhysics.fragmentMultiplier = 1f;
            }
        }

        /// <summary>Previews or fractures depending on <see cref="previewMode"/>.</summary>
        public void TriggerFracture()
        {
            if (previewMode)
                Preview();
            else
                Fracture();
        }

        /// <summary>Shows the cut layout of the current mode without breaking the object.</summary>
        public void Preview()
        {
            switch (mode)
            {
                case FractureMode.Voronoi:
                    {
                        VoronoiGenerator vg = GetOrAddComponent<VoronoiGenerator>();
                        UpdateVoronoiSettings(vg);
                        vg.PreviewVoronoi(gameObject);
                        break;
                    }
                case FractureMode.Simple:
                    // The horizontal cut plane is drawn by OnDrawGizmos
                    break;
            }
        }

        /// <summary>Breaks the object using the current mode. Only works in Play Mode.</summary>
        public void Fracture()
        {
            if (hasFractured) return;

            if (!Application.isPlaying)
            {
                Debug.LogWarning("Fracturing is only available in Play Mode. Use Preview to inspect the cuts in the editor.", this);
                return;
            }

            hasFractured = true;
            IFractureStrategy strategy = null;

            // Fall back to the object's own material if no fragment material is assigned
            Material originalMaterial = GetOriginalMaterial();

            switch (mode)
            {
                case FractureMode.Voronoi:
                    {
                        VoronoiGenerator vg = GetOrAddComponent<VoronoiGenerator>();
                        UpdateVoronoiSettings(vg, originalMaterial);
                        vg.previewMode = false;
                        strategy = vg;
                        break;
                    }
                case FractureMode.Simple:
                    {
                        SimpleFracture sf = new SimpleFracture();
                        sf.interiorMaterial = interiorMaterial;
                        sf.fragmentMaterial = fragmentMaterial ? fragmentMaterial : originalMaterial;
                        sf.destroyOriginal = destroyOriginal;
                        sf.autoDestroy = autoDestroy;
                        sf.destroyAfterSeconds = destroyAfterSeconds;
                        sf.fractureController = this;
                        strategy = sf;
                        break;
                    }
            }

            if (strategy != null)
            {
                strategy.Fracture(gameObject);
            }
        }

        Material GetOriginalMaterial()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            return mr != null ? mr.sharedMaterial : null;
        }

        // Methods to synchronize configurations
        void UpdateVoronoiSettings(VoronoiGenerator vg, Material originalMaterial = null)
        {
            vg.interiorMaterial = interiorMaterial;
            vg.fragmentMaterial = fragmentMaterial ? fragmentMaterial : originalMaterial;
            vg.applyPhysics = applyPhysics;
            vg.destroyOriginal = destroyOriginal;
            vg.autoDestroy = autoDestroy;
            vg.destroyAfterSeconds = destroyAfterSeconds;
            vg.useExplosionForce = useExplosionForce;
            vg.explosionForce = GetPresetExplosionForce();
            vg.fractureController = this;

            // Fragment density depends on the material (applied at generation time, the serialized count is untouched)
            vg.pointMultiplier = GetPresetFragmentMultiplier();
        }

        T GetOrAddComponent<T>() where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        void RemoveComponentSafe(Component component)
        {
            if (component == null) return;

            if (Application.isPlaying)
                Destroy(component);
            else
                DestroyImmediate(component);
        }

        void CleanupUnusedComponents()
        {
            // Keep only the component needed by the current mode
            if (mode != FractureMode.Voronoi)
                RemoveComponentSafe(gameObject.GetComponent<VoronoiGenerator>());

        }

        void SyncModeComponents()
        {
            if (this == null) return;

            CleanupUnusedComponents();

            // Add the component needed by the current mode (Simple doesn't need one)
            switch (mode)
            {
                case FractureMode.Voronoi:
                    GetOrAddComponent<VoronoiGenerator>();
                    break;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Adding or destroying components is not allowed inside OnValidate, so defer it
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
            UnityEditor.EditorApplication.delayCall += SyncModeComponents;
        }
#endif

        // For quick testing
        void OnMouseDown()
        {
            if (Application.isPlaying)
                TriggerFracture();
        }

        void OnDrawGizmosSelected()
        {
            if (mode != FractureMode.Simple) return;

            // Show the horizontal cut of SimpleFracture
            Collider col = GetComponent<Collider>();
            Bounds bounds = col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x, 0f, bounds.size.z));
        }

        // Public methods for UI
        public void SetFractureMode(FractureMode newMode)
        {
            mode = newMode;
            SyncModeComponents();
        }

        public void TogglePreviewMode()
        {
            previewMode = !previewMode;
        }

        public void ConfigureUniversalFragment(GameObject fragment, Vector3 seedPosition = default, int index = 0)
        {
            if (fragment == null) return;

            if (!IsValidUniversalFragment(fragment))
            {
                FractureUtility.DestroyFragment(fragment);
                return;
            }

            // UVs first, so the collider is built from the final mesh

            MeshRenderer mr = fragment.GetComponent<MeshRenderer>();
            if (mr != null && fragmentMaterial != null)
            {
                Material[] mats = mr.sharedMaterials;
                mats[0] = fragmentMaterial;
                mr.sharedMaterials = mats;
            }

            ConfigureUniversalCollider(fragment);

            // Push the fragment slightly away from the center (or toward its Voronoi seed)
            Vector3 displacement;
            if (seedPosition != Vector3.zero)
            {
                displacement = (seedPosition - fragment.transform.position).normalized * fragmentSeparation;
            }
            else
            {
                Vector3 fragmentCenter = fragment.GetComponent<Collider>().bounds.center;
                displacement = (fragmentCenter - transform.position).normalized * fragmentSeparation;

                if (displacement.sqrMagnitude < 0.000001f)
                {
                    displacement = Random.onUnitSphere * fragmentSeparation;
                    displacement.y = Mathf.Abs(displacement.y);
                }
            }
            fragment.transform.position += displacement;

            ApplyPhysicsConfiguration(fragment);

            if (autoDestroy)
            {
                FragmentDestroyer destroyer = fragment.AddComponent<FragmentDestroyer>();
                destroyer.destroyAfter = destroyAfterSeconds;
            }
        }

        bool IsValidUniversalFragment(GameObject fragment)
        {
            return FractureUtility.HasGeometry(fragment, 0.01f, 3);
        }

        void ConfigureUniversalCollider(GameObject fragment)
        {
            foreach (Collider c in fragment.GetComponents<Collider>())
                Destroy(c);

            MeshFilter mf = fragment.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh)
            {
                fragment.AddComponent<BoxCollider>();
                return;
            }

            Mesh mesh = mf.sharedMesh;
            Vector3 sz = mesh.bounds.size;
            bool tooThin = (sz.x < 0.002f && sz.y < 0.002f && sz.z < 0.002f);
            if (mesh.vertexCount < 4 || tooThin)
            {
                BoxCollider box = fragment.AddComponent<BoxCollider>();
                box.size = mesh.bounds.size;
                box.center = mesh.bounds.center;
                return;
            }

            MeshCollider mc = fragment.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = mesh;
        }

        void ApplyPhysicsConfiguration(GameObject fragment)
        {
            if (!applyPhysics) return;

            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb == null)
                rb = fragment.AddComponent<Rigidbody>();

            float fragmentSize = fragment.GetComponent<Collider>().bounds.size.magnitude;

            if (materialType == MaterialType.Custom)
                ApplyCustomPhysics(rb, fragment);
            else
                ApplyPresetPhysics(rb, fragment, fragmentSize);
        }

        float GetPresetExplosionForce()
        {
            if (materialType == MaterialType.Custom)
                return globalExplosionForce * customPhysics.explosionForceMultiplier;

            MaterialPreset preset = GetPreset(materialType);
            return preset != null ? globalExplosionForce * preset.explosionForce : globalExplosionForce;
        }

        float GetPresetFragmentMultiplier()
        {
            if (materialType == MaterialType.Custom)
                return customPhysics.fragmentMultiplier;

            MaterialPreset preset = GetPreset(materialType);
            return preset != null ? preset.fragmentMultiplier : 1f;
        }

        Vector3 CalculateForceDirection(GameObject fragment)
        {
            Vector3 fragmentCenter = fragment.GetComponent<Collider>().bounds.center;
            return (fragmentCenter - transform.position).normalized;
        }

        void ApplyCustomPhysics(Rigidbody rb, GameObject fragment)
        {
            // Basic physical properties
            rb.mass = customPhysics.mass;
            rb.linearDamping = customPhysics.drag;
            rb.angularDamping = customPhysics.angularDrag;
            rb.useGravity = true;
            rb.isKinematic = false;

            // Custom gravity scale lives on the fragment, so it survives the original being destroyed
            if (!Mathf.Approximately(customPhysics.gravityScale, 1f))
                fragment.AddComponent<FragmentGravity>().gravityScale = customPhysics.gravityScale;

            if (useExplosionForce)
            {
                // Push away from the original object's center
                Vector3 forceDirection = CalculateForceDirection(fragment);

                // Random variation for a more natural look (never pointing down)
                Vector3 randomVariation = Random.onUnitSphere * 0.3f;
                randomVariation.y = Mathf.Abs(randomVariation.y);

                float effectiveForce = globalExplosionForce * customPhysics.explosionForceMultiplier;
                Vector3 finalForce = (forceDirection + randomVariation) * effectiveForce;

                rb.AddForce(finalForce, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * effectiveForce * 0.3f, ForceMode.Impulse);
            }

            // Friction and bounciness
            SetupCustomPhysicMaterial(fragment);
        }

        void ApplyPresetPhysics(Rigidbody rb, GameObject fragment, float fragmentSize)
        {
            MaterialPreset preset = GetPreset(materialType);
            if (preset == null) return;

            rb.mass = Mathf.Clamp(fragmentSize * preset.mass, 0.1f, 2f);
            rb.isKinematic = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.useGravity = true;

            if (useExplosionForce)
            {
                Vector3 forceDirection = CalculateForceDirection(fragment);
                Vector3 randomVariation = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0f, 0.5f),
                    Random.Range(-0.5f, 0.5f)
                );

                // Presets set the launch speed directly, so very light materials don't fly off at extreme speeds
                float materialForce = globalExplosionForce * preset.explosionForce;
                rb.AddForce((forceDirection + randomVariation) * materialForce, ForceMode.VelocityChange);

                float spin = preset.isFragile ? 2f : 1f;
                rb.AddTorque(Random.onUnitSphere * materialForce * spin, ForceMode.VelocityChange);
            }
        }

        void SetupCustomPhysicMaterial(GameObject fragment)
        {
            Collider col = fragment.GetComponent<Collider>();
            if (col == null) return;

            // One shared physic material per controller instead of one per fragment
            if (fragmentPhysicMaterial == null)
            {
                fragmentPhysicMaterial = new PhysicsMaterial("Fragment_Material")
                {
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Average
                };
            }

            fragmentPhysicMaterial.dynamicFriction = customPhysics.friction;
            fragmentPhysicMaterial.staticFriction = customPhysics.friction;
            fragmentPhysicMaterial.bounciness = customPhysics.bounciness;

            col.sharedMaterial = fragmentPhysicMaterial;
        }

        public void SetCustomPhysicsPreset(string presetName)
        {
            switch (presetName.ToLower())
            {
                case "lightweight":
                    customPhysics.mass = 0.1f;
                    customPhysics.explosionForceMultiplier = 2f;
                    customPhysics.gravityScale = 0.7f;
                    customPhysics.drag = 0.2f;
                    break;

                case "heavy":
                    customPhysics.mass = 1.5f;
                    customPhysics.explosionForceMultiplier = 0.8f;
                    customPhysics.gravityScale = 1.3f;
                    customPhysics.drag = 0f;
                    break;

                case "bouncy":
                    customPhysics.mass = 0.3f;
                    customPhysics.bounciness = 0.8f;
                    customPhysics.friction = 0.2f;
                    customPhysics.explosionForceMultiplier = 1.5f;
                    break;

                default:
                    customPhysics.mass = 0f;
                    InitializeCustomPhysicsDefaults();
                    break;
            }
        }

        // Getters for the editor
        public VoronoiGenerator GetVoronoiComponent() => GetComponent<VoronoiGenerator>();
    }
}
