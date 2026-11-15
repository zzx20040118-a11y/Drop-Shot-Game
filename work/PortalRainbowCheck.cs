using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortalRainbowCheck
{
    static PortalRainbowCheck() { EditorApplication.delayCall += Check; }

    static void Check()
    {
        const string path = "Assets/VFX/PortalEspacial/Mat/SG_PortalRainbow_BHard.shadergraph";
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        var result = new StringBuilder();
        result.AppendLine("Shader imported: " + (shader != null));
        if (shader != null)
        {
            result.AppendLine("Shader errors: " + ShaderUtil.ShaderHasError(shader));
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                result.AppendLine(message.severity + ": " + message.message);
        }
        File.WriteAllText("../work/portal-rainbow-check.txt", result.ToString());
    }
}
