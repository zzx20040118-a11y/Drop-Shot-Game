using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FractureTool.Editors
{
    /// <summary>
    /// The demo materials are authored for the Built-in Render Pipeline. When the project uses URP or HDRP,
    /// they are switched to the pipeline's default lit shader automatically so the demo never shows pink materials.
    /// </summary>
    [InitializeOnLoad]
    static class DemoMaterialConverter
    {
        const string DemoMaterials = "Assets/FractureTool/Demo/Materials";

        static DemoMaterialConverter()
        {
            EditorApplication.delayCall += AutoConvert;
        }

        static void AutoConvert()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;

            int converted = Convert();
            if (converted > 0)
                Debug.Log($"Fracture Tool: converted {converted} demo materials to {GraphicsSettings.currentRenderPipeline.defaultShader.name}.");
        }

        [MenuItem("Tools/Fracture Tool/Convert Demo Materials To Current Pipeline")]
        static void ConvertFromMenu()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                EditorUtility.DisplayDialog("Fracture Tool", "The project uses the Built-in Render Pipeline. The demo materials are already compatible.", "OK");
                return;
            }

            int converted = Convert();
            EditorUtility.DisplayDialog("Fracture Tool", $"Converted {converted} demo materials.", "OK");
        }

        static int Convert()
        {
            if (!AssetDatabase.IsValidFolder(DemoMaterials)) return 0;

            Shader target = GraphicsSettings.currentRenderPipeline.defaultShader;
            if (target == null) return 0;

            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { DemoMaterials }))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null || mat.shader == null || mat.shader.name != "Standard") continue;

                // Read the Built-in values before switching shaders
                Color color = mat.GetColor("_Color");
                Texture tex = mat.GetTexture("_MainTex");
                Vector2 tiling = mat.GetTextureScale("_MainTex");
                float smoothness = mat.GetFloat("_Glossiness");
                float metallic = mat.GetFloat("_Metallic");
                bool emissive = mat.IsKeywordEnabled("_EMISSION");
                Color emission = mat.GetColor("_EmissionColor");
                bool transparent = mat.GetFloat("_Mode") >= 2f;

                mat.shader = target;

                SetColor(mat, color, "_BaseColor", "_Color");
                SetTexture(mat, tex, tiling, "_BaseMap", "_BaseColorMap", "_MainTex");
                SetFloat(mat, smoothness, "_Smoothness", "_Glossiness");
                SetFloat(mat, metallic, "_Metallic");

                if (emissive)
                {
                    SetColor(mat, emission, "_EmissionColor", "_EmissiveColor");
                    mat.EnableKeyword("_EMISSION");
                }

                if (transparent)
                    MakeTransparent(mat);

                EditorUtility.SetDirty(mat);
                count++;
            }

            AssetDatabase.SaveAssets();
            return count;
        }

        static void MakeTransparent(Material mat)
        {
            SetFloat(mat, 1f, "_Surface", "_SurfaceType");
            SetFloat(mat, 0f, "_Blend", "_BlendMode");
            SetFloat(mat, (float)BlendMode.SrcAlpha, "_SrcBlend");
            SetFloat(mat, (float)BlendMode.OneMinusSrcAlpha, "_DstBlend");
            SetFloat(mat, 0f, "_ZWrite");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
        }

        static void SetColor(Material mat, Color value, params string[] names)
        {
            foreach (string n in names)
                if (mat.HasProperty(n)) { mat.SetColor(n, value); return; }
        }

        static void SetFloat(Material mat, float value, params string[] names)
        {
            foreach (string n in names)
                if (mat.HasProperty(n)) { mat.SetFloat(n, value); return; }
        }

        static void SetTexture(Material mat, Texture tex, Vector2 tiling, params string[] names)
        {
            foreach (string n in names)
            {
                if (!mat.HasProperty(n)) continue;
                mat.SetTexture(n, tex);
                mat.SetTextureScale(n, tiling);
                return;
            }
        }
    }
}
