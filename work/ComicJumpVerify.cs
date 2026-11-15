using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ComicJumpVerify
{
    static ComicJumpVerify() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        var report = new StringBuilder();
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture rt = null;
        Texture2D image = null;
        var previous = RenderTexture.active;
        try
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/M_ComicJump.mat");
            if (material == null || ShaderUtil.ShaderHasError(material.shader))
                throw new Exception("Particle shader failed import.");
            var cameraObject = new GameObject("Preview Camera", typeof(Camera), typeof(BallCameraFollow));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -5);
            camera.orthographic = true;
            camera.orthographicSize = 1.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.65f, 0.53f, 0.46f);
            var actor = new GameObject("VFX Check", typeof(BallStrikeFlight));
            SceneManager.MoveGameObjectToScene(actor, scene);
            actor.transform.localScale = Vector3.one * 0.25f;
            var flight = actor.GetComponent<BallStrikeFlight>();
            flight.ballCamera = cameraObject.GetComponent<BallCameraFollow>();
            var effect = actor.AddComponent<BallJumpPopVFX>();
            effect.flight = flight;
            effect.particleMaterial = material;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BallJumpPopVFX).GetMethod("Awake", flags).Invoke(effect, null);
            typeof(BallJumpPopVFX).GetMethod("Burst", flags).Invoke(effect, new object[] { new Vector3(-0.7f, -0.5f, 0) });
            var emit = typeof(BallJumpPopVFX).GetMethod("Emit", flags);
            for (int i = 0; i < 5; i++)
                emit.Invoke(effect, new object[] { new Vector3(-0.4f + i * 0.22f, -0.1f + i * 0.13f, 0),
                    Vector3.zero, new Vector2(0.14f, 0.14f), 0f, 0.4f });
            var systems = actor.GetComponentsInChildren<ParticleSystem>();
            foreach (var system in systems)
            {
                if (system.particleCount != 11) throw new Exception("Expected 6 burst marks + 5 trail dots.");
                system.Simulate(0.03f, false, false, true);
            }
            rt = new RenderTexture(512, 512, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            image.Apply();
            File.WriteAllBytes("../work/comic-jump-preview.png", image.EncodeToPNG());
            typeof(BallJumpPopVFX).GetField("wasFlying", flags).SetValue(effect, true);
            flight.enabled = false;
            typeof(BallJumpPopVFX).GetMethod("LateUpdate", flags).Invoke(effect, null);
            foreach (var system in systems)
                if (system.particleCount != 0) throw new Exception("Portal entry did not clear particles.");
            report.AppendLine("PASS: shader imported; 6 launch marks and 5 trail dots per layer; portal entry clears both layers.");
        }
        catch (Exception exception) { report.AppendLine(exception.ToString()); }
        finally
        {
            RenderTexture.active = previous;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllText("../work/comic-jump-verify.txt", report.ToString());
        }
    }
}
