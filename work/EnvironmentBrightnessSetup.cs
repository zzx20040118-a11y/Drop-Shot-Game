using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EnvironmentBrightnessSetup
{
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work"));
    static readonly string Report = Path.Combine(Folder, "environment-brightness-check.txt");

    static EnvironmentBrightnessSetup() { EditorApplication.delayCall += Run; }

    static void Run()
    {
        if (File.Exists(Report)) return;
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        HalftoneEnvironmentBrightness activeControl = null;
        try
        {
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in new[] { "Assets/Scenes/Game_Hard.unity", "Assets/Scenes/Game_Easy.unity" })
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                HalftoneEnvironmentBrightness control = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.TryGetComponent(out HalftoneEnvironmentBrightness found)) control = found;
                if (control == null)
                {
                    GameObject go = new GameObject("EnvironmentBrightness");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    control = go.AddComponent<HalftoneEnvironmentBrightness>();
                }
                control.brightness = 0.75f;
                EditorUtility.SetDirty(control);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (scene == original) activeControl = control;
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            SceneManager.SetActiveScene(original);
            if (activeControl == null) throw new Exception("No control in the active gameplay scene");
            activeControl.brightness = 1f;
            activeControl.SendMessage("Update");
            if (Shader.GetGlobalFloat("_DS_EnvironmentDim") != 0f) throw new Exception("Brightness 1 must restore original");
            float originalAverage = Capture("environment-original.png");
            activeControl.brightness = 0.75f;
            activeControl.SendMessage("Update");
            if (Mathf.Abs(Shader.GetGlobalFloat("_DS_EnvironmentDim") - 0.25f) > 0.0001f) throw new Exception("Brightness mapping incorrect");
            float dimAverage = Capture("environment-dimmed.png");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/HalfTone/SG_HalfTone.shadergraph");
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity.ToString() == "Error") throw new Exception(message.message);
            if (dimAverage >= originalAverage - 0.001f) throw new Exception("Rendered environment did not darken");
            activeControl.enabled = false;
            if (Shader.GetGlobalFloat("_DS_EnvironmentDim") != 0f) throw new Exception("Disabling must restore original");
            activeControl.enabled = true;
            EditorSceneManager.MarkSceneDirty(original);
            EditorSceneManager.SaveScene(original);
            Selection.activeGameObject = activeControl.gameObject;
            File.WriteAllText(Report, "PASS: Controls saved in Hard and Easy; 1 restores original; 0.75 applies dim 0.25; disabling resets global state; shader has no compile errors; rendered mean brightness " + originalAverage + " -> " + dimAverage + ".\n");
            Debug.Log("[EnvironmentBrightnessSetup] PASS");
        }
        catch (Exception error)
        {
            if (activeControl != null) { activeControl.brightness = 0.75f; activeControl.enabled = true; activeControl.SendMessage("Update"); }
            File.WriteAllText(Report, "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    static float Capture(string name)
    {
        Camera camera = Camera.main;
        if (camera == null) throw new Exception("MainCamera not found");
        RenderTexture previous = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        float aspect = camera.aspect;
        RenderTexture target = RenderTexture.GetTemporary(960, 540, 24);
        Texture2D image = new Texture2D(960, 540, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = 960f / 540f;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name), image.EncodeToPNG());
            float total = 0f;
            foreach (Color32 pixel in image.GetPixels32()) total += (pixel.r + pixel.g + pixel.b) / (3f * 255f);
            return total / (960f * 540f);
        }
        finally
        {
            camera.targetTexture = previous;
            camera.aspect = aspect;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
