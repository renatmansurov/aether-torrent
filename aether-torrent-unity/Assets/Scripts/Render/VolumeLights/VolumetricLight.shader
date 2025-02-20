Shader "Custom/VolumetricLight"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _CameraDepthTexture ("Camera Depth Texture", 2D) = "white" {}
        _FogDensity ("Fog Density", Float) = 0.05
        _StepCount ("Raymarching Steps", Float) = 16
        _NoiseScale ("Noise Scale", Float) = 1.0
        _GlobalIntensity ("Global Light Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            // Отключаем тест глубины и отрисовку в буфере глубины
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Структуры для передачи данных из вершинного шейдера во фрагментный
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Объявляем текстуры и сэмплеры через макросы, чтобы избежать ошибки отсутствия sampler_MainTex
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            // Объявляем параметры для нескольких источников света
            #define MAX_LIGHTS 4
            float4 _LightPositions[MAX_LIGHTS];    // Позиции точечных/сферических источников
            float _LightIntensities[MAX_LIGHTS];     // Интенсивности для каждого источника
            int _ActiveLightCount;                   // Фактическое число активных источников

            // Параметры эффекта
            float _FogDensity;
            int _StepCount;
            float _NoiseScale;
            float _GlobalIntensity;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.position = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Простая функция генерации шума
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Функция для получения линейной глубины (используется функция из Core.hlsl)
            float LinearEyeDepth(float depth)
            {
                return Linear01Depth(depth, _ZBufferParams);
            }

            float4 frag (Varyings i) : SV_Target
            {
                // Считываем исходный цвет сцены
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Получаем глубину для текущего пикселя
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv).r;
                float viewZ = LinearEyeDepth(depth);

                // Вычисляем размер шага для raymarching
                float stepSize = viewZ / _StepCount;
                float lightAccum = 0.0;

                // Цикл по всем активным источникам света
                for (int l = 0; l < _ActiveLightCount; l++)
                {
                    float singleLightAccum = 0.0;

                    // Выполняем raymarching по заданному числу шагов
                    for (int s = 0; s < _StepCount; s++)
                    {
                        float currentDistance = s * stepSize;
                        // Затухание света с расстоянием (экспоненциальное)
                        float attenuation = exp(-_FogDensity * currentDistance);
                        // Применяем шум для имитации неоднородности тумана
                        float n = noise(i.uv * _NoiseScale + currentDistance);
                        singleLightAccum += attenuation * n;
                    }

                    // Усредняем результат и учитываем интенсивность данного источника
                    singleLightAccum = (singleLightAccum / _StepCount) * _LightIntensities[l];
                    lightAccum += singleLightAccum;
                }

                // Применяем глобальный коэффициент интенсивности
                lightAccum *= _GlobalIntensity;
                // Добавляем вычисленный вклад объемного света к исходному цвету
                col.rgb += lightAccum;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/BlitCopy"
}