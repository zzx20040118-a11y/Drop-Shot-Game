using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortalDonutInspect
{
    static PortalDonutInspect() { EditorApplication.delayCall += Read; }
    static void Read()
    {
        string path = "../work/portal-donut-bounds.txt";
        if (File.Exists(path)) return;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Stage1/T_Donut/donut.obj");
        if (model == null) return;
        string result = "";
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            Bounds b = filter.sharedMesh.bounds;
            Vector3 c = filter.transform.TransformPoint(b.center);
            Vector3 size = b.size;
            result += string.Format(CultureInfo.InvariantCulture, "{0}|{1},{2},{3}|{4},{5},{6}\n", filter.name, c.x,c.y,c.z,size.x,size.y,size.z);
        }
        File.WriteAllText(path,result);
    }
}

