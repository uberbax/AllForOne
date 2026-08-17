Shader "Custom/2D/Sprite Grass Wind"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _WindStrength ("Wind Strength", Range(0, 0.2)) = 0.035
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0
        _WindScale ("Wind Scale", Range(0, 10)) = 2.0

        _BendPower ("Bend Power", Range(0.5, 5)) = 2.2
        _TextureSway ("Texture Sway", Range(0, 0.05)) = 0.012
        _TextureSwayFrequency ("Texture Sway Frequency", Range(0, 30)) = 10.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
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
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float _WindStrength;
            float _WindSpeed;
            float _WindScale;

            float _BendPower;
            float _TextureSway;
            float _TextureSwayFrequency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;

                float wind : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // 0 = низ спрайта, 1 = верх спрайта
                float height01 = saturate(v.uv.y);

                // Низ почти не двигается, верх двигается сильнее
                float bendMask = pow(height01, _BendPower);

                // Разная фаза для разных кустов по позиции в мире
                float phase = worldPos.x * _WindScale + worldPos.y * 0.37;

                float wind =
                    sin(_Time.y * _WindSpeed + phase) * 0.65 +
                    sin(_Time.y * _WindSpeed * 1.7 + phase * 1.3) * 0.35;

                float3 pos = v.vertex.xyz;

                // Основной наклон травы
                pos.x += wind * _WindStrength * bendMask;

                o.vertex = UnityObjectToClipPos(float4(pos, 1.0));
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.wind = wind;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Небольшое внутреннее шевеление текстуры.
                // Низ почти не трогаем.
                float height01 = saturate(uv.y);
                float swayMask = pow(height01, _BendPower);

                uv.x += sin(
                    _Time.y * _WindSpeed * 1.4 +
                    uv.y * _TextureSwayFrequency +
                    i.wind
                ) * _TextureSway * swayMask;

                fixed4 tex = tex2D(_MainTex, uv);

                return tex * i.color;
            }
            ENDCG
        }
    }
}