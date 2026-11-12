using UnityEngine;
using DropShot.Environment;

public class RowGlow : MonoBehaviour
{
    [Tooltip("Assign a separate material for each row, in playback order.")]
    public Material[] rowMaterials = new Material[0];

    [Tooltip("Number of beats for one complete fade in and fade out.")]
    [Min(1f)] public float beatsPerFlash = 2f;

    public Transform cakeRoot;
    public Material decorationMaterial;
    public float lowerBoundary = 2.4f;
    public float upperBoundary = 3.7f;

    private static readonly int GlowId = Shader.PropertyToID("_Glow");

    void Awake()
    {
        AssignCakeRows();
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
        int currentRow = completedFlashes % rowMaterials.Length;

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
            if (i == currentRow)
                glow = brightness;
            material.SetFloat(GlowId, glow);
        }
    }

    void OnDisable()
    {
        ResetGlow();
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
