using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PortalBrightnessVerify
{
    static PortalBrightnessVerify() { EditorApplication.delayCall += Run; }

    static void Run()
    {
        var report = new StringBuilder();
        var scene = EditorSceneManager.NewPreviewScene();
        Material material = null;
        RenderTexture rt = null;
        Texture2D capture = null;
        var previous = RenderTexture.active;
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/VFX/PortalEspacial/Mat/M_PortalRainbow_BHard.mat");
            if (source == null) throw new Exception("Spiral material not imported.");
            report.AppendLine("Revision: direct brightness response");
            report.AppendLine("Material: " + source.name);
            report.AppendLine("Shader errors: " + ShaderUtil.ShaderHasError(source.shader));
            foreach (var message in ShaderUtil.GetShaderMessages(source.shader))
                report.AppendLine(message.severity + ": " + message.message);
            material = new Material(source);
            material.SetFloat("_Cull", 0f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(quad, scene);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            var mesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.colors = colors;
            quad.GetComponent<MeshFilter>().sharedMesh = mesh;

            var cameraObject = new GameObject("Offscreen Shader Check", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -3);
            camera.orthographic = true;
            camera.orthographicSize = 0.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1);
            rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGBHalf);
            camera.targetTexture = rt;
            capture = new Texture2D(256, 256, TextureFormat.RGBAFloat, false, true);
            double[] averages = new double[2];
            for (int test = 0; test < 2; test++)
            {
                material.SetFloat("_Brightness", test == 0 ? 0 : source.GetFloat("_Brightness"));
                camera.Render();
                RenderTexture.active = rt;
                capture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                capture.Apply();
                var pixels = capture.GetPixels();
                foreach (var pixel in pixels) averages[test] += pixel.r + pixel.g + pixel.b;
                averages[test] /= pixels.Length * 3;
                File.WriteAllBytes("../work/portal-brightness-" + test + ".png", capture.EncodeToPNG());
            }
            report.AppendLine("Brightness 0 mean RGB: " + averages[0]);
            report.AppendLine("Current brightness mean RGB: " + averages[1]);
            report.AppendLine("Visible brightness change: " + (averages[1] > averages[0] + 0.01));
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        catch (Exception exception) { report.AppendLine(exception.ToString()); }
        finally
        {
            RenderTexture.active = previous;
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
            if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllText("../work/portal-brightness-verify.txt", report.ToString());
        }
    }
}
