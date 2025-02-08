#ifndef SHADER_UTILS_INCLUDED
#define SHADER_UTILS_INCLUDED

#define PI          3.14159265358979323846
#define TWO_PI      6.28318530717958647693
#define FOUR_PI     12.5663706143591729538
#define INV_PI      0.31830988618379067154
#define INV_TWO_PI  0.15915494309189533577
#define INV_FOUR_PI 0.07957747154594766788
#define HALF_PI     1.57079632679489661923
#define INV_HALF_PI 0.63661977236758134308
#define LOG2_E      1.44269504088896340736
#define INV_SQRT2   0.70710678118654752440
#define PI_DIV_FOUR 0.78539816339744830961

#define SCROLL_TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + (fmod(_Time.y, 1200) * name##_ST.zw))


float2 rotateUV(float2 uv, const float rotation)
{
    uv -= 0.5;
    float sr;
    float cr;
    sincos(rotation, sr, cr);
    return float2(
        cr * uv.x + sr * uv.y + 0.5,
        cr * uv.y - sr * uv.x + 0.5
    );
}

float remap01(const float value, const float min, const float max)
{
    return min + value * (max - min);
}

float remap(float value, float low1, float high1, float low2, float high2)
{
    return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
}

float remapTo01(float value, float min, float max)
{
    return saturate((value - min) / (max - min));
}

float hash(const float n)
{
    return frac(sin(n) * 43758.5453);
}

//Random Noise: float2 in float out
float N21(float2 p)
{
    p = frac(p * float2(123.45, 342.34));
    p += dot(p, p + 34.544);
    return frac(p.x * p.y);
}

float LinearSlope(const float value, const float slope)
{
    const float powered_value = pow(value, slope);
    return saturate(powered_value / (powered_value + pow(1 - value, slope)));
}

float RemapTo01(float value, float low1, float high1)
{
    return saturate((value - low1) * rcp(high1 - low1));
}

float RemapFrom01(float value, float low2, float high2)
{
    return low2 + value * (high2 - low2);
}


float posterize(float value, float steps)
{
    return floor(value * steps) * rcp(steps);
}

float SinNoise(const float x, const float seed)
{
    return sin(2 * x + seed) + sin(PI * x + seed);
}

//3 Channels Fade function
float RGBFade(half3 noise, half fade)
{
    float3 i_min = float3(0, 0.25, 0.5); //Fade curvature
    fade = fade * 2.0 - 1.0; //Remap Fade to -1,1
    float3 remapped = saturate(noise.rgb + fade - i_min);
    half sRGBFade = 0.3333333 * (remapped.r + remapped.g + remapped.b);
    return sRGBFade * (sRGBFade * (sRGBFade * 0.305306011 + 0.682171111) + 0.012522878); //sRGB to Gamma Conversion
}

//Flow UV (Returns UV)
void flow_uvw_float(float2 uv, float3 flowVector, float2 jump, float time, float flowOffset, float flowStrength, float speed, out float4 coords, out float blend)
{
    time = time * speed + flowVector.b;
    flowVector.xy = (flowVector.xy * 2 - 1) * flowStrength;
    const float progress0 = frac(time);
    const float progress1 = frac(time + 0.5f);
    coords.xy = uv - flowVector.xy * (progress0 + flowOffset) + (time - progress0) * jump;
    coords.zw = uv - flowVector.xy * (progress1 + flowOffset) + 0.5f + (time - progress1) * jump;
    blend = abs(1 - 2 * progress0);
    blend = smoothstep(0, 1, blend);
}

//Sample Flow texture
float4 SampleFlowTex(const sampler2D flow_texture, const float2 uv, float3 uvw_offset_a, float3 uvw_offset_b)
{
    const half4 flow_tex_a = tex2D(flow_texture, uv + uvw_offset_a.xy) * uvw_offset_a.z;
    const half4 flow_tex_b = tex2D(flow_texture, uv + uvw_offset_b.xy) * uvw_offset_b.z;
    return saturate(flow_tex_a + flow_tex_b);
}

