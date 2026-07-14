Shader "Sprites/FoliageWind2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _MaskTex ("Mask", 2D) = "white" {}
        [HideInInspector] _NormalMap ("Normal Map", 2D) = "bump" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _WindAnchor ("Wind Anchor", Vector) = (0, 0, 0, 0)
        [HideInInspector] _WindPivotY ("Pivot Y", Float) = 0
        [HideInInspector] _WindHeight ("Height", Float) = 1

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

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
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _WindAnchor;
                float _WindPivotY;
                float _WindHeight;
            CBUFFER_END

            float _FoliageWindEnabled;
            float _FoliageWindStrength;
            float _FoliageWindSpeed;
            float _FoliageWindScale;
            float _FoliageWindBendStart;
            float _FoliageWindBendPower;
            float _FoliageWindPhaseOffset;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 screenPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float WindWave()
            {
                float phase = _Time.y * _FoliageWindSpeed
                    + (_WindAnchor.x * 0.73 + _WindAnchor.y * 0.37) * _FoliageWindScale
                    + _FoliageWindPhaseOffset;
                return sin(phase) + sin(phase * 1.73 + 1.41) * 0.32;
            }

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();

                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                float height = max(_WindHeight, 0.0001);
                float bendStart = saturate(_FoliageWindBendStart);
                float bendStartY = _WindPivotY + height * bendStart;
                float bendRange = max(height - height * bendStart, 0.0001);
                float bend01 = saturate((positionOS.y - bendStartY) / bendRange);
                float bendWeight = pow(bend01, max(_FoliageWindBendPower, 0.0001));
                float sway = WindWave() * _FoliageWindStrength * bendWeight * saturate(_FoliageWindEnabled);

                float3 worldPos = TransformObjectToWorld(positionOS);
                worldPos.x += sway;

                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                o.screenPosition = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(sprite.rgb, sprite.a, mask, surfaceData);
                InitializeInputData(input.uv, half2(input.screenPosition.xy / input.screenPosition.w), inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
