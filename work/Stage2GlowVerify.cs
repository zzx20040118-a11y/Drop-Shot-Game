using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DropShot.Environment;

[InitializeOnLoad]
public static class Stage2GlowVerify
{
    [Serializable] class Sequence { public string[] groups; }
    [Serializable] class Config { public Sequence[] sequences; }
    static Stage2GlowVerify() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var report = new StringBuilder();
        var preview = EditorSceneManager.NewPreviewScene();
        var rows = new List<RowGlow>();
        var instance = typeof(EnvironmentAudioDirector).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = instance.GetValue(null);
        try
        {
            var holder = new GameObject("Stage2 Verification");
            holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder, preview);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Stage1/Stage2/Stage2_sweets2.fbx");
            var clone = UnityEngine.Object.Instantiate(model, holder.transform);
            var director = holder.AddComponent<EnvironmentAudioDirector>();
            instance.SetValue(null, director);
            typeof(EnvironmentAudioDirector).GetField("<SongRunning>k__BackingField", flags).SetValue(director, true);
            var beat = typeof(EnvironmentAudioDirector).GetField("<BeatFloat>k__BackingField", flags);
            var config = JsonUtility.FromJson<Config>(File.ReadAllText("../work/stage2-glow-config.json"));
            foreach (var sequence in config.sequences)
            {
                var go = new GameObject("Sequence");
                go.transform.SetParent(holder.transform, false);
                var row = go.AddComponent<RowGlow>();
                rows.Add(row);
                row.targetRoot = clone.transform;
                row.objectGroups = sequence.groups;
                row.overrideGlowColor = true;
                row.glowStrength = 1;
                typeof(RowGlow).GetMethod("AssignObjectGroups", flags).Invoke(row, null);
                var groupIds = (int[])typeof(RowGlow).GetField("materialGroups", flags).GetValue(row);
                if (row.rowMaterials.Length == 0) throw new Exception("No glow materials matched.");
                for (int group = 0; group < sequence.groups.Length; group++)
                {
                    beat.SetValue(director, group * 2f + 1f);
                    typeof(RowGlow).GetMethod("Update", flags).Invoke(row, null);
                    int matched = 0;
                    for (int i = 0; i < row.rowMaterials.Length; i++)
                    {
                        float expected = groupIds[i] == group ? 1 : 0;
                        if (Mathf.Abs(row.rowMaterials[i].GetFloat("_Glow") - expected) > 0.001f)
                            throw new Exception("Incorrect glow group at beat " + group);
                        if (groupIds[i] == group) matched++;
                        if (ShaderUtil.ShaderHasError(row.rowMaterials[i].shader)) throw new Exception("Shader error.");
                    }
                    if (matched == 0) throw new Exception("Empty group: " + group);
                    report.AppendLine("Group " + group + ": " + matched + " material(s), beat switching PASS");
                }
                typeof(RowGlow).GetMethod("OnDisable", flags).Invoke(row, null);
                foreach (var mat in row.rowMaterials)
                    if (mat.GetFloat("_Glow") != 0) throw new Exception("Disable did not reset glow.");
                report.AppendLine("Disable reset PASS");
            }
            report.AppendLine("ALL PASS: imported model groups, beat switching, material shaders, disable cleanup.");
        }
        catch (Exception exception) { report.AppendLine(exception.ToString()); }
        finally
        {
            instance.SetValue(null, previous);
            foreach (var row in rows)
            {
                var owned = (List<Material>)typeof(RowGlow).GetField("ownedMaterials", flags).GetValue(row);
                foreach (var mat in owned) UnityEngine.Object.DestroyImmediate(mat);
                owned.Clear();
            }
            EditorSceneManager.ClosePreviewScene(preview);
            File.WriteAllText("../work/stage2-glow-verify.txt", report.ToString());
        }
    }
}
