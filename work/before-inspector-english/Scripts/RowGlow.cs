using UnityEngine;
using DropShot.Environment;
using System.Collections.Generic;

public class RowGlow : MonoBehaviour
{
    [Tooltip("Assign a separate material for each row, in playback order.")]
    public Material[] rowMaterials = new Material[0];

    [Tooltip("Number of beats for one complete fade in and fade out.")]
    [Min(1f)] public float beatsPerFlash = 2f;

    [Header("按模型名稱分組（留空沿用原本材質列表）")]
    public Transform targetRoot;
    [Tooltip("每項是一組物件名稱前綴，多個前綴用 | 分隔。")]
    public string[] objectGroups = new string[0];
    [Tooltip("可選：與物件分組一一對應，只讓指定名稱的材質發光，多個名稱用 | 分隔。留空則整組發光。")]
    public string[] groupMaterialNames = new string[0];
    [Min(0f)] public float glowStrength = 1f;
    public bool overrideGlowColor;
    public Color glowColor = Color.white;

    public Transform cakeRoot;
    public Material decorationMaterial;
    public float lowerBoundary = 2.4f;
    public float upperBoundary = 3.7f;

    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    int[] materialGroups;
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Awake()
    {
        AssignCakeRows();
        AssignObjectGroups();
    }

    void AssignObjectGroups()
    {
        if (targetRoot == null || objectGroups == null || objectGroups.Length == 0) return;
        var materials = new List<Material>();
        var groups = new List<int>();
        Renderer[] renderers = targetRoot.GetComponentsInChildren<Renderer>(true);
        for (int group = 0; group < objectGroups.Length; group++)
        {
            string[] prefixes = objectGroups[group].Split('|');
            var copies = new Dictionary<Material, Material>();
            foreach (Renderer target in renderers)
            {
                bool matches = false;
                foreach (string prefix in prefixes)
                    if (!string.IsNullOrWhiteSpace(prefix)
                        && target.name.StartsWith(prefix.Trim(), System.StringComparison.Ordinal))
                        matches = true;
                if (!matches) continue;
                Material[] slots = target.sharedMaterials;
                if (!originalMaterials.ContainsKey(target))
                    originalMaterials.Add(target, (Material[])slots.Clone());
                for (int i = 0; i < slots.Length; i++)
                {
                    Material source = slots[i];
                    if (source == null || !source.HasProperty(GlowId)) continue;
                    if (!MatchesMaterial(source, group)) continue;
                    if (!copies.TryGetValue(source, out Material copy))
                    {
                        copy = new Material(source) { name = source.name + " (Beat Glow)" };
                        copy.SetFloat(GlowId, 0f);
                        if (overrideGlowColor && copy.HasProperty("_GlowColor"))
                            copy.SetColor("_GlowColor", glowColor);
                        copies.Add(source, copy);
                        ownedMaterials.Add(copy);
                        materials.Add(copy);
                        groups.Add(group);
                    }
                    slots[i] = copy;
                }
                target.sharedMaterials = slots;
            }
        }
        rowMaterials = materials.ToArray();
        materialGroups = groups.ToArray();
        if (rowMaterials.Length == 0)
            Debug.LogWarning("RowGlow 沒有找到符合分組名稱、且支援 _Glow 的材質。", this);
    }

    bool MatchesMaterial(Material material, int group)
    {
        if (groupMaterialNames == null || group >= groupMaterialNames.Length
            || string.IsNullOrWhiteSpace(groupMaterialNames[group])) return true;

        foreach (string materialName in groupMaterialNames[group].Split('|'))
            if (material.name == materialName.Trim()) return true;
        return false;
    }

    // Only the cake needs automatic material assignment by height.
    void AssignCakeRows()
    {
        if (cakeRoot == null || decorationMaterial == null || rowMaterials == null
            || rowMaterials.Length != 3)
            return;

        foreach (Renderer target in cakeRoot.GetComponentsInChildren<Renderer>(true))
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            float height = cakeRoot.InverseTransformPoint(
                target.transform.TransformPoint(filter.sharedMesh.bounds.center)).y;
            int row = 0;
            if (height >= upperBoundary)
                row = 2;
            else if (height >= lowerBoundary)
                row = 1;
            Material[] materials = target.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != decorationMaterial || rowMaterials[row] == null)
                    continue;
                materials[i] = rowMaterials[row];
                changed = true;
            }
            if (changed)
                target.sharedMaterials = materials;
        }
    }

    void Update()
    {
        EnvironmentAudioDirector director = EnvironmentAudioDirector.Instance;

        if (rowMaterials == null || rowMaterials.Length == 0)
            return;

        if (director == null || !director.SongRunning || director.BeatFloat < 0f)
        {
            ResetGlow();
            return;
        }

        // A value of 2 makes each flash last two music beats.
        float cycle = director.BeatFloat / Mathf.Max(1f, beatsPerFlash);
        int completedFlashes = Mathf.FloorToInt(cycle);
        int rowCount = materialGroups != null ? objectGroups.Length : rowMaterials.Length;
        int currentRow = completedFlashes % rowCount;

        // Progress goes from 0 to 1 during each flash.
        float progress = cycle - completedFlashes;

        // Brightness starts at 0, rises to 1, then returns to 0.
        float brightness = (1f - Mathf.Cos(progress * Mathf.PI * 2f)) / 2f;

        for (int i = 0; i < rowMaterials.Length; i++)
        {
            Material material = rowMaterials[i];
            if (material == null || !material.HasProperty(GlowId))
                continue;

            float glow = 0f;
            int group = materialGroups != null ? materialGroups[i] : i;
            if (group == currentRow)
                glow = brightness * glowStrength;
            material.SetFloat(GlowId, glow);
        }
    }

    void OnDisable()
    {
        ResetGlow();
    }

    void OnDestroy()
    {
        foreach (var original in originalMaterials)
            if (original.Key != null) original.Key.sharedMaterials = original.Value;
        foreach (Material material in ownedMaterials)
            if (material != null) Destroy(material);
    }

    private void ResetGlow()
    {
        if (rowMaterials == null)
            return;

        foreach (Material material in rowMaterials)
        {
            if (material != null && material.HasProperty(GlowId))
                material.SetFloat(GlowId, 0f);
        }
    }
}
