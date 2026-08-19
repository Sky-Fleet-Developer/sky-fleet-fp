//#include <UnityShaderUtilities.cginc>
//#include <UnityInstancing.cginc>
//#undef HAVE_MESH_MODIFICATION
//#define TESSELLATION_ON
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitConstantPass.hlsl"
//#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
//#include_with_pragmas "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#define TESSELLATION_INTERPOLATE_UV(name, uv) output.name = lerp(lerp(input0.name, input1.name, uv.y), lerp(input3.name, input2.name, uv.y), uv.x)

VaryingsMeshToDS InterpolateWithUvCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, VaryingsMeshToDS input3, float2 uvCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    TESSELLATION_INTERPOLATE_UV(positionRWS, uvCoords);
    output.tessellationFactor = 0.0; // Not used, just to silent the shader compiler
    TESSELLATION_INTERPOLATE_UV(normalWS, uvCoords);
    #ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_UV(tangentWS, uvCoords);
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_UV(texCoord0, uvCoords);
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_UV(texCoord1, uvCoords);
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD2
    TESSELLATION_INTERPOLATE_UV(texCoord2, uvCoords);
    #endif
    #ifdef VARYINGS_DS_NEED_TEXCOORD3
    TESSELLATION_INTERPOLATE_UV(texCoord3, uvCoords);
    #endif
    #ifdef VARYINGS_DS_NEED_COLOR
    TESSELLATION_INTERPOLATE_UV(color, uvCoords);
    #endif

    return output;
}
#ifdef VARYINGS_NEED_PASS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"
VaryingsPassToDS InterpolateWithUvCoordsPassToDS(VaryingsPassToDS input0, VaryingsPassToDS input1, VaryingsPassToDS input2, VaryingsPassToDS input3, float2 uvCoords)
{
    VaryingsPassToDS output;

    TESSELLATION_INTERPOLATE_UV(previousPositionRWS, uvCoords);

    return output;
}
#endif

VaryingsToDS InterpolateWithUvCoordsToDS(VaryingsToDS input0, VaryingsToDS input1, VaryingsToDS input2, VaryingsToDS input3, float2 uvCoords)
{
    VaryingsToDS output;

    output.vmesh = InterpolateWithUvCoordsMeshToDS(input0.vmesh, input1.vmesh, input2.vmesh, input3.vmesh, uvCoords);
    #ifdef VARYINGS_NEED_PASS
    output.vpass = InterpolateWithUvCoordsPassToDS(input0.vpass, input1.vpass, input2.vpass, input3.vpass, uvCoords);
    #endif
    return output;
}

struct TessellationFactorsQuad
{
    float edge[4] : SV_TessFactor;
    float inside[2] : SV_InsideTessFactor;
};

TessellationFactorsQuad constantHS(InputPatch<PackedVaryingsToDS, 4> patch) {
    TessellationFactorsQuad o;
    UNITY_SETUP_INSTANCE_ID(patch[0].vmesh);
    VaryingsToDS varying0 = UnpackVaryingsToDS(patch[0]);
    float tessFactor = varying0.vmesh.tessellationFactor;

    o.edge[0] = tessFactor; // Левый край
    o.edge[1] = tessFactor; // Нижний край
    o.edge[2] = tessFactor; // Правый край
    o.edge[3] = tessFactor; // Верхний край
                
    o.inside[0] = tessFactor; // Внутреннее разбиение по X
    o.inside[1] = tessFactor; // Внутреннее разбиение по Y
    return o;
}

//[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("quad")]
[partitioning("fractional_odd")]
[outputtopology("triangle_ccw")]
[outputcontrolpoints(4)]
[patchconstantfunc("constantHS")]
PackedVaryingsToDS hull_quad(InputPatch<PackedVaryingsToDS, 4> patch, uint id : SV_OutputControlPointID) {
    return patch[id];
}


[domain("quad")]
PackedVaryingsToPS domain_quad(TessellationFactorsQuad tessFactors, float2 uvDomain : SV_DomainLocation, const OutputPatch<PackedVaryingsToDS, 4> patch) {
    UNITY_SETUP_INSTANCE_ID(patch[0].vmesh);

    VaryingsToDS varying0 = UnpackVaryingsToDS(patch[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(patch[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(patch[2]);
    VaryingsToDS varying3 = UnpackVaryingsToDS(patch[3]);
    
    VaryingsToDS varying = InterpolateWithUvCoordsToDS(varying0, varying1, varying2, varying3, uvDomain);

    //#ifdef VARYINGS_DS_NEED_POSITIONPREDISPLACEMENT
    //varying.vmesh.positionPredisplacementRWS = varying.vmesh.positionRWS;
    //#endif
    
    #ifdef VARYINGS_DS_NEED_TEXCOORD0
    float height;
    SampleHeightmap_float(sampler_source_heightmap, varying.vmesh.texCoord0, height);
    varying.vmesh.positionRWS.y += height * height_scale;
    #endif
    
    #ifdef VARYINGS_DS_NEED_POSITIONPREDISPLACEMENT
    varying.vmesh.positionPredisplacementRWS = varying.vmesh.positionRWS;
    #endif

    #ifdef HAVE_TESSELLATION_MODIFICATION
    varying.vmesh = ApplyTessellationModification(varying.vmesh, _TimeParameters.xyz);
    #endif
    
    return VertTesselation(varying);
}

