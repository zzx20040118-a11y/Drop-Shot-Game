using UnityEngine;

[ExecuteAlways]
public class HalftoneEnvironmentBrightness : MonoBehaviour
{
    [Range(0f, 1f)] public float brightness = 0.9f;

    void OnEnable()
    {
        ApplyBrightness();
    }

    void Update()
    {
        ApplyBrightness();
    }

    void OnValidate()
    {
        if (isActiveAndEnabled)
            ApplyBrightness();
    }

    void ApplyBrightness()
    {
        Shader.SetGlobalFloat("_DS_EnvironmentDim", 1f - Mathf.Clamp01(brightness));
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_DS_EnvironmentDim", 0f);
    }
}
