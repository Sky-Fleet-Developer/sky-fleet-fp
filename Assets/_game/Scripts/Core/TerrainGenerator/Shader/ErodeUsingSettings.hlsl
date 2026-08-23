#include "ErosionFilter.hlsl"
//#include "ErosionSettings.hlsl"

float4 ErodeUsingSettings(float2 uv, float3 heightAndSlope, out float debug, out float ridgeMap)
{
    // ------------------------------------------------------------------------
    // Terrain parameters not used in the erosion function itself.
    // ------------------------------------------------------------------------
    
    // Control over whether the erosion effect raises or lowers the terrain.
    //  x: An offset value between -1 and 1, where a value of -1 only lowers, while
    //     1 only raises. The offset is proportional to the erosion strength
    //     parameter, so if that parameter is the same for the entire terrain, the
    //     effect of the height offset will move the entire terrain surface up or
    //     down by the same emount.
    //  y: A value between 0 and 1 which is the degree to which the offset value is
    //     replaced by the negated erosion fade target value. This has the effect
    //     of only raising at valleys and only lowering at peaks, which, due to how
    //     the erosion filter works, has the effect of largely preserving the minima
    //     and maxima of the terrain.
    float2 TERRAIN_HEIGHT_OFFSET = float2(0.0, 0.0);
    float fadeTarget = heightAndSlope.x - 0.5f;
    //float ridgeMap;
    return ErosionFilter(uv, heightAndSlope, fadeTarget, erosion_strength, erosion_gully_weight, erosion_detail,
        erosion_rounding, erosion_onset, erosion_assumed_slope,
        erosion_scale, erosion_octaves, erosion_lacunarity,
        erosion_gain, erosion_cell_scale, erosion_normalization,
        ridgeMap, debug);
}