//Sample Two FX layers
float4 SampleFXTex(const sampler2D fx_texture, float4 uv, const float blend)
{
    const half fx_tex_a = tex2D(fx_texture, uv.xy).r;
    const half fx_tex_b = tex2D(fx_texture, uv.zw).r;
    return lerp(fx_tex_a * fx_tex_b, max(fx_tex_a, fx_tex_b), blend);
}

//Flow UV Offset with phase Shift (More random)
float3 flow_uvw(const float2 uv, const float2 flow_vector, const float2 jump, const float time, const bool flow_b)
{
    const float phaseOffset = flow_b ? 0.5 : 0;
    const float progress = frac(time + phaseOffset);
    float3 uvw;
    uvw.xy = uv - flow_vector * progress;
    uvw.xy += phaseOffset;
    uvw.xy += (time - progress) * jump;
    uvw.z = 1 - abs(1 - 2 * progress);
    return uvw;
}

//Flow UV Offset with phase Shift (More random)
float3 flow_uvw_float(const float2 uv, const float2 flow_vector, const float2 jump, const float time, const bool flow_b)
{
    const float phaseOffset = flow_b ? 0.5 : 0;
    const float progress = frac(time + phaseOffset);
    float3 uvw;
    uvw.xy = uv - flow_vector * progress;
    uvw.xy += phaseOffset;
    uvw.xy += (time - progress) * jump;
    uvw.z = 1 - abs(1 - 2 * progress);
    return uvw;
}

//Three Tone Gradient Map with Bias
float3 TriToneBiased(half source, const float3 color0, const float3 color1, const float3 color2, const float bias)
{
    return half3(lerp(lerp(color1, color2, (source - bias) / (1 - bias)), lerp(color0, color1, source / bias),
                      step(source, bias)));
}

//Three Tone Gradient Map Simple
half3 TriTone(half source, half3 color0, half3 color1, half3 color2)
{
    source *= 2.0;
    return half3(lerp(color2, lerp(color1, color0, saturate(source - 1.0)), min(source, 1.0)));
}

//Gerstner Wave (direction = wave.xy steepness = wave.z, wavelength = wave.w)
float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal, const float time)
{
    const half k = TWO_PI / wave.w;
    const half c = sqrt(9.8 / k);
    const float2 dir = normalize(wave.xy);
    const half f = k * (dot(dir, p.xz) - c * time);
    const half a = wave.z / k;
    const half sin_f = sin(f);
    const half cos_f = cos(f);
    const half s_sin_f = sin_f * wave.z;
    const half s_cos_f = cos_f * wave.z;

    tangent += float3(-dir.x * dir.x * s_sin_f, dir.x * s_cos_f, -dir.x * dir.y * s_sin_f);
    binormal += float3(-dir.x * dir.y * s_sin_f, dir.y * s_cos_f, -dir.y * dir.y * s_sin_f);
    return float3(dir.x * a * cos_f, a * sin_f, dir.y * cos_f);
}

half Fresnel(const float3 normalWS, const float3 viewDir, const float power)
{
    return pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDir))), power);
}

//Overlay Blend Mode
float4 Unity_Blend_Overlay(float4 Base, float4 Blend, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    float4 Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
    return Out;
}

float3 Overlay(const float3 dst, const float3 src)
{
    return dst > 0.5 ? 1.0 - (1.0 - 2.0 * (dst - 0.5)) * (1.0 - src) : 2.0 * dst * src;
}

//Adds Ease In and Out Slopes to the Gradient, Slope 1 means Linear, >1 tends Graph to Step
float Slope(const float value, const float slope)
{
    const float powered_value = pow(value, slope);
    return saturate(powered_value / (powered_value + pow(1 - value, slope)));
}

half BandedGradient(float source, int steps, half slope)
{
    float final_gradient = 0;
    const float step_length = rcp(steps);
    float current_pos = 0;
    float next_pos = step_length + step_length;
    for (int s = 0; s < steps - 1; ++s)
    {
        float remappedGradient = RemapTo01(source, current_pos, next_pos);
        remappedGradient = Slope(remappedGradient, slope);
        remappedGradient = RemapFrom01(remappedGradient, current_pos, next_pos);
        final_gradient = lerp(final_gradient, remappedGradient, step(current_pos, source));
        current_pos += step_length;
        next_pos += step_length;
    }
    return final_gradient;
}
#endif