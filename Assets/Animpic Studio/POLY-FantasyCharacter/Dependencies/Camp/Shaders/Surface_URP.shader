Shader "Animpic Studio/Fantasy Character/Surface/URP"
{
    Properties
    {
        [MainTexture] _BaseColor("Base Color", 2D) = "white" {}
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [ToggleUI] _DetailEnabled("Detail Enabled", Float) = 0
        [NoScaleOffset] _DetailTexture("Detail Texture", 2D) = "white" {}
        _DetailColor("Detail Color", Color) = (1, 1, 1, 1)
        _DetailTiling("Detail Tiling", Float) = 1
        _DetailIntensity("Detail Intensity", Range(0, 1)) = 1
        [Enum(Multiply,0,Overlay,1,Alpha Blend,2)] _DetailBlendMode("Detail Blend Mode", Float) = 0

        [ToggleUI] _MossEnabled("Moss Enabled", Float) = 0
        _MossColor("Moss Color", Color) = (0.25, 0.45, 0.12, 1)
        [NoScaleOffset] _MossTexture("Moss Texture", 2D) = "white" {}
        _MossTiling("Moss Tiling (Object Space)", Float) = 1
        _MossGroundHeight("Moss Ground Offset (World Y)", Float) = 0
        _MossHeight("Moss Height", Float) = 1
        _MossIntensity("Moss Intensity", Range(0, 1)) = 1
        _MossEdgeSoftness("Moss Edge Softness", Range(0.001, 5)) = 0.25

        [ToggleUI] _TopMossEnabled("Top Moss Enabled", Float) = 0
        [NoScaleOffset] _TopMossTexture("Top Moss Texture", 2D) = "white" {}
        _TopMossColor("Top Moss Color", Color) = (0.25, 0.45, 0.12, 1)
        _TopMossTiling("Top Moss Tiling (Object Space)", Float) = 1
        _TopMossTopHeight("Top Moss Top Offset (World Y)", Float) = 1
        _TopMossHeight("Top Moss Coverage Depth", Float) = 1
        _TopMossIntensity("Top Moss Intensity", Range(0, 1)) = 1
        _TopMossEdgeSoftness("Top Moss Edge Softness", Range(0.001, 5)) = 0.25

        [ToggleUI] _RustEnabled("Rust Enabled", Float) = 0
        [NoScaleOffset] _RustTexture("Rust Texture", 2D) = "white" {}
        [ToggleUI] _RustUseTexture("Use Rust Texture", Float) = 1
        _RustColor("Rust Color", Color) = (0.55, 0.18, 0.05, 1)
        _RustTiling("Rust Tiling", Float) = 1
        [ToggleUI] _RustProceduralEnabled("Procedural Rust Enabled", Float) = 0
        _RustProceduralScale("Procedural Rust Scale", Float) = 2
        _RustProceduralCoverage("Procedural Rust Coverage", Range(0.05, 1.2)) = 0.45
        _RustProceduralSoftness("Procedural Rust Softness", Range(0.001, 0.5)) = 0.08
        _RustProceduralSeed("Procedural Rust Seed", Float) = 0
        _RustProceduralInfluence("Procedural Rust Influence", Range(0, 1)) = 1
        [ToggleUI] _RustFlowEnabled("Rust Flow Enabled", Float) = 1
        _RustFlowScale("Rust Flow Scale", Float) = 2
        _RustFlowStretch("Rust Flow Vertical Stretch", Range(1, 30)) = 10
        _RustFlowDistortion("Rust Flow Distortion", Range(0, 3)) = 0.5
        _RustFlowCoverage("Rust Flow Coverage", Range(0, 1)) = 0.5
        _RustFlowSoftness("Rust Flow Softness", Range(0.001, 0.5)) = 0.08
        _RustFlowInfluence("Rust Flow Influence", Range(0, 1)) = 1
        [Enum(UV0 V,0,UV1 V,1,Vertex Color R,2)] _RustHeightSource("Rust Height Source", Float) = 0
        [ToggleUI] _RustHeightInvert("Invert Rust Height", Float) = 0
        _RustDownwardCoverage("Rust Downward Coverage", Range(0, 1)) = 0.35
        _RustTopDownSoftness("Rust Top-down Softness", Range(0.001, 1)) = 0.1
        _RustTopDownIntensity("Rust Top-down Intensity", Range(0, 1)) = 1

        _TriplanarSharpness("Projection Sharpness", Range(1, 16)) = 6
        _TriplanarSeamSmoothing("Projection Seam Smoothing", Range(0, 1)) = 0.25
    }

    SubShader
    {
        // Preserve each renderer's object space for moss height and projection.
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        LOD 250
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampSurfaceCommon.hlsl"

        TEXTURE2D(_BaseColor);
        SAMPLER(sampler_BaseColor);
        TEXTURE2D(_MossTexture);
        SAMPLER(sampler_MossTexture);
        TEXTURE2D(_TopMossTexture);
        SAMPLER(sampler_TopMossTexture);
        TEXTURE2D(_DetailTexture);
        SAMPLER(sampler_DetailTexture);
        TEXTURE2D(_RustTexture);
        SAMPLER(sampler_RustTexture);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor_ST;
            half _Cutoff;
            float4 _MossColor;
            float _MossEnabled;
            float _MossTiling;
            float _MossGroundHeight;
            float _MossHeight;
            float _MossIntensity;
            float _MossEdgeSoftness;
            float _TopMossEnabled;
            float4 _TopMossColor;
            float _TopMossTiling;
            float _TopMossTopHeight;
            float _TopMossHeight;
            float _TopMossIntensity;
            float _TopMossEdgeSoftness;
            float4 _DetailColor;
            float _DetailEnabled;
            float _DetailTiling;
            float _DetailIntensity;
            float _DetailBlendMode;
            float _TriplanarSharpness;
            float _TriplanarSeamSmoothing;
            float4 _RustColor;
            float _RustEnabled;
            float _RustUseTexture;
            float _RustTiling;
            float _RustProceduralEnabled;
            float _RustProceduralScale;
            float _RustProceduralCoverage;
            float _RustProceduralSoftness;
            float _RustProceduralSeed;
            float _RustProceduralInfluence;
            float _RustFlowEnabled;
            float _RustFlowScale;
            float _RustFlowStretch;
            float _RustFlowDistortion;
            float _RustFlowCoverage;
            float _RustFlowSoftness;
            float _RustFlowInfluence;
            float _RustHeightSource;
            float _RustHeightInvert;
            float _RustDownwardCoverage;
            float _RustTopDownSoftness;
            float _RustTopDownIntensity;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            float2 staticLightmapUV : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        half4 SampleSurface(float2 uv)
        {
            half4 value = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            #if defined(_ALPHATEST_ON)
                clip(value.a - _Cutoff);
            #endif
            return value;
        }

        half4 SampleMossTriplanar(float3 positionOS, half3 weights)
        {
            float scale = max(abs(_MossTiling), 0.0001);
            half4 sideX = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.zy * scale);
            half4 topY = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xz * scale);
            half4 sideZ = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        half4 SampleTopMossTriplanar(float3 positionOS, half3 weights)
        {
            float scale = max(abs(_TopMossTiling), 0.0001);
            half4 sideX = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.zy * scale);
            half4 topY = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xz * scale);
            half4 sideZ = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        half4 SampleDetailTriplanar(float3 positionWS, half3 weights)
        {
            float scale = max(abs(_DetailTiling), 0.0001);
            half4 sideX = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.zy * scale);
            half4 topY = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.xz * scale);
            half4 sideZ = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        half4 SampleRustTriplanar(float3 positionWS, half3 weights)
        {
            float scale = max(abs(_RustTiling), 0.0001);
            half4 sideX = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.zy * scale);
            half4 topY = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.xz * scale);
            half4 sideZ = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }



        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                half normalizedRustHeight : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = input.positionOS.xyz;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                float3 heightSources = float3(input.uv.y,
                    input.staticLightmapUV.y, input.color.r);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalizedRustHeight = FantasyCampSelectNormalizedHeight(
                    heightSources, _RustHeightSource, _RustHeightInvert);
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 tex = SampleSurface(input.uv);
                half3 projectionWeights = half3(1.0h, 0.0h, 0.0h);
                if (_DetailEnabled > 0.5
                    || (_RustEnabled > 0.5 && _RustUseTexture > 0.5))
                {
                    half3 geometricNormalWS = normalize(cross(
                        ddx(input.positionWS), ddy(input.positionWS)));
                    projectionWeights = FantasyCampTriplanarWeights(
                        geometricNormalWS, _TriplanarSharpness,
                        _TriplanarSeamSmoothing);
                }
                float3 mossPositionOS = float3(0.0, 0.0, 0.0);
                float mossHeightFromPivot = 0.0;
                float3 mossProjectionWeights = float3(1.0, 0.0, 0.0);
                if (_MossEnabled > 0.5
                    || (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0))
                {
                    mossPositionOS = TransformWorldToObject(input.positionWS);
                    mossHeightFromPivot = FantasyCampMossHeightFromPivot(
                        mossPositionOS, (float3x3)GetObjectToWorldMatrix());
                    float3 mossNormalOS = normalize(cross(
                        ddx(mossPositionOS), ddy(mossPositionOS)));
                    mossProjectionWeights = FantasyCampTriplanarWeights(
                        mossNormalOS, _TriplanarSharpness,
                        _TriplanarSeamSmoothing);
                }
                if (_DetailEnabled > 0.5)
                    tex.rgb = FantasyCampApplyDetail(tex.rgb,
                        SampleDetailTriplanar(input.positionWS,
                            projectionWeights), _DetailColor.rgb,
                        _DetailIntensity, _DetailBlendMode);
                if (_MossEnabled > 0.5)
                {
                    half mossMask = FantasyCampMossMask(mossHeightFromPivot,
                        _MossGroundHeight, _MossHeight, _MossEdgeSoftness,
                        _MossIntensity);
                    half3 moss = SampleMossTriplanar(mossPositionOS,
                        mossProjectionWeights).rgb * _MossColor.rgb;
                    tex.rgb = lerp(tex.rgb, moss, mossMask);
                }
                if (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0)
                {
                    float topMossMask = FantasyCampTopMossMask(mossHeightFromPivot,
                        _TopMossTopHeight, _TopMossHeight, _TopMossEdgeSoftness,
                        _TopMossIntensity);
                    half3 topMoss = SampleTopMossTriplanar(mossPositionOS,
                        mossProjectionWeights).rgb * _TopMossColor.rgb;
                    tex.rgb = lerp(tex.rgb, topMoss, topMossMask);
                }
                if (_RustEnabled > 0.5)
                {
                    half topDownMask = FantasyCampRustTopDownNormalizedMask(
                        input.normalizedRustHeight,
                        _RustDownwardCoverage, _RustTopDownSoftness,
                        _RustTopDownIntensity);
                    half rustMask = topDownMask;
                    if (_RustProceduralEnabled > 0.5)
                    {
                        half proceduralMask = FantasyCampProceduralRustMask(input.positionWS,
                            _RustProceduralScale, _RustProceduralCoverage,
                            _RustProceduralSoftness, _RustProceduralSeed);
                        rustMask *= lerp(1.0h, proceduralMask, _RustProceduralInfluence);
                    }
                    if (_RustFlowEnabled > 0.5)
                    {
                        half flowMask = FantasyCampRustFlowMask(input.positionWS, _RustFlowScale,
                            _RustFlowStretch, _RustFlowDistortion, _RustFlowCoverage,
                            _RustFlowSoftness, _RustProceduralSeed);
                        rustMask *= lerp(1.0h, flowMask, _RustFlowInfluence);
                    }
                    half3 rustTexture = _RustUseTexture > 0.5
                        ? SampleRustTriplanar(input.positionWS,
                            projectionWeights).rgb
                        : half3(1.0h, 1.0h, 1.0h);
                    half3 rust = rustTexture * _RustColor.rgb;
                    tex.rgb = lerp(tex.rgb, rust, rustMask);
                }
                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS)
                    * faceSign;
                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 color = tex.rgb * (bakedGI + mainLight.color *
                    saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation);

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < count; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex,
                        input.positionWS, half4(1.0h, 1.0h, 1.0h, 1.0h));
                    color += tex.rgb * light.color * saturate(dot(normalWS, light.direction)) *
                        light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            // Populated by UniversalRenderPipeline.ShadowUtils.
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowVaryings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input):SV_Target { SampleSurface(input.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            struct DepthVaryings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            DepthVaryings DepthVert(Attributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }
            half4 DepthFrag(DepthVaryings input):SV_Target { SampleSurface(input.uv); return 0; }
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampSurfaceShaderGUI"
}
