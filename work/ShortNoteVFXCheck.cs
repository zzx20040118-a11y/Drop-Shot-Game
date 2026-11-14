using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ShortNoteVFXCheck
{
    static readonly string Report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work/short-note-check.txt"));
    static ShortNoteVFXCheck() { EditorApplication.delayCall += Run; }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Run()
    {
        if (File.Exists(Report)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        try
        {
            Require(Camera.main != null, "MainCamera missing");
            foreach (string name in new[] { "Crash_Collider_NormalNote 1", "Crash_Collider_NormalNote 2" })
                Check(name);
            File.WriteAllText(Report, "PASS: Both candy prefabs. Sprite and shader references; alpha 0 / 0.5 / 1; fixed sprite scale; no shake before window; repeatable shake in window; root/collider unchanged; shared material unchanged; Finish hides sprite and resets per-note properties; empty animation slot.\n");
            Debug.Log("[ShortNoteVFXCheck] PASS");
        }
        catch (Exception error)
        {
            File.WriteAllText(Report, "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    static void Check(string name)
    {
        GameObject note = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + name + ".prefab"));
        note.hideFlags = HideFlags.HideAndDontSave;
        note.transform.position = new Vector3(10000f, 10000f, 10000f);
        ShortNoteVFX vfx = note.GetComponent<ShortNoteVFX>();
        SpriteRenderer burst = null;
        try
        {
            Require(vfx != null && vfx.candy != null && vfx.burstPrefab != null, name + ": missing references");
            Require(vfx.animatorSlot == null, "Animation slot must be optional");
            Require(vfx.candy.sharedMaterial.HasProperty("_NoteVisualOffset"), "Shader property not imported");
            foreach (var message in ShaderUtil.GetShaderMessages(vfx.candy.sharedMaterial.shader))
                Require(message.severity.ToString() != "Error", "Shader compile error: " + message.message);
            Color original = vfx.candy.sharedMaterial.GetColor("_RimColor");
            Vector3 position = note.transform.position;
            Quaternion rotation = note.transform.rotation;
            Vector3 scale = note.transform.localScale;
            vfx.Initialize();
            burst = (SpriteRenderer)typeof(ShortNoteVFX).GetField("burst", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(vfx);
            Require(burst != null && burst.sprite != null && burst.sharedMaterial != null && burst.sharedMaterial.shader.isSupported, "Burst asset missing or unsupported");
            Require(burst.transform.parent == null, "Burst inherits candy scale");
            Vector3 burstScale = burst.transform.localScale;
            MethodInfo late = typeof(ShortNoteVFX).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var block = new MaterialPropertyBlock();
            vfx.UpdateVisual(0, 1, 0.15f); late.Invoke(vfx, null);
            Require(!burst.gameObject.activeSelf, "Idle burst visible");
            vfx.UpdateVisual(0.7, 1, 0.15f); late.Invoke(vfx, null);
            Require(Mathf.Abs(burst.color.a - 0.5f) < 0.001f, "Halfway alpha incorrect");
            vfx.candy.GetPropertyBlock(block);
            Require(block.GetVector("_NoteVisualOffset") == Vector4.zero, "Shake before window");
            vfx.UpdateVisual(1, 1, 0.15f); late.Invoke(vfx, null);
            Require(Mathf.Abs(burst.color.a - 1f) < 0.001f, "Hit alpha incorrect");
            vfx.candy.GetPropertyBlock(block);
            Vector4 offset = block.GetVector("_NoteVisualOffset");
            Require(offset.sqrMagnitude > 0f, "No shake inside window");
            vfx.UpdateVisual(1, 1, 0.15f); late.Invoke(vfx, null);
            vfx.candy.GetPropertyBlock(block);
            Require(block.GetVector("_NoteVisualOffset") == offset, "Shake changes with frozen song clock");
            Require(note.transform.position == position && note.transform.rotation == rotation && note.transform.localScale == scale, "Root moved");
            Require(burst.transform.localScale == burstScale, "Burst resized");
            Require(vfx.candy.sharedMaterial.GetColor("_RimColor") == original, "Shared material changed");
            vfx.Finish();
            vfx.candy.GetPropertyBlock(block);
            Require(!vfx.enabled && !burst.gameObject.activeSelf, "Finish left active prompt");
            Require(block.GetVector("_NoteVisualOffset") == Vector4.zero && block.GetColor("_RimColor") == original, "Finish did not reset");
        }
        finally
        {
            if (burst != null) UnityEngine.Object.DestroyImmediate(burst.gameObject);
            UnityEngine.Object.DestroyImmediate(note);
        }
    }
}
