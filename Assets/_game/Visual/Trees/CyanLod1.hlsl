#ifndef CYAN_LOD1
#define CYAN_LOD1

// Custom function of the LOD1 shader graph (CyanLod1.shadergraph): the cheapest look of the crystal and the petals of the
// Cyan Tree. One gradient between two tints, no textures, no layers, no view dependent work. The same graph serves both:
//   gradient_source 0 (crystal): the gradient runs along the crystal, from the vertex color g (0 foot .. 1 tip, written by
//                                the greybox generator, GameEditor.Trees.CyanTreeGreybox); old crystal (vertex color b)
//                                turns milky
//   gradient_source 1 (petal):   the gradient runs along the petal, from the uv x (0 base .. PETAL_UV_END tip)

// Artistic values
#define LOD1_MILK float3(0.52, 0.76, 0.78) // color of old, cloudy crystal
#define LOD1_MILK_SHARE 0.55               // how much of the crystal turns milky at age 1
#define LOD1_PETAL_UV_END 0.985            // uv x of the tip of a petal (CyanTreeGreybox.PetalOutline)

void CyanLod1Fragment_float(float4 vertex_color, float2 uv, float3 normal_os,
    float4 tint_base, float4 tint_tip, float gradient_source, float smoothness_in, float glow,
    out float3 base_color, out float smoothness, out float3 emission, out float3 normal_out_os)
{
    float petal = step(0.5, gradient_source);
    float along = lerp(vertex_color.g, saturate(uv.x / LOD1_PETAL_UV_END), petal);
    float age = vertex_color.b * (1.0 - petal);

    float3 color = lerp(tint_base.rgb, tint_tip.rgb, along);
    color = lerp(color, LOD1_MILK, age * LOD1_MILK_SHARE);

    base_color = color;
    smoothness = saturate(smoothness_in * (1.0 - 0.4 * age));
    // A faint glow in the color of the surface, stronger at the foot, like the full shader
    emission = color * glow * (1.0 - along * 0.5);
    normal_out_os = normalize(normal_os);
}

#endif
