//VolumeLightsFeature.cs
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Render.VolumeLights
{
    public class VolumeLightsFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public LayerMask layerMask = 0;
            public RenderingLayerMask RenderingLayerMask = 0;
            public Material overrideMaterial;
            public Material blitMaterial;
            public bool clearDepth;
        }

        [Serializable]
        public class OutlineSettings
        {
            public float outlineScale = 1f;
            public float robertsCrossMultiplier = 100;
            public float depthThreshold = 10f;
            public float normalThreshold = 0.4f;
            public float steepAngleThreshold = 0.2f;
            public float steepAngleMultiplier = 25f;
            public Color outlineColor = Color.white;
        }

        public class OutlineData : ContextItem
        {
            public TextureHandle FilterTextureHandle;

            public override void Reset()
            {
                FilterTextureHandle = TextureHandle.nullHandle;
            }
        }

        public Settings featureSettings;
        public OutlineSettings materialSettings;
        private VolumeLightsPassFilter volumeLightsPassFilter;
        private VolumeLightsPassFinal volumeLightsPassFinal;

        public override void Create()
        {
            volumeLightsPassFilter = new VolumeLightsPassFilter(featureSettings);
            volumeLightsPassFinal = new VolumeLightsPassFinal(featureSettings, materialSettings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(volumeLightsPassFilter);
            renderer.EnqueuePass(volumeLightsPassFinal);
        }
    }
}