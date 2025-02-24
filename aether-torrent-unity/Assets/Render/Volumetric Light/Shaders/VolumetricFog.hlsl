#ifndef VOLUMETRIC_FOG_INCLUDED
#define VOLUMETRIC_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "./DeclareDownsampledDepthTexture.hlsl"
#include "./VolumetricShadows.hlsl"
#include "./ProjectionUtils.hlsl"

int _FrameCount;
uint _CustomAdditionalLightsCount;
float _Distance;
float _BaseHeight;
float _MaximumHeight;
float _GroundHeight;
float _Density;
float _Absortion;
float3 _Tint;
int _MaxSteps;
float _Anisotropies[MAX_VISIBLE_LIGHTS + 1];
float _Scatterings[MAX_VISIBLE_LIGHTS + 1];
float _RadiiSq[MAX_VISIBLE_LIGHTS];

// Computes the ray origin, direction, and returns the reconstructed world position for orthographic projection.
float3 ComputeOrthographicParams(float2 uv, float depth, out float3 ro, out float3 rd)
{
    float4x4 viewMatrix = UNITY_MATRIX_V;
    float2 ndc = uv * 2.0 - 1.0;

    rd = normalize(-viewMatrix[2].xyz);
    float3 rightOffset = normalize(viewMatrix[0].xyz) * (ndc.x * unity_OrthoParams.x);
    float3 upOffset = normalize(viewMatrix[1].xyz) * (ndc.y * unity_OrthoParams.y);
    float3 posWs = GetCameraPositionWS() + rd * depth + rightOffset + upOffset;
    ro = posWs - rd * depth;

    return posWs;
}

// Gets the fog density at the given world height.
float GetFogDensity(float posWSy)
{
    float t = saturate((posWSy - _BaseHeight) / (_MaximumHeight - _BaseHeight));
    t = 1.0 - t;
    t = lerp(t, 0.0, posWSy < _GroundHeight);
    return _Density * t;
}

// Gets the main light color at one raymarch step.
float3 GetStepMainLightColor(float3 currPosWS, float phaseMainLight, float density)
{
    #if _MAIN_LIGHT_CONTRIBUTION_DISABLED
	return float3(0.0, 0.0, 0.0);
    #endif
    Light mainLight = GetMainLight();
    mainLight.shadowAttenuation = VolumetricMainLightRealtimeShadow(TransformWorldToShadowCoord(currPosWS));
    #if _LIGHT_COOKIES
	mainLight.color *= SampleMainLightCookie(currPosWS);
    #endif
    return mainLight.color * _Tint * mainLight.shadowAttenuation * phaseMainLight * density * _Scatterings[_CustomAdditionalLightsCount];
}

// Gets the accumulated color from additional lights at one raymarch step.
float3 GetStepAdditionalLightsColor(float2 uv, float3 currPosWS, float3 rd, float density)
{
    #if _ADDITIONAL_LIGHTS_CONTRIBUTION_DISABLED
	return float3(0.0, 0.0, 0.0);
    #endif
    #if _FORWARD_PLUS
	InputData inputData = (InputData)0;
	inputData.normalizedScreenSpaceUV = uv;
	inputData.positionWS = currPosWS;
    #endif
    float3 additionalLightsColor = float3(0.0, 0.0, 0.0);

    LIGHT_LOOP_BEGIN(_CustomAdditionalLightsCount)
        if (_Scatterings[lightIndex] <= 0.0)
            continue;

        Light additionalLight = GetAdditionalPerObjectLight(lightIndex, currPosWS);
        additionalLight.shadowAttenuation = VolumetricAdditionalLightRealtimeShadow(lightIndex, currPosWS, additionalLight.direction);
        #if _LIGHT_COOKIES
		additionalLight.color *= SampleAdditionalLightCookie(lightIndex, currPosWS);
        #endif
        float3 distToPos = _AdditionalLightsPosition[lightIndex].xyz - currPosWS;
        float distToPosMagnitudeSq = dot(distToPos, distToPos);
        float newScattering = smoothstep(0.0, _RadiiSq[lightIndex], distToPosMagnitudeSq) * _Scatterings[lightIndex];

        float phase = CornetteShanksPhaseFunction(_Anisotropies[lightIndex], dot(rd, additionalLight.direction));
        additionalLightsColor += additionalLight.color * additionalLight.shadowAttenuation * additionalLight.distanceAttenuation * phase * density * newScattering;
    LIGHT_LOOP_END

    return additionalLightsColor;
}


