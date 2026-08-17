Shader "Renderers/QuadTesselationPass"
{
    Properties
    {
        _TesselationFactor("Tesselation Factor", Float) = 100.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "TerrainForwardPass"
            
            Cull Front
            
            HLSLPROGRAM
            // 1. Объявляем все стадии шейдера
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma fragment frag

            // Включаем необходимые библиотеки HDRP для трансформаций и освещения
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "SampleHeightmap.hlsl"

            // Структуры данных
            struct VS_INPUT {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VS_OUTPUT {
                float3 positionOS : TEXCOORD0;
                float3 normalOS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            // Структуры для тесселяции квадов
            struct HS_CONSTANT_OUTPUT {
                float edges[4]  : SV_TessFactor;
                float inside[2] : SV_InsideTessFactor;
            };

            struct HS_OUTPUT {
                float3 positionOS : TEXCOORD0;
                float3 normalOS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            struct DS_OUTPUT {
                float4 positionCS : SV_Position;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

           /* // Текстуры и параметры
            SamplerState sampler_source_heightmap;
            float height_scale;
            */
            // -------------------------------------------------------------
            // 1. VERTEX SHADER: Просто передает данные дальше (ничего не трансформирует)
            // -------------------------------------------------------------
            VS_OUTPUT vert(VS_INPUT v) {
                VS_OUTPUT o;
                o.positionOS = v.positionOS;
                o.normalOS = v.normalOS;
                o.uv = v.uv;
                return o;
            }

            // -------------------------------------------------------------
            // 2. HULL SHADER: Определяет логику разбиения
            // -------------------------------------------------------------
            HS_CONSTANT_OUTPUT constantHS(InputPatch<VS_OUTPUT, 4> patch) {
                HS_CONSTANT_OUTPUT o;
                float tessFactor = 32.0; // Тут будет ваша логика LOD на основе дистанции

                o.edges[0] = tessFactor; // Левый край
                o.edges[1] = tessFactor; // Нижний край
                o.edges[2] = tessFactor; // Правый край
                o.edges[3] = tessFactor; // Верхний край
                
                o.inside[0] = tessFactor; // Внутреннее разбиение по X
                o.inside[1] = tessFactor; // Внутреннее разбиение по Y
                return o;
            }

            [domain("quad")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(4)]
            [patchconstantfunc("constantHS")]
            HS_OUTPUT hull(InputPatch<VS_OUTPUT, 4> patch, uint id : SV_OutputControlPointID) {
                HS_OUTPUT o;
                o.positionOS = patch[id].positionOS;
                o.normalOS = patch[id].normalOS;
                o.uv = patch[id].uv;
                return o;
            }
            
            [domain("quad")]
            DS_OUTPUT domain(HS_CONSTANT_OUTPUT constantData, float2 uvDomain : SV_DomainLocation, const OutputPatch<HS_OUTPUT, 4> patch) {
                DS_OUTPUT o;

                // Билинейная интерполяция позиции внутри квада
                float3 posOS = lerp(
                    lerp(patch[0].positionOS, patch[1].positionOS, uvDomain.y),
                    lerp(patch[3].positionOS, patch[2].positionOS, uvDomain.y),
                    uvDomain.x
                );

                // Интерполяция UV
                float2 uv = lerp(
                    lerp(patch[0].uv, patch[1].uv, uvDomain.y),
                    lerp(patch[3].uv, patch[2].uv, uvDomain.y),
                    uvDomain.x
                );

                // Выборка высоты (используем SampleLevel, так как в DS нет градиентов для обычного Sample)
                float height;
                SampleHeightmap_float(sampler_source_heightmap, uv, height);
                posOS.y += height * height_scale;

                // Переводим в Мировые и Клиппируемые координаты (HDRP функции)
                float3 posWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(posWS);
                o.positionWS = posWS;
                o.uv = uv;

                return o;
            }

            // -------------------------------------------------------------
            // 4. FRAGMENT SHADER: Выводит финальный цвет (базовый пример)
            // -------------------------------------------------------------
            float4 frag(DS_OUTPUT i) : SV_Target {
                // В простейшем виде возвращаем UV как цвет
                float h = (i.positionWS.y + _WorldSpaceCameraPos_Internal.y) / height_scale;
                return float4(h, h, h, 1.0);
                
                // ПРИМЕЧАНИЕ: Чтобы ландшафт реагировал на свет HDRP, 
                // здесь нужно будет рассчитать PBR-данные (Normal, Albedo, Roughness)
                // и вызвать сборщик G-Buffer (через структуры HDRP, такие как SurfaceData).
            }
            ENDHLSL
        }
    }
}
