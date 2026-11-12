#ifndef DS_HALFTONE_REACTION_INCLUDED
#define DS_HALFTONE_REACTION_INCLUDED
#include "../../EnvironmentReaction/Shaders/DS_TunnelPulse.hlsl"
#ifndef SHADERGRAPH_PREVIEW
float _DS_BeatPulse;
float _DS_Bass;
float _DS_Breath;
float _DS_Dim;
float _DS_Desat;
float _DS_Flash;
#endif
void DS_HalftoneReaction_float(float3 BaseColor, float Enabled, float Strength, float3 Tint, out float3 Result)
{
    Result = BaseColor;
    if (Enabled < 0.5 || Strength <= 0.0) return;
#ifndef SHADERGRAPH_PREVIEW
    float pulse = saturate(_DS_BeatPulse * _DS_Breath * 0.6 + _DS_Bass * 0.15);
    pulse *= 1.0 - saturate(_DS_Dim) * 0.8;
    float3 color, emission;
    DS_EnvColor_float(BaseColor, Tint, pulse, _DS_Desat, _DS_Flash, color, emission);
    Result = lerp(BaseColor, color + emission, saturate(Strength));
#endif
}
#endif
