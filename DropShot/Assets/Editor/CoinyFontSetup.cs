using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CoinyFontSetup
{
    const string AssetPath = "Assets/Fonts/Coiny/Coiny SDF.asset";

    static CoinyFontSetup()
    {
        EditorApplication.delayCall += Create;
    }

    static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Create;
            return;
        }
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (existing != null) return;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Coiny/Coiny-Regular.ttf");
        if (source == null) return;
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Coiny SDF";
        font.TryAddCharacters("0123456789 COMBO");
        AssetDatabase.CreateAsset(font, AssetPath);
        foreach (Texture2D atlas in font.atlasTextures)
            AssetDatabase.AddObjectToAsset(atlas, font);
        AssetDatabase.AddObjectToAsset(font.material, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
        Bind(font);
        Debug.Log("Coiny font asset created and Combo digits connected.");
    }
    static void Bind(TMP_FontAsset font)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font, out string guid, out long fontId);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font.material, out string materialGuid, out long materialId);
        foreach (string scene in new[] { "Game_A_Easy", "Game_B_Easy" })
        {
            string path = "Assets/Scenes/" + scene + ".unity";
            string text = File.ReadAllText(path);
            string updated = Regex.Replace(text,
                @"(?ms)^--- !u!114 &1121752203218061498\r?\n.*?(?=^--- !u!|\z)",
                match => {
                    string block = Regex.Replace(match.Value, @"m_fontAsset: [^\r\n]+",
                        "m_fontAsset: {fileID: " + fontId + ", guid: " + guid + ", type: 2}");
                    return Regex.Replace(block, @"m_sharedMaterial: [^\r\n]+",
                        "m_sharedMaterial: {fileID: " + materialId + ", guid: " + materialGuid + ", type: 2}");
                });
            if (updated != text)
                File.WriteAllText(path, updated, new UTF8Encoding(false));
        }
    }

}
