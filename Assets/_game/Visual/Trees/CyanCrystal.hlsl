#ifndef CYAN_CRYSTAL
#define CYAN_CRYSTAL

// Custom function of the cyan crystal shader graph (CyanCrystal.shadergraph): the semi-opaque crystal that coats the
// Cyan Tree. All the look is here, so the graph is only inputs, this function and outputs.
// Vertex color, written by the greybox generator (GameEditor.Trees.CyanTreeGreybox):
//   r  random per crystal, 0..1: varies the tint and the veins from one crystal to the next
//   g  along the crystal, 0 at its foot .. 1 at its tip
//   b  age of the tree, 0 young .. 1 old: old crystal is cloudy
//   a  thickness of the crystal, 0 thin .. 1 thick: thick parts are deeper in color

// Artistic value: how much the crystal glows
// Debug: 0 normal, 1 a flat color, 2 shows the vertex color as the color, 3 and 4 show the trunk data
// (checks that the meshes carry it)
#define CRYSTAL_DEBUG 0
#define CRYSTAL_GLOW 0.22

float CrystalHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Smooth value noise, 0..1
float CrystalNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = CrystalHash(i);
    float n100 = CrystalHash(i + float3(1, 0, 0));
    float n010 = CrystalHash(i + float3(0, 1, 0));
    float n110 = CrystalHash(i + float3(1, 1, 0));
    float n001 = CrystalHash(i + float3(0, 0, 1));
    float n101 = CrystalHash(i + float3(1, 0, 1));
    float n011 = CrystalHash(i + float3(0, 1, 1));
    float n111 = CrystalHash(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
}

// Thin lines where the noise crosses its middle: fractures and veins inside the crystal, 0..1
float CrystalVeins(float3 p, float width)
{
    float n = CrystalNoise(p);
    return 1.0 - smoothstep(0.0, width, abs(n - 0.5));
}

// Textures are sampled in three planes along the object axes (the meshes have no usable uvs). Scales are repetitions per
// meter of object space
#define CRYSTAL_ICE_SCALE 0.9
#define CRYSTAL_CRACKS_SCALE 0.55
#define CRYSTAL_MILKY_SCALE 0.45
#define CRYSTAL_NORMAL_STRENGTH 0.55
// Fake volume: the cracks are seen again in layers inside (material properties: how many, how far apart, how dark), along the view ray (object space
// meters), fading with depth; they shift with the view like things inside the crystal
#define CRYSTAL_MAX_LAYERS 8
#define CRYSTAL_DEPTH_STRENGTH 1.2
// The trunk inside the coat: dark bark that fades into the teal of the crystal with the depth (meters along the view ray)
#define CRYSTAL_TRUNK_COLOR float3(0.05, 0.035, 0.03)
#define CRYSTAL_TRUNK_FADE 0.18
// The trunk shows through the solid surface as a dark mass: how far in it is still seen (meters) and how dark it is
#define CRYSTAL_TRUNK_SHOW 0.2
#define CRYSTAL_TRUNK_SHADOW 0.8

float3 CrystalPlanes(float3 normal_os)
{
    float3 w = pow(abs(normal_os), 4.0);
    return w / max(w.x + w.y + w.z, 1e-4);
}

float4 CrystalTriplanar(UnityTexture2D tex, UnitySamplerState s, float3 p, float3 w)
{
    return SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.zy) * w.x
         + SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.xz) * w.y
         + SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.xy) * w.z;
}

// A DirectX normal map in three planes, blended into the object-space normal (whiteout)
float3 CrystalTriplanarNormal(UnityTexture2D tex, UnitySamplerState s, float3 p, float3 w, float3 n, float strength)
{
    float3 tx = SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.zy).xyz * 2.0 - 1.0;
    float3 ty = SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.xz).xyz * 2.0 - 1.0;
    float3 tz = SAMPLE_TEXTURE2D(tex.tex, s.samplerstate, p.xy).xyz * 2.0 - 1.0;
    tx.y = -tx.y; ty.y = -ty.y; tz.y = -tz.y; // DirectX: green points down
    tx.xy *= strength; ty.xy *= strength; tz.xy *= strength;
    tx = float3(tx.xy + n.zy, abs(tx.z) * n.x);
    ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
    tz = float3(tz.xy + n.xy, abs(tz.z) * n.z);
    return normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
}

