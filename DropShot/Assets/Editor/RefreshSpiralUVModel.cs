using System.IO;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class RefreshSpiralUVModel
{
    static RefreshSpiralUVModel() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("SpiralUVUpdateResult.txt")) return;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/spiral/spiral.fbx");
        if (!model) return;
        var filter = model.GetComponentInChildren<MeshFilter>(true);
        if (!filter) return;
        const string path = "Assets/Prefab/RoadA.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            File.WriteAllText("SpiralUVUpdateResult.txt", "Updated RoadA mesh from new spiral.fbx without changing transforms or connection points.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
