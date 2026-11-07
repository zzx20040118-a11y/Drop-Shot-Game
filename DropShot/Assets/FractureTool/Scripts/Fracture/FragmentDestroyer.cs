using UnityEngine;

namespace FractureTool
{
    public class FragmentDestroyer : MonoBehaviour
    {
        public float destroyAfter = 10f;

        void Start()
        {
            Destroy(gameObject, destroyAfter);
        }

        void OnDestroy()
        {
            FractureUtility.ReleaseFragmentResources(gameObject);
        }
    }
}
