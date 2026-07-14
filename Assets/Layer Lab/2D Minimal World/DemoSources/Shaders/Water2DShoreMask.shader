Shader "LayerLab/Map/Water2D Shore Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1, 1, 1, 1)
        [HideInInspector][NoScaleOffset] _ShoreDataTex ("Shore Data Texture", 2D) = "black" {}
        [HideInInspector] _UseShoreData ("Use Shore Data", Range(0, 1)) = 0
        [HideInInspector] _ShoreDataWorldRect ("Shore Data World Rect", Vector) = (0, 0, 1, 1)

        [Header(Depth Color)]
        [Toggle] _DepthColorEnabled ("Depth Color Enabled", Float) = 1
        _TopColor ("Top Water Color", Color) = (0.38, 0.86, 0.84, 1)
        _DeepColor ("Deep Water Color", Color) = (0.02, 0.26, 0.42, 1)
        _DeepDepthStart ("Deep Color Start", Range(-1, 1)) = 0.72
        _DeepDepthEnd ("Deep Color End", Range(0, 1)) = 1.00
        _DepthColorOffsetY ("Depth Color Y Offset", Range(-0.5, 0.5)) = 0

        [Header(Shore Shadow)]
        [Toggle] _ShoreReflectionEnabled ("Shore Shadow Enabled", Float) = 1
        _ShoreReflectionColor ("Shore Shadow Color", Color) = (0.33, 0.42, 0.30, 1)
        _ShoreReflectionStrength ("Shore Shadow Strength", Range(0, 1)) = 0.18
        _ShoreReflectionWidth ("Shore Shadow Width", Range(0.01, 1)) = 0.18
        _ShoreReflectionSoftness ("Shore Shadow Softness", Range(0.001, 0.5)) = 0.08
        _ShoreReflectionOffsetY ("Shore Shadow Y Offset", Range(-0.5, 0.5)) = 0
        _ShoreReflectionWarpStrength ("Shore Shadow Warp", Range(0, 0.4)) = 0.04
        _ShoreReflectionMotionStrength ("Shore Shadow Motion Strength", Range(0, 0.2)) = 0.03
        _ShoreReflectionMotionScale ("Shore Shadow Motion Scale", Range(0.1, 8)) = 1.2
        _ShoreReflectionMotionSpeed ("Shore Shadow Motion Speed", Range(0, 2)) = 0.35

        [Header(Highlights)]
        [Toggle] _LineHighlightEnabled ("Line Highlight Enabled", Float) = 1
        _LineColor ("Line Highlight Color", Color) = (0.72, 0.98, 1.00, 1)
        _LineScale ("Line Scale", Range(0.05, 3)) = 1.08
        _LineDensity ("Line Density", Range(0, 1)) = 0.18
        _LineWidth ("Line Width", Range(0.002, 0.08)) = 0.012
        _LineLength ("Line Length", Range(0.05, 0.8)) = 0.26
        _LineStrength ("Line Strength", Range(0, 1)) = 0.42
        _LineSpeed ("Line Cycle Speed", Range(0, 2)) = 0.42

        [Header(Shore Foam)]
        [Toggle] _ShoreFoamEnabled ("Shore Foam Enabled", Float) = 1
        _ShoreFoamColor ("Shore Foam Color", Color) = (0.96, 1.00, 0.98, 1)
        _ShoreFoamStrength ("Shore Foam Strength", Range(0, 1)) = 0.38
        _ShoreFoamWidth ("Shore Foam Width", Range(0.01, 1)) = 0.16
        _ShoreFoamSoftness ("Shore Foam Softness", Range(0.001, 0.25)) = 0.035
        _ShoreFoamNoiseStrength ("Shore Foam Noise", Range(0, 1)) = 0.015
        _ShoreFoamNoiseScale ("Shore Foam Noise Scale", Range(0.2, 12)) = 4
        _ShoreFoamWarpStrength ("Shore Foam Warp", Range(0, 0.4)) = 0.08
        _ShoreFoamWarpScale ("Shore Foam Warp Scale", Range(0.1, 8)) = 1.35
        _ShoreFoamMotionStrength ("Shore Foam Motion Strength", Range(0, 0.2)) = 0.03
        _ShoreFoamMotionScale ("Shore Foam Motion Scale", Range(0.1, 8)) = 1.2
        _ShoreFoamMotionSpeed ("Shore Foam Motion Speed", Range(0, 2)) = 0.35
        _ShoreFoamOffsetY ("Shore Foam Y Offset", Range(-0.5, 0.5)) = 0

        [Header(Alpha)]
        _WaterAlpha ("Water Alpha", Range(0, 1)) = 1

        [Header(Lighting)]
        [Toggle] _DayNightLightingEnabled ("DayNight Lighting Enabled", Float) = 1
        _LightInfluence ("DayNight Light Influence", Range(0, 1)) = 1
        _MinBrightness ("Minimum Brightness", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShoreDataTex);
            SAMPLER(sampler_ShoreDataTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShoreDataWorldRect;
                float _UseShoreData;
                half4 _Color;
                float _DepthColorEnabled;
                half4 _TopColor;
                half4 _DeepColor;
                float _DeepDepthStart;
                float _DeepDepthEnd;
                float _DepthColorOffsetY;
                float _ShoreReflectionEnabled;
                half4 _ShoreReflectionColor;
                float _ShoreReflectionStrength;
                float _ShoreReflectionWidth;
                float _ShoreReflectionSoftness;
                float _ShoreReflectionOffsetY;
                float _ShoreReflectionWarpStrength;
                float _ShoreReflectionMotionStrength;
                float _ShoreReflectionMotionScale;
                float _ShoreReflectionMotionSpeed;
                float _LineHighlightEnabled;
                half4 _LineColor;
                float _LineScale;
                float _LineDensity;
                float _LineWidth;
                float _LineLength;
                float _LineStrength;
                float _LineSpeed;
                float _ShoreFoamEnabled;
                half4 _ShoreFoamColor;
                float _ShoreFoamStrength;
                float _ShoreFoamWidth;
                float _ShoreFoamSoftness;
                float _ShoreFoamNoiseStrength;
                float _ShoreFoamNoiseScale;
                float _ShoreFoamWarpStrength;
                float _ShoreFoamWarpScale;
                float _ShoreFoamMotionStrength;
                float _ShoreFoamMotionScale;
                float _ShoreFoamMotionSpeed;
                float _ShoreFoamOffsetY;
                float _WaterAlpha;
                float _DayNightLightingEnabled;
                float _LightInfluence;
                float _MinBrightness;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 screenPosition : TEXCOORD1;
                float2 worldXY : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            float SparseLine(float2 worldXY, float timeValue)
            {
                float2 p = worldXY * _LineScale;
                float2 cell = floor(p);
                float2 local = frac(p);
                float seed = Hash21(cell);
                float phase = Hash21(cell + 9.17);
                float cycleTime = timeValue * max(_LineSpeed, 0.001) + phase;
                float cycleIndex = floor(cycleTime);
                float cycle = frac(cycleTime);
                float visible = step(1.0 - _LineDensity, seed);
                float life = smoothstep(0.05, 0.22, cycle) * (1.0 - smoothstep(0.72, 0.96, cycle));
                float lengthPulse = sin(cycle * 3.14159265);
                float2 cycleSeed = cell + cycleIndex * float2(13.7, 29.3);
                float offsetY = lerp(0.22, 0.78, Hash21(cycleSeed + 17.3));
                float centerX = lerp(0.20, 0.80, Hash21(cycleSeed + 41.7));
                float width = max(_LineWidth, 0.001);
                float lineLength = max(_LineLength * lengthPulse, 0.01);
                float halfLength = lineLength * 0.5;
                float2 capsuleDelta = float2(max(abs(local.x - centerX) - halfLength, 0.0), local.y - offsetY);
                float capsuleDistance = length(capsuleDelta);
                return saturate(1.0 - smoothstep(width, width * 2.2, capsuleDistance)) * visible * life;
            }

            float2 ShoreDataUv(float2 worldXY)
            {
                return saturate((worldXY - _ShoreDataWorldRect.xy) / max(_ShoreDataWorldRect.zw, 0.0001));
            }

            float4 SampleShoreDataUv(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_ShoreDataTex, sampler_ShoreDataTex, uv);
            }

            float4 SampleShoreData(float2 worldXY)
            {
                return SampleShoreDataUv(ShoreDataUv(worldXY));
            }

            float SampleShoreFoamProximity(float2 worldXY)
            {
                float2 foamOffset = float2(0.0, _ShoreFoamOffsetY);
                float radius = max(abs(_ShoreFoamOffsetY) * 0.75, 0.035);
                float warpScale = max(_ShoreFoamWarpScale, 0.001);
                float warpX = ValueNoise(worldXY * warpScale + float2(37.19, -11.73));
                float warpY = ValueNoise(worldXY * warpScale + float2(-23.41, 29.67));
                float2 foamWarp = (float2(warpX, warpY) - 0.5) * _ShoreFoamWarpStrength;
                float motionScale = max(_ShoreFoamMotionScale, 0.001);
                float motionTime = _Time.y * _ShoreFoamMotionSpeed;
                float motionX = ValueNoise(worldXY * motionScale + float2(motionTime, -motionTime * 0.61));
                float motionY = ValueNoise(worldXY * motionScale + float2(13.7 - motionTime * 0.43, motionTime));
                float2 foamMotion = (float2(motionX, motionY) - 0.5) * _ShoreFoamMotionStrength;
                float2 center = worldXY + foamOffset + foamWarp + foamMotion;
                float centerProximity = SampleShoreData(center).g;
                float leftProximity = SampleShoreData(center - float2(radius, 0.0)).g;
                float rightProximity = SampleShoreData(center + float2(radius, 0.0)).g;
                float maxProximity = max(max(leftProximity, rightProximity), centerProximity);
                return lerp(centerProximity, maxProximity, 0.28);
            }

            float2 ShoreReflectionMotionOffset(float2 worldXY)
            {
                float2 offset = 0.0;
                float motionScale = max(_ShoreReflectionMotionScale, 0.001);

                [branch]
                if (_ShoreReflectionWarpStrength > 0.0001)
                {
                    float warpX = ValueNoise(worldXY * motionScale + float2(-19.31, 43.77));
                    float warpY = ValueNoise(worldXY * motionScale + float2(52.23, -34.19));
                    offset += (float2(warpX, warpY) - 0.5) * _ShoreReflectionWarpStrength;
                }

                [branch]
                if (_ShoreReflectionMotionStrength > 0.0001 && _ShoreReflectionMotionSpeed > 0.0001)
                {
                    float motionTime = _Time.y * _ShoreReflectionMotionSpeed;
                    float motionX = ValueNoise(worldXY * motionScale + float2(31.73 + motionTime, -7.10 + motionTime * 0.37));
                    float motionY = ValueNoise(worldXY * motionScale + float2(-42.61 - motionTime * 0.49, 18.44 + motionTime));
                    offset += (float2(motionX, motionY) - 0.5) * _ShoreReflectionMotionStrength;
                }

                return offset;
            }

            float ShoreFoamMask(float2 worldXY, float shoreProximity, float waterMask, float useShoreData)
            {
                float noise = ValueNoise(worldXY * max(_ShoreFoamNoiseScale, 0.01));
                float width = saturate(_ShoreFoamWidth);
                float noiseOffset = (noise - 0.5) * _ShoreFoamNoiseStrength * width;
                float proximity = saturate(shoreProximity + noiseOffset);
                float foam = smoothstep(1.0 - width - _ShoreFoamSoftness, 1.0 - width, proximity);
                return saturate(foam * waterMask * useShoreData);
            }

            float ShoreDistanceBand(float shoreDepth, float width, float softness, float waterMask, float useShoreData)
            {
                float band = 1.0 - smoothstep(saturate(width), saturate(width + softness), saturate(shoreDepth));
                return saturate(band * waterMask * useShoreData);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldXY = worldPos.xy;
                o.screenPosition = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                clip(sprite.a - 0.001);

                float t = _Time.y;
                float2 w = i.worldXY;
                float2 shoreUv = ShoreDataUv(w);
                float4 shoreData = SampleShoreDataUv(shoreUv);
                float useShoreData = saturate(_UseShoreData) * shoreData.a;
                float shoreMaskAvailable = step(0.001, saturate(_UseShoreData));
                float hasShoreData = step(0.001, useShoreData);
                float depthEnabled = step(0.5, _DepthColorEnabled);
                float2 depthSampleCenter = w + float2(0.0, _DepthColorOffsetY);

                float normalizedShoreDepth = shoreData.r;
                [branch]
                if (hasShoreData > 0.0 && depthEnabled > 0.0)
                {
                    normalizedShoreDepth = SampleShoreData(depthSampleCenter).r;
                }

                float bakedDepthBase = smoothstep(0.0, 1.0, pow(normalizedShoreDepth, 1.38));
                float bakedDepth = saturate(lerp(0.08, 0.76, bakedDepthBase));
                float depth = lerp(0.45, bakedDepth, useShoreData * depthEnabled);

                float deepStart = min(_DeepDepthStart, _DeepDepthEnd);
                float deepEnd = max(_DeepDepthStart, _DeepDepthEnd);
                half3 col = lerp(_TopColor.rgb, _DeepColor.rgb, smoothstep(deepStart, deepEnd, depth) * depthEnabled);
                float shallowGlow = (1.0 - smoothstep(0.18, 0.68, normalizedShoreDepth)) * useShoreData * depthEnabled;
                col = lerp(col, _TopColor.rgb, shallowGlow * 0.24);

                [branch]
                if (hasShoreData > 0.0 && _ShoreReflectionEnabled > 0.5 && _ShoreReflectionStrength > 0.001)
                {
                    float2 reflectionSample = w + float2(0.0, _ShoreReflectionOffsetY) + ShoreReflectionMotionOffset(w);
                    float reflectionDepth = SampleShoreData(reflectionSample).r;
                    float reflectionMask = ShoreDistanceBand(reflectionDepth, _ShoreReflectionWidth, _ShoreReflectionSoftness, shoreData.a, useShoreData);
                    col = lerp(col, _ShoreReflectionColor.rgb, reflectionMask * _ShoreReflectionColor.a * _ShoreReflectionStrength);
                }

                [branch]
                if (_LineHighlightEnabled > 0.5 && _LineStrength > 0.001 && _LineDensity > 0.001)
                {
                    float lineAmount = SparseLine(w, t);
                    col = lerp(col, _LineColor.rgb, lineAmount * _LineStrength);
                }

                float spriteAlpha = smoothstep(0.001, 0.08, sprite.a);
                float shoreAlpha = smoothstep(0.16, 0.62, shoreData.a);
                float alpha = lerp(spriteAlpha, min(spriteAlpha, shoreAlpha), shoreMaskAvailable) * _WaterAlpha;

                [branch]
                if (hasShoreData > 0.0 && _ShoreFoamEnabled > 0.5 && _ShoreFoamStrength > 0.001)
                {
                    float foamProximity = SampleShoreFoamProximity(w);
                    float foamMask = ShoreFoamMask(w, foamProximity, shoreData.a, useShoreData);
                    col = lerp(col, _ShoreFoamColor.rgb, foamMask * _ShoreFoamColor.a * _ShoreFoamStrength);
                }

                half3 finalRgb = col;
                [branch]
                if (_DayNightLightingEnabled > 0.5 && _LightInfluence > 0.001)
                {
                    SurfaceData2D surfaceData;
                    InputData2D inputData;
                    InitializeSurfaceData(col, alpha, half4(1, 1, 1, 1), surfaceData);
                    InitializeInputData(i.uv, half2(i.screenPosition.xy / i.screenPosition.w), inputData);
                    half4 lit = CombinedShapeLightShared(surfaceData, inputData);
                    finalRgb = lerp(col, lit.rgb, saturate(_LightInfluence));
                    finalRgb = max(finalRgb, col * saturate(_MinBrightness));
                }

                return half4(finalRgb, alpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "LayerLab.Water2DShaderGUI"
    Fallback "Sprites/Default"
}
