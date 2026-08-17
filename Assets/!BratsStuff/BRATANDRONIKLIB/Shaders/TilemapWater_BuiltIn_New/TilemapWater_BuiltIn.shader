Shader "Custom/BuiltIn/Tilemap Water 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Renderer Tint", Color) = (1,1,1,1)
        _WaterTint ("Water Tint", Color) = (1,1,1,1)

        [Header(Water Wobble)]
        _WobbleAmount ("Wobble Amount", Range(0, 0.08)) = 0.012
        _WobbleFrequency ("Wobble Frequency", Range(0.1, 12)) = 3.2
        _WobbleSpeed ("Wobble Speed", Range(-8, 8)) = 1.15

        [Header(White Wave Lines)]
        _WaveColor ("Wave Color", Color) = (1,1,1,0.8)
        _LineDensity ("Line Density", Range(0.5, 30)) = 8.0
        _LineWidth ("Line Width", Range(0.01, 0.45)) = 0.12
        _LineSpeed ("Line Vertical Speed", Range(-8, 8)) = 0.9
        _LineStrength ("Line Strength", Range(0, 1)) = 0.75
        _LineBend ("Line Bend", Range(0, 1.5)) = 0.18
        _LineBendFrequency ("Line Bend Frequency", Range(0.1, 12)) = 2.2

        [Header(Line Segments And Fading)]
        _DashDensity ("Segment Density", Range(0.2, 20)) = 4.0
        _DashSpeed ("Segment Speed", Range(-8, 8)) = 0.55
        _DashCut ("Segment Length", Range(0, 0.95)) = 0.35
        _PulseSpeed ("Appear / Disappear Speed", Range(0, 8)) = 1.1

        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
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
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _WaterTint;
            fixed4 _WaveColor;

            float _WobbleAmount;
            float _WobbleFrequency;
            float _WobbleSpeed;

            float _LineDensity;
            float _LineWidth;
            float _LineSpeed;
            float _LineStrength;
            float _LineBend;
            float _LineBendFrequency;

            float _DashDensity;
            float _DashSpeed;
            float _DashCut;
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

            v2f vert(appdata v)
            {
                v2f o;

                float4 world = mul(unity_ObjectToWorld, v.vertex);

                // Two small waves keep the motion organic without deforming the tilemap too much.
                float wobbleA = sin(world.x * _WobbleFrequency + _Time.y * _WobbleSpeed);
                float wobbleB = sin(world.x * (_WobbleFrequency * 0.47) - _Time.y * (_WobbleSpeed * 0.73));
                world.y += (wobbleA + wobbleB * 0.5) * _WobbleAmount;

                o.vertex = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xy;
                o.texcoord = v.texcoord;
                o.color = v.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                    o.vertex = UnityPixelSnap(o.vertex);
                #endif

                return o;
            }

            float MakeWaveLine(
                float2 worldPos,
                float time,
                float density,
                float speed,
                float phaseOffset,
                float xOffset)
            {
                // Mostly horizontal line with a very small bend.
                float bend = sin((worldPos.x + xOffset) * _LineBendFrequency + time * 0.55) * _LineBend;
                float phase = worldPos.y * density + bend + time * speed + phaseOffset;

                // Narrow bright stripe around the peak of the sine wave.
                float crest = 0.5 + 0.5 * sin(phase);
                float stripe = smoothstep(1.0 - _LineWidth, 1.0, crest);

                // Break the stripe into short horizontal segments.
                float dashWave = 0.5 + 0.5 * sin((worldPos.x + xOffset) * _DashDensity - time * _DashSpeed);
                float dash = smoothstep(_DashCut, 1.0, dashWave);

                // Slowly make different lines appear and disappear.
                float pulseWave = 0.5 + 0.5 * sin(time * _PulseSpeed + worldPos.y * 1.37 + phaseOffset);
                float pulse = smoothstep(0.18, 0.92, pulseWave);

                return stripe * dash * pulse;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                col *= _WaterTint;

                // Do not draw procedural waves outside the sprite's visible pixels.
                if (col.a <= 0.001)
                    discard;

                float t = _Time.y;

                float waveA = MakeWaveLine(i.worldPos, t, _LineDensity, _LineSpeed, 0.0, 0.0);
                float waveB = MakeWaveLine(
                    i.worldPos,
                    t,
                    _LineDensity * 0.71,
                    -_LineSpeed * 0.63,
                    2.15,
                    0.83);

                float waves = saturate(max(waveA, waveB) * _LineStrength);
                float waveBlend = saturate(waves * _WaveColor.a);
                col.rgb = lerp(col.rgb, _WaveColor.rgb, waveBlend);

                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
