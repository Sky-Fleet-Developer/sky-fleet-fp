Shader "Custom/LitTessQuad"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TessellationFactor("Tessellation Factor", Range(1, 64)) = 8 // Фактор разбиения (примерно квадратный корень от числа клеток)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        
        Pass
        {
            Name "Forward"
            Tags {"LightMode"="UniversalForward"}
            
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero
            AlphaToMask Off
            
            HLSLINCLUDE
                #pragma vertex Vert
                #pragma hull HSControlPoint
                #pragma domain DS
                #pragma fragment Frag
                
               // #include "Packages/com.unity.render-pipelines.high-definition/HDRP.hlsl"
               // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.lmh"
               // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SurfaceInput.h"
               // #include "Packages/com.unity.render-pipelines.high-definition/ShaderLibrary/LightingEngine.h"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
                
                struct appdata
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float4 posCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normalWS : NORMAL;
                };

                struct patchData
                {
                    float4 edges[4]; // 4 control points for a quad
                };

                Texture2D _MainTex;
                SamplerState sampler_MainTex;
                float4 _Color;
                float _TessellationFactor;

                v2f Vert(appdata IN)
                {
                    v2f OUT;
                    OUT.posCS = TransformObjectToHClip(IN.positionOS);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    return OUT;
                }

                patchData HSControlPoint(uint id : SV_PrimitiveID,
                                         InputPatch<appdata, 4> inputPatch,
                                         uint patchId : SV_PatchConstant)
                {
                    patchData pd;
                    pd.edges[id % 4] = inputPatch[id].positionOS;
                    return pd;
                }

                [domain("quad")]
                v2f DS(InputPatch<patchData, 1> p,
                       float2 uv : SV_DomainLocation,
                       const OutputPatch<v2f, 4>& triStream)
                {
                    v2f OUT;
                    
                    // Билинейная интерполяция положения вершин для расчета позиций новых точек
                    float3 edgeA = lerp(p[0].edges[0], p[0].edges[1], uv.y); // нижний ряд
                    float3 edgeB = lerp(p[0].edges[3], p[0].edges[2], uv.y); // верхний ряд
                    float3 finalPos = lerp(edgeA, edgeB, uv.x);
                    
                    OUT.posCS = TransformObjectToHClip(float4(finalPos, 1));
                    OUT.normalWS = normalize(TransformNormalObjectToWorld(float3(0, 0, 1)));
                    OUT.uv = TRANSFORM_TEX(uv, _MainTex);
                    
                    return OUT;
                }

                float4 Frag(v2f i) : SV_TARGET
                {
                    SurfaceData surfaceData;
                    InitializeSurfaceData(i.uv, _MainTex, sampler_MainTex, surfaceData);

                    Light mainLight = GetMainLight();
                    float3 lightDir = mainLight.direction;
                    float ndotl = saturate(dot(i.normalWS, lightDir));

                    return float4(surfaceData.baseColor.rgb * _Color.rgb * ndotl, 1);
                }

            ENDHLSL
        }
    }
}