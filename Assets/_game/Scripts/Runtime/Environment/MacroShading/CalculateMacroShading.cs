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
        public enum ShaderPass
        {
            // Ordered by frame time in HDRP
            ///<summary>Object Depth pre-pass, only the depth of the object will be rendered.</summary>
            DepthPrepass = 1,
            ///<summary>Forward pass, render the object color.</summary>
            Forward = 0,
        }
        
        private static readonly int SourcePropertyId = Shader.PropertyToID("source");
        private static readonly int ResultPropertyID = Shader.PropertyToID("result");
        private static readonly int MaxDepthPropertyId = Shader.PropertyToID("max_depth");
        private static readonly int SinAlphaPropertyId = Shader.PropertyToID("sin_alpha");
        private static readonly int PixelsToUnitsPropertyId = Shader.PropertyToID("pixels_to_units");
        private static readonly int ResultResolutionPropertyId = Shader.PropertyToID("result_resolution");
        [SerializeField] private int resolution = 512;
        [SerializeField] private float rectSize = 200;
        [SerializeField] private RawImage debug;
        [SerializeField] private string shaderPassName;
        [SerializeField] private LayerMask layerMask = 1;
        [SerializeField] private ComputeShader analyzeShader;
        [SerializeField] private float threshold = 0.05f;
        [SerializeField] private float grayZoneAngle = 20;
        [SerializeField] private int resultResolution = 1024;
        [SerializeField] private float r;
        public ShaderPass shaderPass = ShaderPass.Forward;
        public float ResultLightness => (float)_result[0] * _resultMultiplier / resultResolution;
        
        private Shader _shader;
        private RenderTexture _tex;
        private Material _material;
        private int _shaderPass;
        private ComputeBuffer _analyzeBuffer;
        private int[] _result;
        private float _resultMultiplier;

        private RenderQueueType RenderQueueType => RenderQueueType.AllOpaque;
        public SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque & (~SortingCriteria.QuantizedFrontToBack); //HDUtils.k_OpaqueSortingCriteria
        //private ShaderTagId[] _forwardShaderTags;
        static ShaderTagId[] forwardShaderTags;
        static ShaderTagId[] depthShaderTags;
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _tex = RenderTexture.GetTemporary(resolution, resolution, 0, GraphicsFormat.R32G32B32A32_SFloat);
            _tex.enableRandomWrite = true;
            _tex.Create();
            _shader = Shader.Find("Renderers/MacroShadingPass");
            _material = new Material(_shader);
            _shaderPass = _material.FindPass(shaderPassName);
            _resultMultiplier = 4f / (resolution * resolution);
            if (debug != null)
            {
                debug.texture = _tex;
            }
            forwardShaderTags = new ShaderTagId[]
            {
                HDShaderPassNames.s_ForwardName,            // HD Lit shader
                HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };

            depthShaderTags = new ShaderTagId[]
            {
                HDShaderPassNames.s_DepthForwardOnlyName,
                HDShaderPassNames.s_DepthOnlyName,
                HDShaderPassNames.s_EmptyName,              // Add an empty slot for the override material
            };
            /*_forwardShaderTags = new []
            {
                HDShaderPassNames.s_ForwardName,            // HD Lit shader
                HDShaderPassNames.s_ForwardOnlyName,        // HD Unlit shader
                HDShaderPassNames.s_SRPDefaultUnlitName,    // Cross SRP Unlit shader
                new ShaderTagId(shaderPassName),              // Add an empty slot for the override material
            };*/
            _material.SetFloat(Shader.PropertyToID("_FadeValue"), fadeValue);
            _analyzeBuffer = new ComputeBuffer(1, 4);
            _result = new int[1];
        }
        
        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layerMask;
        }
        ShaderTagId[] GetShaderTagIds()
        {
            if (shaderPass == ShaderPass.DepthPrepass)
                return depthShaderTags;
            else
                return forwardShaderTags;
        }
        protected override void Execute(CustomPassContext ctx)
        {
            if(_shader == null || analyzeShader == null)
            {
                return;
            }
            var shaderPasses = GetShaderTagIds();

            //var stateBlock = new RenderStateBlock(0)
            //{
            //    depthState = new DepthState(false, CompareFunction.LessEqual),
            //    stencilState = new StencilState(false, (byte)UserStencilUsage.AllUserBits, (byte)UserStencilUsage.AllUserBits),
            //    stencilReference = 0,
            //};

            PerObjectData renderConfig = HDUtils.GetRendererConfiguration(false, false);

            var result = new RendererListDesc(shaderPasses, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRange(RenderQueueType),
                sortingCriteria = sortingCriteria,
                excludeObjectMotionVectors = false,
                overrideShader = _shader,
                overrideMaterial = null,
                overrideMaterialPassIndex = 0,
                overrideShaderPassIndex = _shaderPass,
                stateBlock = null,
                layerMask = layerMask,
            };

            var renderCtx = ctx.renderContext;
            var rendererList = renderCtx.CreateRendererList(result);
            ctx.cmd.Blit(Texture2D.blackTexture, _tex);
            CoreUtils.SetRenderTarget(ctx.cmd, _tex, clearFlags);
            //ctx.cmd.SetRenderTarget(_tex, clearFlags);
            HDRenderPipeline.RenderForwardRendererList(ctx.hdCamera.frameSettings, rendererList, true, ctx.renderContext, ctx.cmd);
            //CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, rendererList);
            _result[0] = 0;
            _analyzeBuffer.SetData(_result);
            analyzeShader.SetFloat(MaxDepthPropertyId, threshold);
            analyzeShader.SetFloat(PixelsToUnitsPropertyId, rectSize / resolution);
            analyzeShader.SetFloat(SinAlphaPropertyId, Mathf.Sin(grayZoneAngle * Mathf.Deg2Rad));
            analyzeShader.SetInt(ResultResolutionPropertyId, resultResolution);
            analyzeShader.SetTexture(0, SourcePropertyId, _tex);
            analyzeShader.SetBuffer(0, ResultPropertyID, _analyzeBuffer);
            ctx.cmd.DispatchCompute(analyzeShader, 0, resolution / 16, resolution / 16, 1);
            ctx.cmd.RequestAsyncReadback(_analyzeBuffer, Readback);
            //_analyzeBuffer.GetData(_result);
             
        }

        private void Readback(AsyncGPUReadbackRequest readback)
        {
            _result[0] = readback.GetData<int>()[0];
            r = ResultLightness;
        }

        protected override void Cleanup()
        {
            RenderTexture.ReleaseTemporary(_tex);
            _analyzeBuffer.Dispose();
        }
    }
}