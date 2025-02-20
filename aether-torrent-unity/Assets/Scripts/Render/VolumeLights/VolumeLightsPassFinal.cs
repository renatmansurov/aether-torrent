//VolumeLightsPassFinal.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Render.VolumeLights
{
    public class VolumeLightsPassFinal : ScriptableRenderPass
    {
        private class PassData
        {
            internal TextureHandle FilterTextureHandle;
            internal TextureHandle OpaqueTextureHandle;
            internal Material Material;
        }

        private static readonly int FilterTexture = Shader.PropertyToID("_FilterTexture");
        private static readonly int OutlineScale = Shader.PropertyToID("_OutlineScale");
        private static readonly int RobertsCrossMultiplier = Shader.PropertyToID("_RobertsCrossMultiplier");
        private static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        private static readonly int SteepAngleThreshold = Shader.PropertyToID("_SteepAngleThreshold");
        private static readonly int SteepAngleMultiplier = Shader.PropertyToID("_SteepAngleMultiplier");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

        private readonly Material blitMaterial;

        public VolumeLightsPassFinal(VolumeLightsFeature.Settings settings, VolumeLightsFeature.OutlineSettings outlineSettings)
        {
            renderPassEvent = settings.renderPassEvent;
            blitMaterial = settings.blitMaterial;

            if (blitMaterial == null)
                return;

            blitMaterial.SetFloat(OutlineScale, outlineSettings.outlineScale);
            blitMaterial.SetFloat(RobertsCrossMultiplier, outlineSettings.robertsCrossMultiplier);
            blitMaterial.SetFloat(DepthThreshold, outlineSettings.depthThreshold);
            blitMaterial.SetFloat(NormalThreshold, outlineSettings.normalThreshold);
            blitMaterial.SetFloat(SteepAngleThreshold, outlineSettings.steepAngleThreshold);
            blitMaterial.SetFloat(SteepAngleMultiplier, outlineSettings.steepAngleMultiplier);
            blitMaterial.SetColor(OutlineColor, outlineSettings.outlineColor);
        }

        private static void ExecutePass(PassData passData, RasterGraphContext context)
        {
            if (passData.Material != null)
            {
                passData.Material.SetTexture(FilterTexture, passData.FilterTextureHandle);
            }

            Blitter.BlitTexture(context.cmd, passData.FilterTextureHandle, new Vector4(1, 1, 0, 0), passData.Material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var outlineData = frameData.Get<VolumeLightsFeature.OutlineData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>("OutlinePass_Final", out var passData, new ProfilingSampler("OutlinePass_Final"));

            if (!outlineData.FilterTextureHandle.IsValid())
                return;

            if (blitMaterial == null)
                return;

            passData.Material = blitMaterial;
            passData.FilterTextureHandle = outlineData.FilterTextureHandle;

            builder.AllowPassCulling(false);
            builder.UseTexture(passData.FilterTextureHandle);
            builder.SetRenderAttachment(resourceData.cameraColor, index: 0);
            builder.SetRenderFunc<PassData>(ExecutePass);
        }
    }
}