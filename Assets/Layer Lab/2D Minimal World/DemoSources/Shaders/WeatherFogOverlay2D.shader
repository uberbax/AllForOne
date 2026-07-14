Shader "LayerLab/Weather/FogOverlay2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FogFocusPosition ("Fog Focus Position", Vector) = (0, 0, 0, 0)
        _FogClearRadius ("Clear Radius", Float) = 2.5
        _FogFullRadius ("Full Fog Radius", Float) = 8
        _FogGradientPower ("Gradient Power", Float) = 1.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FogOverlay2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _FogFocusPosition;
                float _FogClearRadius;
                float _FogFullRadius;
                float _FogGradientPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.color = input.color * _Color;
                output.texcoord = input.texcoord;
                output.worldPos = positionInputs.positionWS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
                half4 color = sprite * input.color;

                float clearRadius = max(0.0, _FogClearRadius);
                float fullRadius = max(clearRadius + 0.0001, _FogFullRadius);
                float distanceToFocus = distance(input.worldPos, _FogFocusPosition.xy);
                float fogT = saturate((distanceToFocus - clearRadius) / (fullRadius - clearRadius));
                fogT = fogT * fogT * (3.0 - 2.0 * fogT);
                fogT = pow(fogT, max(0.1, _FogGradientPower));
                color.a *= fogT;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
