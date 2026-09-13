Shader "Custom/TilemapStylizedWater"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Water Tint", Color) = (0.25, 0.65, 1, 1)

        [Header(Surface Waves)]
        _WaveColor ("Wave Color", Color) = (1,1,1,1)
        _WaveDensity ("Wave Density", Range(0.2, 8)) = 2
        _WaveScale ("Wave Scale", Range(0.2, 5)) = 1
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.5
        _WaveBrightness ("Wave Brightness", Range(0, 3)) = 1.5
        _WaveFadeSpeed ("Wave Fade Speed", Range(0.2, 5)) = 1.2

        [Header(Shore Foam)]
        _FoamColor ("Foam Color", Color) = (1,1,1,1)
        _FoamWidth ("Foam Width", Range(0.001, 0.5)) = 0.08
        _FoamSoftness ("Foam Softness", Range(0.001, 0.5)) = 0.04
        _FoamSpeed ("Foam Speed", Range(0, 5)) = 1
        _FoamAmount ("Foam Amount", Range(0, 2)) = 1

        [Header(Mask)]
        _ShoreMask ("Shore Mask", 2D) = "white" {}
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

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _ShoreMask;

            float4 _Color;

            float4 _WaveColor;
            float _WaveDensity;
            float _WaveScale;
            float _WaveSpeed;
            float _WaveBrightness;
            float _WaveFadeSpeed;

            float4 _FoamColor;
            float _FoamWidth;
            float _FoamSoftness;
            float _FoamSpeed;
            float _FoamAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wp.xy;

                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float sdSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;

                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            float Chevron(float2 p)
            {
                // centered V shape
                float2 a = float2(-0.35, 0.15);
                float2 b = float2(0.0, -0.12);
                float2 c = float2(0.35, 0.15);

                float d1 = sdSegment(p, a, b);
                float d2 = sdSegment(p, b, c);

                float d = min(d1, d2);

                return 1.0 - smoothstep(0.035, 0.075, d);
            }

            float SurfaceWave(float2 worldPos)
            {
                float2 p = worldPos * _WaveDensity;

                float2 cell = floor(p);
                float2 local = frac(p) - 0.5;

                float rnd = hash21(cell);

                // make waves rare
                float exists = step(0.78, rnd);

                float timeOffset = rnd * 6.2831;
                float t = frac(_Time.y * _WaveFadeSpeed + rnd);

                // fade in / fade out
                float fade = sin(t * UNITY_PI);
                fade = pow(saturate(fade), 2.5);

                // slight drift
                local.x += sin(_Time.y * _WaveSpeed + timeOffset) * 0.08;
                local.y += _Time.y * _WaveSpeed * 0.03;

                local *= 1.0 / max(_WaveScale, 0.001);

                float wave = Chevron(local);

                return wave * exists * fade;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                fixed4 col = tex * _Color * i.color;

                // -----------------------------
                // Surface white chevrons
                // -----------------------------

                float wave = SurfaceWave(i.worldPos);

                float3 waveColor =
                    _WaveColor.rgb *
                    wave *
                    _WaveBrightness;

                col.rgb += waveColor;


                // -----------------------------
                // Shore foam
                // -----------------------------

                // Shore mask:
                // black = deep water
                // white = shoreline
                float shore = tex2D(_ShoreMask, i.uv).r;

                // animate foam advancing / retreating
                float foamMove =
                    sin(
                        _Time.y * _FoamSpeed +
                        i.worldPos.x * 2.0 +
                        i.worldPos.y * 1.3
                    );

                foamMove = foamMove * 0.5 + 0.5;

                float threshold =
                    1.0 -
                    _FoamWidth * foamMove;

                float foam =
                    smoothstep(
                        threshold - _FoamSoftness,
                        threshold,
                        shore
                    );

                // break foam edge slightly
                float noise =
                    sin(i.worldPos.x * 8 + _Time.y * 0.5) *
                    sin(i.worldPos.y * 7 - _Time.y * 0.35);

                foam *= saturate(0.85 + noise * 0.15);

                foam *= _FoamAmount;

                col.rgb =
                    lerp(
                        col.rgb,
                        _FoamColor.rgb,
                        saturate(foam)
                    );

                col.a = tex.a * i.color.a;

                return col;
            }

            ENDCG
        }
    }

    Fallback "Sprites/Default"
}