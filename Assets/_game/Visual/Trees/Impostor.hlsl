#ifndef TREE_IMPOSTOR
#define TREE_IMPOSTOR

// Custom functions of the tree impostor shader graph (TreeImpostor.shadergraph). Far trees are quads facing the camera,
// drawn by Graphics.RenderMeshIndirect from the instances the far field collects (Runtime.Trees.TreeFarField,
// PlacedPrefabPlacement.compute: FarCull): a batch mesh holds a quad for every instance, and the vertex id tells which.
// The picture of a tree comes from the atlases baked by Editor TreeImpostorBaker: a hemisphere of orthographic views
// (Core.Trees.ImpostorLayout; the functions below are its HLSL twin, change them together). The pixel looks at the
// three frames nearest to the direction towards the camera: its ray is intersected with the plane of each frame, the
// frames are sampled where the ray meets them and blended. Normals are stored in the space of the tree.
// Everything is in camera relative world space, where the camera is the origin.

struct FarInstance
{
    float3 position; // pivot, absolute world
    float scale;
    float4 rotation; // quaternion
};

StructuredBuffer<FarInstance> far_visible;
float far_instance_offset;       // per prefab: its first instance in far_visible
float far_vertices_per_instance; // 4: a quad

Texture2D impostor_albedo;       // rgb color, a coverage
Texture2D impostor_normal;       // rgb normal * 0.5 + 0.5 in the space of the tree, a depth
SamplerState impostor_trilinear_clamp_sampler;
float4 impostor_grid;            // x frames along a side of the atlas, y 1 / x
float4 impostor_center;          // per prefab: xyz center of a frame in the space of the prefab, w half the size of it
float4 impostor_fade;            // x distance where the fade in over the models starts, y its width, z the far range

