#ifndef DEPTH_AWARE_GAUSSIAN_BLUR_INCLUDED
#define DEPTH_AWARE_GAUSSIAN_BLUR_INCLUDED

#include "./DeclareDownsampledDepthTexture.hlsl"
#include "./ProjectionUtils.hlsl"

#define KERNEL_RADIUS 4
#define BLUR_DEPTH_FALLOFF 0.5

static const float KernelWeights[] = {0.2026, 0.1790, 0.1240, 0.0672, 0.0285};

// Blurs the RGB channels of the given texture using depth aware gaussian blur, which uses the downsampled camera depth to apply weights to the blur.
// The alpha channel is not blurred so the original value is returned.
float4 DepthAwareGaussianBlur(float2 uv, float2 dir, TEXTURE2D_X(textureToBlur), SAMPLER(sampler_TextureToBlur), float2 textureToBlurTexelSizeXy)
{
    float4 centerSample = SAMPLE_TEXTURE2D_X(textureToBlur, sampler_TextureToBlur, uv);
    float centerDepth = SampleDownsampledSceneDepth(uv);
    float centerLinearEyeDepth = LinearEyeDepthConsiderProjection(centerDepth);

    float3 rgbResult = centerSample.rgb * KernelWeights[0];
    float weights = KernelWeights[0];
    float2 texelSizeTimesDir = textureToBlurTexelSizeXy * dir;

    for (int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; ++i)
    {
        if (i == 0) continue;

        float2 uvOffset = (float)i * texelSizeTimesDir;
        float2 uvSample = uv + uvOffset;

        float depth = SampleDownsampledSceneDepth(uvSample);
        float linearEyeDepth = LinearEyeDepthConsiderProjection(depth);
        float depthDiff = abs(centerLinearEyeDepth - linearEyeDepth);
        float r2 = BLUR_DEPTH_FALLOFF * depthDiff;
        float g = exp(-r2 * r2);
        float weight = g * KernelWeights[abs(i)];

        float3 rgb = SAMPLE_TEXTURE2D_X(textureToBlur, sampler_TextureToBlur, uvSample).rgb;
        rgbResult += (rgb * weight);
        weights += weight;
    }

    return float4(rgbResult * rcp(weights), centerSample.a);
}

#endif