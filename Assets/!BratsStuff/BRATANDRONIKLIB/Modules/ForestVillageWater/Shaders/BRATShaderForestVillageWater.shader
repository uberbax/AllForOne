Shader "BRATANDRONIKLIB/Forest Village Water"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Renderer Tint", Color) = (1,1,1,1)
        _WaterTint ("Water Tint", Color) = (1,1,1,1)

        [Header(Shore Wobble)]
        _EdgeWobble ("Wobble Amount", Range(0, 0.12)) = 0
        _EdgeWobbleScale ("Wobble Scale", Range(0.1, 4)) = 0.72
        _EdgeWobbleSpeed ("Wobble Speed", Range(0, 4)) = 0.8

        [Header(Shore Ripple)]
        _ShoreRippleColor ("Ripple Color", Color) = (1,1,1,1)
        _ShoreRippleWidth ("Ripple Width", Range(1, 8)) = 6
        _ShoreRippleScale ("Ripple Segments", Range(0.5, 8)) = 2.8
        _ShoreRippleSpeed ("Ripple Speed", Range(0, 4)) = 1.4
        _SpriteSizePixels ("Sprite Size In Atlas", Range(16, 512)) = 256

        [Header(Random Wave Streaks)]
        _WaveColor ("Wave Color", Color) = (1,1,1,1)
        _WaveDensity ("Wave Density", Range(0, 1)) = 0.76
        _WaveCellWidth ("Horizontal Spacing", Range(3, 16)) = 5.0
        _WaveCellHeight ("Vertical Spacing", Range(2, 12)) = 3.2
        _WaveLengthMin ("Minimum Length", Range(0.25, 4)) = 0.4
        _WaveLengthMax ("Maximum Length", Range(0.5, 6)) = 1.4
        _WaveWidth ("Line Width", Range(0.005, 0.2)) = 0.025
        _WaveSoftness ("Line Softness", Range(0.002, 0.15)) = 0.018
        _WaveBend ("Line Bend", Range(0, 0.3)) = 0
        _WaveDrift ("Horizontal Drift", Range(0, 4)) = 0.65
        _PulseSpeed ("Appear / Disappear Speed", Range(0.1, 3)) = 2.2

        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _WaterTint;
            fixed4 _WaveColor;
            fixed4 _ShoreRippleColor;

            float _EdgeWobble;
            float _EdgeWobbleScale;
            float _EdgeWobbleSpeed;
            float _ShoreRippleWidth;
            float _ShoreRippleScale;
            float _ShoreRippleSpeed;
            float _SpriteSizePixels;

            float _WaveDensity;
            float _WaveCellWidth;
            float _WaveCellHeight;
            float _WaveLengthMin;
            float _WaveLengthMax;
            float _WaveWidth;
            float _WaveSoftness;
            float _WaveBend;
            float _WaveDrift;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            float Hash11(float value)
            {
                return frac(sin(value) * 43758.5453123);
            }

            float2 Hash22(float2 value)
            {
                float2 dotValue = float2(
                    dot(value, float2(127.1, 311.7)),
                    dot(value, float2(269.5, 183.3)));
                return frac(sin(dotValue) * 43758.5453123);
            }

            float2 GetSurfaceOffset(float2 worldPos, float time)
            {
                float vertical = sin(worldPos.x * _EdgeWobbleScale + time * _EdgeWobbleSpeed);
                vertical += sin(worldPos.x * (_EdgeWobbleScale * 1.91) - time * (_EdgeWobbleSpeed * 0.63)) * 0.42;

                float horizontal = sin(worldPos.y * (_EdgeWobbleScale * 0.83) - time * (_EdgeWobbleSpeed * 0.74));
                horizontal += sin(worldPos.y * (_EdgeWobbleScale * 1.47) + time * (_EdgeWobbleSpeed * 0.51)) * 0.35;

                return float2(horizontal * 0.38, vertical) * _EdgeWobble;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.xy += GetSurfaceOffset(world.xy, _Time.y);

                o.vertex = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xy;
                o.texcoord = v.texcoord;
                o.color = v.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                    o.vertex = UnityPixelSnap(o.vertex);
                #endif

                return o;
            }

            float WaveInCell(float2 worldPos, float2 cell, float time)
            {
                float2 cellRandom = Hash22(cell);
                float cellPhase = Hash11(dot(cell, float2(41.13, 73.71)) + 5.17);
                float cycleSpeed = lerp(0.075, 0.145, cellRandom.x) * _PulseSpeed;
                float cycleTime = time * cycleSpeed + cellPhase;
                float cycleIndex = floor(cycleTime);
                float cycle = frac(cycleTime);

                // A new seed is used after every fully faded cycle, so the next
                // streak appears at another position instead of repeating in place.
                float2 cycleSeed = cell + cycleIndex * float2(19.19, 47.77);
                float2 randomA = Hash22(cycleSeed + 3.17);
                float2 randomB = Hash22(cycleSeed + 17.37);
                float randomC = Hash11(dot(cycleSeed, float2(53.31, 91.17)) + 7.23);
                float present = step(1.0 - _WaveDensity, randomA.x);

                // One continuous ease: fully transparent at the cycle edges,
                // fully white in the middle, with no threshold or hold phase.
                float life = 0.5 - 0.5 * cos(cycle * 6.2831853);

                float2 center = (cell + 0.5) * float2(_WaveCellWidth, _WaveCellHeight);
                center.x += (randomA.y - 0.5) * _WaveCellWidth * 0.42;
                center.y += (randomB.y - 0.5) * _WaveCellHeight * 0.56;

                float direction = randomB.x < 0.5 ? -1.0 : 1.0;
                center.x += (cycle - 0.5) * _WaveDrift * direction;

                float2 local = worldPos - center;
                float length = lerp(_WaveLengthMin, _WaveLengthMax, randomB.x);
                float bendFrequency = lerp(0.72, 1.36, randomA.y);
                float curve = sin(local.x * bendFrequency + randomC * 6.28318 + time * direction * 0.16) * _WaveBend;
                float distanceToLine = abs(local.y - curve);

                float stripe = 1.0 - smoothstep(_WaveWidth, _WaveWidth + _WaveSoftness, distanceToLine);
                float ends = 1.0 - smoothstep(length * 0.38, length * 0.5, abs(local.x));

                return present * life * stripe * ends;
            }

            float MakeRandomWaves(float2 worldPos, float time)
            {
                float2 cellSize = float2(_WaveCellWidth, _WaveCellHeight);
                float2 baseCell = floor(worldPos / cellSize);
                float waves = 0.0;

                [unroll]
                for (int x = -1; x <= 1; x++)
                    waves = max(waves, WaveInCell(worldPos, baseCell + float2(x, 0), time));

                return saturate(waves);
            }

            float MakeShoreRipple(float2 uv, float centerAlpha, float2 worldPos, float time)
            {
                float2 texel = _MainTex_TexelSize.xy * _ShoreRippleWidth;
                float2 spriteUvSize = _MainTex_TexelSize.xy * _SpriteSizePixels;
                float2 spriteIndex = floor(uv / spriteUvSize);
                float2 spriteOrigin = spriteIndex * spriteUvSize;
                float2 spriteMin = spriteOrigin + _MainTex_TexelSize.xy * 0.5;
                float2 spriteMax = (spriteIndex + 1.0) * spriteUvSize - _MainTex_TexelSize.xy * 0.5;
                float2 spriteLocal = saturate((uv - spriteOrigin) / spriteUvSize);
                float borderDistance = min(
                    min(spriteLocal.x, 1.0 - spriteLocal.x),
                    min(spriteLocal.y, 1.0 - spriteLocal.y));
                float atlasSafe = step((_ShoreRippleWidth + 1.0) / _SpriteSizePixels, borderDistance);

                float2 sampleUv = clamp(uv + float2(texel.x, 0.0), spriteMin, spriteMax);
                float neighbourAlpha = tex2D(_MainTex, sampleUv).a;
                sampleUv = clamp(uv - float2(texel.x, 0.0), spriteMin, spriteMax);
                neighbourAlpha = min(neighbourAlpha, tex2D(_MainTex, sampleUv).a);
                sampleUv = clamp(uv + float2(0.0, texel.y), spriteMin, spriteMax);
                neighbourAlpha = min(neighbourAlpha, tex2D(_MainTex, sampleUv).a);
                sampleUv = clamp(uv - float2(0.0, texel.y), spriteMin, spriteMax);
                neighbourAlpha = min(neighbourAlpha, tex2D(_MainTex, sampleUv).a);

                // Only opaque water pixels next to transparent shoreline pixels.
                float edge = saturate((centerAlpha - neighbourAlpha) * 2.0);

                // Two non-parallel phases break the rim into small glints. The
                // squared sine remains continuous, so every glint fades smoothly.
                float phaseA = dot(worldPos, float2(1.0, 0.73)) * _ShoreRippleScale;
                phaseA += time * _ShoreRippleSpeed;
                float phaseB = dot(worldPos, float2(-0.61, 1.0)) * (_ShoreRippleScale * 0.57);
                phaseB -= time * (_ShoreRippleSpeed * 0.71);
                float pulse = 0.5 + 0.5 * sin(phaseA);
                pulse *= pulse;
                float breakup = 0.65 + 0.35 * (0.5 + 0.5 * sin(phaseB));

                return edge * atlasSafe * pulse * breakup;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 textureColor = tex2D(_MainTex, i.texcoord);
                fixed4 color = textureColor * i.color;
                color.rgb *= _WaterTint.rgb;
                color.a *= _WaterTint.a;
                clip(color.a - 0.001);

                float waves = MakeRandomWaves(i.worldPos, _Time.y);
                float waveBlend = saturate(waves * _WaveColor.a);
                color.rgb = lerp(color.rgb, _WaveColor.rgb, waveBlend);

                float shoreRipple = MakeShoreRipple(i.texcoord, textureColor.a, i.worldPos, _Time.y);
                float shoreBlend = saturate(shoreRipple * _ShoreRippleColor.a);
                color.rgb = lerp(color.rgb, _ShoreRippleColor.rgb, shoreBlend);

                return color;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