float3 ImpostorRotate(float4 q, float3 v)
{
    float3 t = 2 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

float4 ImpostorInverse(float4 q)
{
    return float4(-q.xyz, q.w);
}

// Hemi-octahedral layout, the same as Core.Trees.ImpostorLayout
float2 ImpostorEncode(float3 direction)
{
    direction.y = max(direction.y, 0);
    float sum = abs(direction.x) + direction.y + abs(direction.z);
    float x = direction.x / sum;
    float z = direction.z / sum;
    return float2(x + z, x - z) * 0.5 + 0.5;
}

float3 ImpostorDecode(float2 uv)
{
    float2 e = uv * 2 - 1;
    float x = (e.x + e.y) * 0.5;
    float z = (e.x - e.y) * 0.5;
    float y = 1 - abs(x) - abs(z);
    return normalize(float3(x, y, z));
}

void ImpostorFrameAxes(float3 direction, out float3 right, out float3 up)
{
    float3 forward = -direction;
    float3 hint = abs(forward.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
    right = normalize(cross(hint, forward));
    up = cross(forward, right);
}

// The frame (column, row): what the pixel at position (camera relative) sees of it
void ImpostorSampleFrame(float2 frame, float3 center, float radius, float4 rotation, float3 position, out float4 albedo,
    out float3 normal_ws)
{
    float3 direction_ts = ImpostorDecode((frame + 0.5) * impostor_grid.y);
    float3 direction = ImpostorRotate(rotation, direction_ts);
    float3 right_ts;
    float3 up_ts;
    ImpostorFrameAxes(direction_ts, right_ts, up_ts);
    float3 right = ImpostorRotate(rotation, right_ts);
    float3 up = ImpostorRotate(rotation, up_ts);

    // The ray from the camera through the pixel meets the plane of the frame (through the center, facing the camera)
    float denominator = dot(position, direction);
    float t = dot(center, direction) / (abs(denominator) > 1e-4 ? denominator : 1e-4);
    float3 hit = position * t - center;
    float2 local = float2(dot(hit, right), dot(hit, up)) / radius;
    float2 uv = (frame + local * 0.5 + 0.5) * impostor_grid.y;
    bool inside = abs(denominator) > 1e-4 && all(abs(local) <= 1);

    albedo = impostor_albedo.Sample(impostor_trilinear_clamp_sampler, uv);
    float4 normal_depth = impostor_normal.Sample(impostor_trilinear_clamp_sampler, uv);
    albedo.a = inside ? albedo.a : 0;
    normal_ws = ImpostorRotate(rotation, normal_depth.rgb * 2 - 1);
}

// impostor_a: xyz center of the tree in camera relative world space, w its radius; impostor_b: its rotation
void ImpostorVertex_float(float3 mesh_position, float3 mesh_normal, float3 mesh_tangent, float4 mesh_uv, float4 mesh_color,
    float vertex_id, out float3 position_os, out float3 normal_os, out float3 tangent_os, out float4 impostor_a,
    out float4 impostor_b)
{
#ifdef SHADERGRAPH_PREVIEW
    position_os = mesh_position;
    normal_os = float3(0, 0, -1);
    tangent_os = float3(1, 0, 0);
    impostor_a = float4(0, 0, 1, 1);
    impostor_b = float4(0, 0, 0, 1);
#else
    uint slot = (uint)vertex_id / (uint)max(far_vertices_per_instance, 1);
    FarInstance tree = far_visible[(uint)far_instance_offset + slot];
    float radius = impostor_center.w * tree.scale;
    float3 center = GetCameraRelativePositionWS(tree.position + ImpostorRotate(tree.rotation, impostor_center.xyz * tree.scale));

    // A quad facing the camera, a little larger than the frame so its edge is never in the picture. In the view matrix
    // the rows are the right, the up and the backwards direction of the camera
    float3 camera_right = UNITY_MATRIX_V[0].xyz;
    float3 camera_up = UNITY_MATRIX_V[1].xyz;
    float3 camera_back = UNITY_MATRIX_V[2].xyz;
    float3 corner = center + (camera_right * mesh_position.x + camera_up * mesh_position.y) * (radius * 1.04);
    position_os = TransformWorldToObject(corner);
    normal_os = TransformWorldToObjectNormal(camera_back);
    tangent_os = TransformWorldToObjectDir(camera_right);
    impostor_a = float4(center, radius);
    impostor_b = tree.rotation;
#endif
}

float ImpostorNoise(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

void ImpostorFragment_float(float4 impostor_a, float4 impostor_b, float3 position_ws, out float3 base_color,
    out float3 normal_ws, out float metallic, out float smoothness, out float occlusion, out float alpha)
{
#ifdef SHADERGRAPH_PREVIEW
    base_color = 0.5;
    normal_ws = float3(0, 1, 0);
    metallic = 0;
    smoothness = 0.3;
    occlusion = 1;
    alpha = 1;
#else
    float3 center = impostor_a.xyz;
    float radius = impostor_a.w;
    float4 rotation = impostor_b;

    // The direction towards the camera, in the space of the tree, picks the frames: the triangle of the grid it is in
    float distance_to_tree = length(center);
    float3 view = ImpostorRotate(ImpostorInverse(rotation), -center / max(distance_to_tree, 1e-3));
    float frames = impostor_grid.x;
    float2 grid = ImpostorEncode(view) * frames - 0.5;
    float2 low = floor(grid);
    float2 fraction = grid - low;
    float2 frame_a;
    float2 frame_b;
    float2 frame_c;
    float3 weights;
    if (fraction.x + fraction.y < 1)
    {
        frame_a = low;
        frame_b = low + float2(1, 0);
        frame_c = low + float2(0, 1);
        weights = float3(1 - fraction.x - fraction.y, fraction.x, fraction.y);
    }
    else
    {
        frame_a = low + float2(1, 1);
        frame_b = low + float2(0, 1);
        frame_c = low + float2(1, 0);
        weights = float3(fraction.x + fraction.y - 1, 1 - fraction.x, 1 - fraction.y);
    }
    frame_a = clamp(frame_a, 0, frames - 1);
    frame_b = clamp(frame_b, 0, frames - 1);
    frame_c = clamp(frame_c, 0, frames - 1);

    float4 albedo_a;
    float4 albedo_b;
    float4 albedo_c;
    float3 normal_a;
    float3 normal_b;
    float3 normal_c;
    ImpostorSampleFrame(frame_a, center, radius, rotation, position_ws, albedo_a, normal_a);
    ImpostorSampleFrame(frame_b, center, radius, rotation, position_ws, albedo_b, normal_b);
    ImpostorSampleFrame(frame_c, center, radius, rotation, position_ws, albedo_c, normal_c);

    // Frames that don't have the tree here don't count towards the color and the normal
    float3 weighted = weights * float3(albedo_a.a, albedo_b.a, albedo_c.a);
    float total = weighted.x + weighted.y + weighted.z;
    base_color = (albedo_a.rgb * weighted.x + albedo_b.rgb * weighted.y + albedo_c.rgb * weighted.z) / max(total, 1e-4);
    float3 normal = normal_a * weighted.x + normal_b * weighted.y + normal_c * weighted.z;
    normal_ws = normal / max(length(normal), 1e-4);
    metallic = 0;
    smoothness = 0.3;
    occlusion = 1;

    // Dithered fade in over the models of the trees, and out at the end of the far ring
    float fade = saturate(min((distance_to_tree - impostor_fade.x) / impostor_fade.y, (impostor_fade.z - distance_to_tree) / impostor_fade.y));
    alpha = total * (fade > ImpostorNoise(position_ws * 97) ? 1 : 0);
#endif
}

#endif
