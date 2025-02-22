Shader "Custom/VolumetricFogRaymarching"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.8, 0.8, 0.8, 1)
        _FogDensity ("Fog Density", Float) = 0.05
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightDirection ("Light Direction", Vector) = (0, -1, 0, 0)
        _Scattering ("Scattering Coefficient", Float) = 0.5
        _NumSteps ("Raymarch Steps", Float) = 64
        _MaxDistance ("Max Ray Distance", Float) = 100
        _ShadowStrength ("Shadow Strength", Float) = 1.0
        _ShadowMap ("Shadow Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        Pass
        {
            Name "VolumetricFog"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            // Vertex input structure
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            // Vertex to fragment structure
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            // Shader properties (uniforms)
            half4 _FogColor;
            float _FogDensity;
            half4 _LightColor;
            float4 _LightDirection;
            float _Scattering;
            float _NumSteps;
            float _MaxDistance;
            float _ShadowStrength;
            sampler2D _ShadowMap;
            float4x4 _LightSpaceMatrix;

            // Built-in camera position
            //float3 _WorldSpaceCameraPos;

            // Vertex shader: Pass through vertex data
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Helper function: Simplified shadow sampling
            float SampleShadow(float3 pos)
            {
                float4 shadowCoord = mul(_LightSpaceMatrix, float4(pos, 1.0));
                shadowCoord.xyz /= shadowCoord.w;
                // Convert from clip space [-1,1] to texture space [0,1]
                float2 uvShadow = shadowCoord.xy * 0.5 + 0.5;
                // Sample the shadow map (a more robust implementation would compare depth values)
                float shadowSample = tex2D(_ShadowMap, uvShadow).r;
                return saturate(shadowSample * _ShadowStrength);
            }

            // Fragment shader: Perform raymarching for volumetric fog
            half4 frag (v2f i) : SV_Target
            {
                // Convert screen UV to clip space [-1,1]
                float2 screenPos = i.uv * 2.0 - 1.0;
                float4 clipPos = float4(screenPos, 0, 1);
                // Reconstruct view-space position
                float4 viewPos = mul(unity_CameraInvProjection, clipPos);
                viewPos /= viewPos.w;
                // Compute ray direction in world space
                float3 rayDir = normalize(mul((float3x3)UNITY_MATRIX_V, viewPos.xyz));
                float3 rayOrigin = _WorldSpaceCameraPos;

                // Set up raymarching parameters
                float stepSize = _MaxDistance / _NumSteps;
                float fogAccum = 0.0;
                float3 currentPos = rayOrigin;

                // Raymarch loop: sample along the ray
                for (int s = 0; s < (int)_NumSteps; s++)
                {
                    // Here, the fog density is constant; a more advanced version could use noise or depth-based attenuation
                    float density = _FogDensity;

                    // Compute light scattering: dot product between normalized light direction and ray direction
                    float lightIntensity = saturate(dot(normalize(_LightDirection.xyz), rayDir));

                    // Sample shadow at the current position to modulate light contribution
                    float shadow = SampleShadow(currentPos);

                    // Accumulate fog effect considering scattering, shadowing, and step size
                    fogAccum += density * _Scattering * lightIntensity * shadow * stepSize;

                    // Move to the next sample along the ray
                    currentPos += rayDir * stepSize;
                }

                // Interpolate the final fog color based on the accumulated effect
                float3 finalColor = lerp(float3(0,0,0), _FogColor.rgb, fogAccum);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}