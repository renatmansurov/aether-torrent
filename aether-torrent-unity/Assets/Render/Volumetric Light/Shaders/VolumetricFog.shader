Shader "Hidden/VolumetricFog"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "VolumetricFogRender"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "./VolumetricFog.hlsl"

            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            #pragma multi_compile_local_fragment _ _MAIN_LIGHT_CONTRIBUTION_DISABLED
            #pragma multi_compile_local_fragment _ _ADDITIONAL_LIGHTS_CONTRIBUTION_DISABLED

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D(_VolumeNoiseTexture);
            SAMPLER(sampler_VolumeNoiseTexture);
            float4 _NoiseScale;
            float4 _NoiseSpeed;
            float _NoiseHeightInfluence;
            float _NoiseBlendFactor;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 posWS;
                half4 volumetricFog = VolumetricFog(input.texcoord, input.positionCS.xy, posWS);
                float2 noiseUV = posWS.xz + float2(0.0, posWS.y * _NoiseHeightInfluence);
                float noise1 = SAMPLE_TEXTURE3D(_VolumeNoiseTexture, sampler_VolumeNoiseTexture, noiseUV * _NoiseScale.xy + _Time.yy * _NoiseSpeed.xy).r;
                float noise2 = SAMPLE_TEXTURE3D(_VolumeNoiseTexture, sampler_VolumeNoiseTexture, noiseUV * _NoiseScale.zw + _Time.yy * _NoiseSpeed.zw).r;
                float combinedNoise = (noise1 + noise2 * 0.5) * 0.66;
                combinedNoise = pow(combinedNoise, _NoiseBlendFactor) * _NoiseBlendFactor;
                volumetricFog.rgb *= lerp(1.0, combinedNoise, 1);
                return volumetricFog;
            }
            ENDHLSL
        }

        Pass
        {
            Name "VolumetricFogHorizontalBlur"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./DepthAwareGaussianBlur.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return DepthAwareGaussianBlur(input.texcoord, float2(1.0, 0.0), _BlitTexture, sampler_PointClamp, _BlitTexture_TexelSize.xy);
            }
            ENDHLSL
        }

        Pass
        {
            Name "VolumetricFogVerticalBlur"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./DepthAwareGaussianBlur.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return DepthAwareGaussianBlur(input.texcoord, float2(0.0, 1.0), _BlitTexture, sampler_PointClamp, _BlitTexture_TexelSize.xy);
            }
            ENDHLSL
        }

        Pass
        {
            Name "VolumetricFogUpsampleComposition"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./DepthAwareUpsample.hlsl"

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D_X(_VolumetricFogTexture);
            SAMPLER(sampler_BlitTexture);

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 volumetricFog = DepthAwareUpsample(input.texcoord, _VolumetricFogTexture);
                float4 cameraColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord);
                return float4(cameraColor.rgb * volumetricFog.a + volumetricFog.rgb, cameraColor.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        UsePass "Hidden/VolumetricFog/VOLUMETRICFOGRENDER"

        UsePass "Hidden/VolumetricFog/VOLUMETRICFOGHORIZONTALBLUR"

        UsePass "Hidden/VolumetricFog/VOLUMETRICFOGVERTICALBLUR"

        Pass
        {
            Name "VolumetricFogUpsampleComposition"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "./DepthAwareUpsample.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D_X(_VolumetricFogTexture);
            SAMPLER(sampler_BlitTexture);

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 volumetricFog = DepthAwareUpsample(input.texcoord, _VolumetricFogTexture);
                float4 cameraColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord);
                return float4(cameraColor.rgb * volumetricFog.a + volumetricFog.rgb, cameraColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}