// Calculates the volumetric fog. Returns the color in the RGB channels and transmittance in alpha.
float4 VolumetricFog(float2 uv, float2 positionCS, out float3 posWS)
{
    float depth = SampleDownsampledSceneDepth(uv);

    float3 ro;
    float3 rd;
    float iniOffsetToNearPlane;
    float offsetLength;
    float3 rdPhase;

    // Perspective path (active in your case)
    UNITY_BRANCH
    if (unity_OrthoParams.w <= 0)
    {
        ro = GetCameraPositionWS();
        #if !UNITY_REVERSED_Z
        depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
        #endif
        posWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
        float3 offset = posWS - ro;
        offsetLength = length(offset);
        rd = offset / offsetLength;
        rdPhase = rd;

        // Compute an elongation factor based on the angle between the camera forward and the ray.
        float3 camFwd = normalize(-UNITY_MATRIX_V[2].xyz);
        float cosAngle = dot(camFwd, rd);
        float fragElongation = 1.0 / cosAngle;
        iniOffsetToNearPlane = fragElongation * _ProjectionParams.y;
    }
    else
    {
        // Orthographic path
        depth = LinearEyeDepthOrthographic(depth);
        posWS = ComputeOrthographicParams(uv, depth, ro, rd);
        offsetLength = depth;
        rdPhase = normalize(posWS - GetCameraPositionWS());
        iniOffsetToNearPlane = _ProjectionParams.y;
    }

    offsetLength -= iniOffsetToNearPlane;
    float3 roNearPlane = ro + rd * iniOffsetToNearPlane;
    float stepLength = (_Distance - iniOffsetToNearPlane) / (float)_MaxSteps;
    float jitter = stepLength * InterleavedGradientNoise(positionCS, _FrameCount);

    #if _MAIN_LIGHT_CONTRIBUTION_DISABLED
    float phaseMainLight = 0.0;
    #else
    float phaseMainLight = CornetteShanksPhaseFunction(_Anisotropies[_CustomAdditionalLightsCount], dot(rdPhase, GetMainLight().direction));
    #endif
    float minusStepLengthTimesAbsortion = -stepLength * _Absortion;

    float3 volumetricFogColor = float3(0.0, 0.0, 0.0);
    float transmittance = 1.0;

    // Optimized raymarch loop with early exit if transmittance drops below a threshold.
    UNITY_LOOP
    for (int i = 0; i < _MaxSteps; ++i)
    {
        float dist = jitter + i * stepLength;
        if (dist >= offsetLength)
            break;

        // Early termination: If transmittance is negligible, break out of the loop.
        if (transmittance < 0.01)
            break;

        float3 currPosWS = roNearPlane + rd * dist;
        float density = GetFogDensity(currPosWS.y);

        // Skip computation if density is effectively zero.
        if (density <= 0.0)
            continue;

        float stepAttenuation = exp(minusStepLengthTimesAbsortion * density);
        transmittance *= stepAttenuation;

        float3 mainLightColor = GetStepMainLightColor(currPosWS, phaseMainLight, density);
        float3 additionalLightsColor = GetStepAdditionalLightsColor(uv, currPosWS, rd, density);

        float3 stepColor = mainLightColor + additionalLightsColor;
        volumetricFogColor += (stepColor * (transmittance * stepLength));
    }

    return float4(volumetricFogColor, transmittance);
}

#endif