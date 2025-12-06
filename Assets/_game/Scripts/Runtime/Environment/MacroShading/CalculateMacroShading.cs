#define OUT_TO_TEX
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UI;

namespace Runtime.Environment.MacroShading
{
    [Serializable]
    public class CalculateMacroShading : CustomPass
    {
        private static readonly int SourcePropertyId = Shader.PropertyToID("source");
        private static readonly int ResultPropertyID = Shader.PropertyToID("result");
        private static readonly int MaxDepthPropertyId = Shader.PropertyToID("max_depth");
        private static readonly int ResultResolutionPropertyId = Shader.PropertyToID("result_resolution");
        [SerializeField] private int resolution = 512;
        [SerializeField] private float rectSize = 200;
        [SerializeField] private RawImage debug;
        [SerializeField] private LayerMask layerMask = 1;
        [SerializeField] private ComputeShader analyzeShader;
        [SerializeField] private float threshold = 0.05f;
        [SerializeField] private float grayZoneAngle = 20;
        [SerializeField] private int resultResolution = 1024;
        [SerializeField] private float r;
        public float ResultLightness => r;
        
#if OUT_TO_TEX
        private RenderTexture _tex;
#endif
        private ComputeBuffer _analyzeBuffer;
        private int[] _result;
        private float _resultMultiplier;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
#if OUT_TO_TEX
            _tex = RenderTexture.GetTemporary(resolution, resolution, 1, GraphicsFormat.R32G32B32A32_SFloat);
            _tex.enableRandomWrite = true;
            _tex.Create();
#endif
            _resultMultiplier = 0.0625f / (resolution * resolution);
#if OUT_TO_TEX
            if (debug != null)
            {
                debug.texture = _tex;
            }
#endif
            cmd.SetRenderTarget(_tex);
            _analyzeBuffer = new ComputeBuffer(1, 4);
            _result = new int[1];
        }
        
        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layerMask;
        }

        protected override void Execute(CustomPassContext ctx)
        {
            _result[0] = 0;
            _analyzeBuffer.SetData(_result);
            ctx.cmd.SetComputeFloatParam(analyzeShader, MaxDepthPropertyId, threshold);
            ctx.cmd.SetComputeIntParam(analyzeShader, ResultResolutionPropertyId, resultResolution);
            ctx.cmd.SetComputeTextureParam(analyzeShader, 0, SourcePropertyId, ctx.cameraDepthBuffer);
            ctx.cmd.SetComputeBufferParam(analyzeShader, 0, ResultPropertyID, _analyzeBuffer);

            ctx.cmd.DispatchCompute(analyzeShader, 0, resultResolution / 8, resultResolution / 8, 1);
            ctx.cmd.RequestAsyncReadback(_analyzeBuffer, Readback);
        }

        private void Readback(AsyncGPUReadbackRequest readback)
        {
            _result[0] = readback.GetData<int>()[0];
            r = _result[0] * _resultMultiplier / resultResolution;
        }

        protected override void Cleanup()
        {
#if OUT_TO_TEX
            RenderTexture.ReleaseTemporary(_tex);
#endif
            _analyzeBuffer.Dispose();
        }
    }
}