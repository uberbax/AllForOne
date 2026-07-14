Shader "Sprites/FoliageWindShadow2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _MaskTex ("Mask", 2D) = "white" {}
        [HideInInspector] _WindAnchor ("Wind Anchor", Vector) = (0, 0, 0, 0)

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

        Stencil
        {
            Ref 64
            ReadMask 64
            WriteMask 64
            Comp NotEqual
            Pass Replace
        }

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
                float4 _WindAnchor;
            CBUFFER_END

            float _FoliageWindEnabled;
            float _FoliageWindSpeed;
            float _FoliageWindScale;
            float _FoliageWindPhaseOffset;
            float _FoliageWindShadowStrength;
            half4 _FoliageWindShadowColor;
            float _FoliageWindShadowAlphaThreshold;

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
                float3 worldPos = TransformObjectToWorld(positionOS);
                worldPos.x += WindWave() * _FoliageWindShadowStrength * saturate(_FoliageWindEnabled);

                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = input.uv;
                o.color = input.color * unity_SpriteColor;
                o.screenPosition = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _FoliageWindShadowAlphaThreshold);

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(_FoliageWindShadowColor.rgb, input.color.a * _FoliageWindShadowColor.a, mask, surfaceData);
                InitializeInputData(input.uv, half2(input.screenPosition.xy / input.screenPosition.w), inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
