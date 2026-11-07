using UnityEngine;
using FractureTool.EzySlice;

namespace FractureTool
{
    public class SimpleFracture : IFractureStrategy
    {
        public Material interiorMaterial;
        public Material fragmentMaterial;
        public bool destroyOriginal = true;
        public bool autoDestroy = true;
        public float destroyAfterSeconds = 10f;
        public FractureController fractureController;

        public void Fracture(GameObject target)
        {
            // Horizontal cut through the center of the object
            Collider col = target.GetComponent<Collider>();
            Vector3 planePosition = col != null ? col.bounds.center : target.transform.position;
            Vector3 planeNormal = Vector3.up;

            Material interior = FractureUtility.ResolveInteriorMaterial(interiorMaterial, target);
            SlicedHull slicedHull = target.Slice(planePosition, planeNormal, interior);

            if (slicedHull == null)
            {
                Debug.LogWarning("Simple fracture could not cut " + target.name, target);
                return;
            }

            GameObject upperHull = slicedHull.CreateUpperHull(target, interior);
            GameObject lowerHull = slicedHull.CreateLowerHull(target, interior);

            ConfigureFragment(upperHull, "Upper");
            ConfigureFragment(lowerHull, "Lower");

            if (destroyOriginal)
                Object.Destroy(target);
            else
                FractureUtility.SetSourceVisible(target, false);
        }

        void ConfigureFragment(GameObject fragment, string suffix)
        {
            if (fragment == null) return;

            fragment.name = $"Fragment_Simple_{suffix}";

            if (fractureController != null)
            {
                fractureController.ConfigureUniversalFragment(fragment);
                return;
            }

            // Configure material
            if (fragmentMaterial != null)
            {
                MeshRenderer mr = fragment.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Material[] mats = mr.sharedMaterials;
                    mats[0] = fragmentMaterial;
                    mr.sharedMaterials = mats;
                }
            }

            // Configure collider
            MeshCollider mc = fragment.AddComponent<MeshCollider>();
            mc.convex = true;

            // Configure physics
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            rb.mass = 0.3f;

            if (autoDestroy)
            {
                FragmentDestroyer destroyer = fragment.AddComponent<FragmentDestroyer>();
                destroyer.destroyAfter = destroyAfterSeconds;
            }
        }
    }
}
