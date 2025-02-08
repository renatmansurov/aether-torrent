#ifndef GLOBAL_CLOUDS_INCLUDED
#define GLOBAL_CLOUDS_INCLUDED

#include "Assets/3DPixelArtEnvironment/Shaders/Includes/SimplexNoise3D.hlsl"
#include "Assets/Shaders/Includes/shader_utils.hlsl"

float4 _GlobalCloudsSS;
float4 _GlobalCloudsSpdPostRemap;
float4 _GlobalLightColor;
float _GlobalCloudsFade;

void cloudMask_float(float2 uv, float time, out float sample)
{
    time *= 0.01;
    float cloud_shadow = abs(snoise(float3(uv * _GlobalCloudsSS.xy + time.xx * _GlobalCloudsSS.zw, time.x * _GlobalCloudsSpdPostRemap.x)));
    _GlobalCloudsSS *= float4(0.67512, 0.53723, 0.7426, 0.5323);
    const float cloud_shadow1 = abs(snoise(float3(uv * _GlobalCloudsSS.xy + time.xx * _GlobalCloudsSS.zw, time.x * _GlobalCloudsSpdPostRemap.x)));
    cloud_shadow = max(cloud_shadow, cloud_shadow1);
    const float remapped_step = lerp(-1 * _GlobalCloudsSpdPostRemap.w, 1.0, _GlobalCloudsSpdPostRemap.z);
    sample = 1 - saturate(smoothstep(remapped_step + _GlobalCloudsSpdPostRemap.w, remapped_step, cloud_shadow));
}

void cloudMaskTexture_float(float2 uv, float time, out float sample)
{
    time *= 0.01;
    float cloud_shadow = abs(snoise(float3(uv * _GlobalCloudsSS.xy + time.xx * _GlobalCloudsSS.zw, time.x * _GlobalCloudsSpdPostRemap.x)));
    _GlobalCloudsSS *= float4(0.67512, 0.53723, 0.7426, 0.5323);
    const float cloud_shadow1 = abs(snoise(float3(uv * _GlobalCloudsSS.xy + time.xx * _GlobalCloudsSS.zw, time.x * _GlobalCloudsSpdPostRemap.x)));
    cloud_shadow = max(cloud_shadow, cloud_shadow1);
    const float remapped_step = lerp(-1 * _GlobalCloudsSpdPostRemap.w, 1.0, _GlobalCloudsSpdPostRemap.z);
    sample = 1 - saturate(smoothstep(remapped_step + _GlobalCloudsSpdPostRemap.w, remapped_step, cloud_shadow));
}
#endif