void CyanCrystalFragment_float(float4 vertex_color, float3 position_os, float3 normal_os, float3 normal_ws, float3 view_ws,
    float3 view_os,
    UnityTexture2D deep_cracks_tex, UnityTexture2D milky_tex, UnityTexture2D ice_color_tex, UnityTexture2D ice_normal_tex,
    UnityTexture2D ice_roughness_tex, UnitySamplerState tex_sampler,
    float4 tint_light, float4 tint_deep, float layers, float layer_darkening, float layer_depth,
    UnityTexture2D surface_cracks_tex, float2 surface_remap, float4 trunk_axis, float3 trunk_direction,
    out float3 base_color, out float smoothness, out float3 emission, out float translucency_thickness,
    out float3 normal_out_os)
{
    float id = vertex_color.r;
    float along = vertex_color.g;
    float age = vertex_color.b;
    float thickness = vertex_color.a;

    float3 n_os = normalize(normal_os);
    float3 planes = CrystalPlanes(n_os);
    // Each crystal samples its own part of the textures
    float3 shift = float3(id * 7.31, id * 3.17, id * 5.53);

    float milky = CrystalTriplanar(milky_tex, tex_sampler, position_os * CRYSTAL_MILKY_SCALE + shift, planes).r;
    float3 ice = CrystalTriplanar(ice_color_tex, tex_sampler, position_os * CRYSTAL_ICE_SCALE + shift, planes).rgb;
    float ice_rough = CrystalTriplanar(ice_roughness_tex, tex_sampler, position_os * CRYSTAL_ICE_SCALE + shift, planes).r;
    // The surface texture is how much the surface covers the crystal: white is solid surface, dark (the cracks) is a window
    // into the layers inside. surface_remap (x, y) maps the values x..y of it to 0..1: (0, 0) is a fully solid surface
    float surface_cover = CrystalTriplanar(surface_cracks_tex, tex_sampler, position_os * CRYSTAL_CRACKS_SCALE + shift, planes).r;
    surface_cover = saturate((surface_cover - surface_remap.x) / max(surface_remap.y - surface_remap.x, 1e-4));
    float frost = dot(ice, float3(0.3333, 0.3333, 0.3333));

    // Growth zones: the foot is cloudier than the tip, and old crystal is cloudier all over; the milk makes it patchy
    float cloud = saturate(smoothstep(0.15, 1.0, age) * (1.0 - along * 0.55) + (milky - 0.5) * 0.6);
    // Frosted patches on the surface follow the ice texture; more of them on old crystal
    cloud = saturate(cloud + (frost - 0.55) * 0.5 * (0.4 + age));

    // Tint: deeper teal where thick, a little different from crystal to crystal
    float3 deep = tint_deep.rgb;
    float3 light = tint_light.rgb;
    float3 milk = float3(0.52, 0.76, 0.78);
    float3 color = lerp(light, deep, saturate(thickness * 0.8 + (id - 0.5) * 0.5));
    color = lerp(color, milk, cloud * 0.65);
    color *= lerp(0.85, 1.15, saturate((frost - 0.35) * 2.0)); // the frost brightens it a little

    // Edges catch the light: a paler, glassy rim; it belongs to the surface
    float n_dot_v = saturate(dot(normalize(normal_ws), normalize(view_ws)));
    float rim = pow(1.0 - n_dot_v, 3.0);
    float3 surface_color = lerp(color, float3(0.7, 0.92, 0.95), rim * 0.35);

    // Fake volume. The crystal is opaque, so what is inside is painted in layers along the view ray, from the deepest to
    // the nearest, each nearer layer over the deeper ones. They darken with depth. Cloud pockets deep inside darken the
    // body. It is seen only through the windows of the surface texture.
    float3 into = -normalize(view_os); // from the surface into the crystal
    int layer_count = (int) clamp(round(layers), 1.0, (float) CRYSTAL_MAX_LAYERS);
    float deep_milk = CrystalTriplanar(milky_tex, tex_sampler, (position_os + into * layer_depth * (layer_count + 1)) * CRYSTAL_MILKY_SCALE + shift, planes).r;
    float3 interior = lerp(color, deep * 0.45, deep_milk * 0.5 * (1.0 - cloud));

    // The trunk is a cylinder (trunk_axis: a point on its axis, radius in w; trunk_direction: its axis, from the
    // uv channels of the coat). The view ray into the crystal meets it at trunk_depth. No radius: no trunk here
    float trunk_cover = 0.0;
    float trunk_depth = 1000.0;
    float trunk_radius = trunk_axis.w;
    float3 trunk_color = deep * 0.3;
    if (trunk_radius > 0.001)
    {
        float3 axis = normalize(trunk_direction);
        float3 o = position_os - trunk_axis.xyz;
        o -= axis * dot(o, axis);
        float3 d = into - axis * dot(into, axis);
        float a = dot(d, d);
        float b = dot(o, d);
        float c = dot(o, o) - trunk_radius * trunk_radius;
        float disc = b * b - a * c;
        if (a > 1e-5 && disc > 0.0)
        {
            float t = (-b - sqrt(disc)) / a;
            if (t > 0.0)
            {
                trunk_depth = t;
                // Softly at the silhouette of the trunk
                trunk_cover = smoothstep(0.0, 0.05, disc / (a * trunk_radius * trunk_radius));
                float bark = 0.75 + 0.5 * CrystalNoise((position_os + into * t) * 18.0 + shift);
                trunk_color = lerp(CRYSTAL_TRUNK_COLOR * bark, deep * 0.3, 1.0 - exp(-t / CRYSTAL_TRUNK_FADE));
                interior = lerp(interior, trunk_color, trunk_cover);
            }
        }
    }

    float inner_cracks = 0.0;
    [loop]
    for (int k = layer_count; k >= 1; k--)
    {
        float depth = layer_depth * k;
        // The layers behind the trunk are hidden by it
        float visible = lerp(1.0, step(depth, trunk_depth), trunk_cover);
        float3 deep_point = position_os + into * depth;
        float layer = 1.0 - CrystalTriplanar(deep_cracks_tex, tex_sampler, deep_point * CRYSTAL_CRACKS_SCALE * (1.0 + 0.12 * k) + shift + k * 1.713, planes).r;
        // From the nearest layer to the deepest: pale to dark, as much as the darkening says
        float3 layer_color = lerp(lerp(light, float3(1.0, 1.0, 1.0), 0.45), deep * 0.3,
            saturate((k - 1) / (float) max(layer_count - 1, 1) * layer_darkening));
        float strength = saturate(layer * CRYSTAL_DEPTH_STRENGTH) * (1.0 - cloud * 0.5) * visible;
        interior = lerp(interior, layer_color, strength);
        inner_cracks = max(inner_cracks, strength * (k == 1 ? 1.0 : 0.5));
    }

    // The trunk shows through the solid surface as a dark mass that fades with its depth: the crystal is a frosted glass
    // around it
    surface_color = lerp(surface_color, trunk_color, CRYSTAL_TRUNK_SHADOW * trunk_cover * exp(-trunk_depth / CRYSTAL_TRUNK_SHOW));

    // The surface over the interior, as much as the surface texture covers it
    color = lerp(interior, surface_color, surface_cover);
    float veins = inner_cracks * (1.0 - surface_cover);

    // Light passes through the thin parts, the tips and the edges, and not through the thick foot (translucent material)
    translucency_thickness = saturate(lerp(0.15, 1.0, saturate(thickness * 0.8 + (1.0 - along) * 0.35)) + cloud * 0.25 - rim * 0.3);

    // The ice roughness map roughens the surface: frosted spots are matte, the rest is glossy
    smoothness = saturate(lerp(0.92, 0.5, cloud) - (ice_rough - 0.5) * 0.35);

    // Fine surface relief from the ice normal map, only a part of it, so the facets stay flat
    normal_out_os = CrystalTriplanarNormal(ice_normal_tex, tex_sampler, position_os * CRYSTAL_ICE_SCALE + shift, planes, n_os,
        CRYSTAL_NORMAL_STRENGTH);

    // A faint glow from the core, only for the look
    emission = float3(0.04, 0.5, 0.56) * CRYSTAL_GLOW * (0.35 + veins * 0.65) * (1.0 - along * 0.5) * (1.0 - cloud * 0.4);
    base_color = color;

#if CRYSTAL_DEBUG == 1
    base_color = float3(0.3, 0.9, 1.0);
    normal_out_os = n_os;
#elif CRYSTAL_DEBUG == 2
    base_color = vertex_color.rgb;
    normal_out_os = n_os;
#elif CRYSTAL_DEBUG == 3
    // Trunk data: red where the view ray meets the trunk, green where the vertices carry a trunk radius, blue where
    // they carry a direction
    base_color = float3(trunk_cover, trunk_radius > 0.001 ? 1.0 : 0.0, dot(trunk_direction, trunk_direction) > 0.5 ? 1.0 : 0.0);
    normal_out_os = n_os;
#elif CRYSTAL_DEBUG == 4
    // The trunk radius in decimeters as green, the depth of the hit in decimeters as red
    base_color = float3(saturate(trunk_depth * 10.0 / 4.0), saturate(trunk_radius * 10.0 / 4.0), 0.0);
    normal_out_os = n_os;
#endif
}

#endif
