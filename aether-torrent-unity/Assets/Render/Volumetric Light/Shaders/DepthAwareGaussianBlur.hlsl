#ifndef DEPTH_AWARE_GAUSSIAN_BLUR_INCLUDED
#define DEPTH_AWARE_GAUSSIAN_BLUR_INCLUDED

#include "./DeclareDownsampledDepthTexture.hlsl"
#include "./ProjectionUtils.hlsl"

#define KERNEL_RADIUS 4
#define BLUR_DEPTH_FALLOFF 0.5

static const float KernelWeights[] = {0.2026, 0.1790, 0.1240, 0.0672, 0.0285};

// Blurs the RGB channels of the given texture using depth-aware Gaussian blur.
// The alpha channel remains unblurred.
float4 DepthAwareGaussianBlur(float2 uv, float2 dir, TEXTURE2D_X(textureToBlur), SAMPLER(sampler_TextureToBlur), float2 textureToBlurTexelSizeXy)
{
    // Sample center pixel and compute its depth.
    float4 centerSample = SAMPLE_TEXTURE2D_X(textureToBlur, sampler_TextureToBlur, uv);
    float centerDepth = SampleDownsampledSceneDepth(uv);
    float centerLinearEyeDepth = LinearEyeDepthConsiderProjection(centerDepth);

    // Initialize accumulation with the center sample.
    float3 rgbResult = centerSample.rgb * KernelWeights[0];
    float totalWeight = KernelWeights[0];

    // Precompute the texel direction.
    float2 texelDir = textureToBlurTexelSizeXy * dir;

    UNITY_UNROLL
    for (int i = 1; i <= KERNEL_RADIUS; ++i)
    {
        float2 offset = (float)i * texelDir;

        // Positive offset sample.
        float2 uvPos = uv + offset;
        float depthPos = SampleDownsampledSceneDepth(uvPos);
        float linearDepthPos = LinearEyeDepthConsiderProjection(depthPos);
        float depthDiffPos = abs(centerLinearEyeDepth - linearDepthPos);
        float r2Pos = BLUR_DEPTH_FALLOFF * depthDiffPos;
        float weightPos = exp(-r2Pos * r2Pos) * KernelWeights[i];
        float3 rgbPos = SAMPLE_TEXTURE2D_X(textureToBlur, sampler_TextureToBlur, uvPos).rgb;

        // Negative offset sample.
        float2 uvNeg = uv - offset;
        float depthNeg = SampleDownsampledSceneDepth(uvNeg);
        float linearDepthNeg = LinearEyeDepthConsiderProjection(depthNeg);
        float depthDiffNeg = abs(centerLinearEyeDepth - linearDepthNeg);
        float r2Neg = BLUR_DEPTH_FALLOFF * depthDiffNeg;
        float weightNeg = exp(-r2Neg * r2Neg) * KernelWeights[i];
        float3 rgbNeg = SAMPLE_TEXTURE2D_X(textureToBlur, sampler_TextureToBlur, uvNeg).rgb;

        // Accumulate both samples.
        rgbResult += (rgbPos * weightPos) + (rgbNeg * weightNeg);
        totalWeight += weightPos + weightNeg;
    }

    return float4(rgbResult * rcp(totalWeight), centerSample.a);
}

#endif