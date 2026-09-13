Shader "Custom/TilemapWater2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.25, 0.65, 1.0, 0.8)

        _WaveColor ("Wave Color", Color) = (0.5, 0.9, 1.0, 1.0)

        _WaveSpeed1 ("Wave Speed 1", Vector) = (0.08, 0.03, 0, 0)
        _WaveSpeed2 ("Wave Speed 2", Vector) = (-0.04, 0.06, 0, 0)

        _WaveScale1 ("Wave Scale 1", Float) = 8
        _WaveScale2 ("Wave Scale 2", Float) = 13

        _Distortion ("Distortion", Range(0, 0.1)) = 0.015
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.25
        _Brightness ("Brightness", Range(0, 2)) = 1

        _Alpha ("Alpha", Range(0, 1)) = 0.85
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

            float4 _MainTex_ST;
            float4 _Color;
            float4 _WaveColor;

            float4 _WaveSpeed1;
            float4 _WaveSpeed2;

            float _WaveScale1;
            float _WaveScale2;

            float _Distortion;
            float _WaveStrength;
            float _Brightness;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y;

                // Используем world position, чтобы вода шла непрерывно
                // через разные тайлы Tilemap.
                float2 pos = i.worldPos;

                float wave1 =
                    sin(
                        pos.x * _WaveScale1 +
                        pos.y * (_WaveScale1 * 0.7) +
                        time * 2.0
                    );

                float wave2 =
                    sin(
                        pos.x * (_WaveScale2 * 0.6) -
                        pos.y * _WaveScale2 +
                        time * 1.3
                    );

                float2 distortion;

                distortion.x =
                    wave1 * _Distortion +
                    sin(pos.y * 5.0 + time) * _Distortion * 0.5;

                distortion.y =
                    wave2 * _Distortion +
                    cos(pos.x * 4.0 + time * 0.8) * _Distortion * 0.5;

                float2 uv = i.uv + distortion;

                fixed4 tex = tex2D(_MainTex, uv);

                // animated highlights
                float highlight =
                    saturate(
                        (wave1 + wave2) * 0.5
                    );

                fixed3 waterColor =
                    tex.rgb * _Color.rgb;

                waterColor =
                    lerp(
                        waterColor,
                        _WaveColor.rgb,
                        highlight * _WaveStrength
                    );

                waterColor *= _Brightness;

                fixed4 col;

                col.rgb = waterColor;
                col.a = tex.a * _Color.a * _Alpha;

                col *= i.color;

                return col;
            }

            ENDCG
        }
    }

    Fallback "Sprites/Default"
}