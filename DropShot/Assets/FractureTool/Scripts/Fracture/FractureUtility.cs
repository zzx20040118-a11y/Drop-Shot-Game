using System.Collections.Generic;
using UnityEngine;
using FractureTool.EzySlice;

namespace FractureTool
{
    public static class FractureUtility
    {
        /// <summary>
        /// Cuts every fragment in the list with the given world-space plane.
        /// The source object is never destroyed here; intermediate pieces created by previous cuts are.
        /// </summary>
        public static List<GameObject> SliceAll(List<GameObject> fragments, Vector3 position, Vector3 normal,
            Material interiorMaterial, GameObject source, System.Func<GameObject, bool> isValid = null)
        {
            List<GameObject> result = new List<GameObject>(fragments.Count * 2);

            foreach (GameObject fragment in fragments)
            {
                if (fragment == null) continue;

                SlicedHull hull = fragment.Slice(position, normal, interiorMaterial);
                if (hull == null)
                {
                    // The plane does not intersect this piece, keep it as it is
                    result.Add(fragment);
                    continue;
                }

                AddOrDiscard(result, hull.CreateUpperHull(fragment, interiorMaterial), isValid);
                AddOrDiscard(result, hull.CreateLowerHull(fragment, interiorMaterial), isValid);

                if (fragment != source)
                    DestroyFragment(fragment);
            }

            return result;
        }

        static void AddOrDiscard(List<GameObject> list, GameObject piece, System.Func<GameObject, bool> isValid)
        {
            if (piece == null) return;

            if (isValid == null || isValid(piece))
                list.Add(piece);
            else
                DestroyFragment(piece);
        }

        /// <summary>
        /// Uses the object's own material for the cut faces when no interior material is assigned,
        /// so fragments never end up with an empty material slot.
        /// </summary>
        public static Material ResolveInteriorMaterial(Material interiorMaterial, GameObject target)
        {
            if (interiorMaterial != null) return interiorMaterial;

            MeshRenderer mr = target.GetComponent<MeshRenderer>();
            return mr != null ? mr.sharedMaterial : null;
        }

        public static void SetSourceVisible(GameObject source, bool visible)
        {
            MeshRenderer mr = source.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = visible;

            Collider col = source.GetComponent<Collider>();
            if (col != null) col.enabled = visible;
        }

        /// <summary>
        /// Removes the untouched source from the fragment list. If nothing was cut, the source is shown again.
        /// </summary>
        public static void FinalizeFragments(List<GameObject> fragments, GameObject source)
        {
            fragments.RemoveAll(f => f == null || f == source);

            if (fragments.Count == 0)
            {
                SetSourceVisible(source, true);
                Debug.LogWarning($"Fracture on '{source.name}' produced no fragments. Check that the cut region intersects the mesh.", source);
            }
        }

        public static bool HasGeometry(GameObject fragment, float minSize, int minVertices)
        {
            if (fragment == null) return false;

            MeshFilter mf = fragment.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;

            Mesh mesh = mf.sharedMesh;
            return mesh.vertexCount >= minVertices
                && GetIndexCount(mesh) >= 3
                && mesh.bounds.size.magnitude > minSize;
        }

        public static long GetIndexCount(Mesh mesh)
        {
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                count += mesh.GetIndexCount(i);
            return count;
        }

        public static void DestroyFragment(GameObject fragment)
        {
            if (fragment == null) return;

            ReleaseFragmentResources(fragment);
            Object.Destroy(fragment);
        }

        /// <summary>
        /// Frees the runtime meshes and material instances created for a fragment.
        /// </summary>
        public static void ReleaseFragmentResources(GameObject fragment)
        {
            MeshFilter mf = fragment.GetComponent<MeshFilter>();
            Mesh mesh = mf != null ? mf.sharedMesh : null;
            if (mesh != null && IsRuntimeObject(mesh))
                Object.Destroy(mesh);

            MeshCollider mc = fragment.GetComponent<MeshCollider>();
            if (mc != null && mc.sharedMesh != null && mc.sharedMesh != mesh && IsRuntimeObject(mc.sharedMesh))
                Object.Destroy(mc.sharedMesh);

            MeshRenderer mr = fragment.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (Material mat in mr.sharedMaterials)
                {
                    if (mat != null && mat.name.EndsWith("(Instance)"))
                        Object.Destroy(mat);
                }
            }
        }

        // Objects created at runtime have negative instance IDs, assets loaded from disk have positive ones
        static bool IsRuntimeObject(Object obj) => obj.GetInstanceID() < 0;
    }
}
