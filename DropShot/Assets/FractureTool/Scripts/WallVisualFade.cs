using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FractureTool
{
    public class WallVisualFade : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        readonly List<Color> colors = new List<Color>();
        float duration;
        float elapsed;

        public void Begin(float seconds)
        {
            duration = Mathf.Max(0.01f, seconds);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                var copies = renderer.sharedMaterials;
                for (int i = 0; i < copies.Length; i++)
                {
                    if (copies[i] == null)
                        continue;
                    var material = new Material(copies[i]);
                    material.name = "Wall Fade";
                    // The walls use URP Lit. Change only runtime material copies.
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0f);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.DisableKeyword("_ALPHAMODULATE_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    materials.Add(material);
                    colors.Add(material.GetColor("_BaseColor"));
                    copies[i] = material;
                }
                renderer.sharedMaterials = copies;
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < materials.Count; i++)
            {
                Color color = colors[i];
                color.a *= alpha;
                materials[i].SetColor("_BaseColor", color);
            }
            if (elapsed >= duration)
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null)
                    Destroy(material);
        }
    }
}
