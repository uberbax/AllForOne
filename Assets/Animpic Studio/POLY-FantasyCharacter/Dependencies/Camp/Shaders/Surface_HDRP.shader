Shader "Animpic Studio/Fantasy Character/Surface/HDRP"
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
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        LOD 250
        Cull Off

        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
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
            float _Cutoff;
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
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            float normalizedRustHeight : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float4 SampleSurface(float2 uv)
        {
            float4 value = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            #if defined(_ALPHATEST_ON)
                clip(value.a - _Cutoff);
            #endif
            return value;
        }
        float4 SampleMossTriplanar(float3 positionOS, float3 weights)
        {
            float scale = max(abs(_MossTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        float4 SampleTopMossTriplanar(float3 positionOS, float3 weights)
        {
            float scale = max(abs(_TopMossTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }
        float4 SampleDetailTriplanar(float3 positionWS, float3 weights)
        {
            float scale = max(abs(_DetailTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_DetailTexture, sampler_DetailTexture, positionWS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }
        float4 SampleRustTriplanar(float3 positionWS, float3 weights)
        {
            float scale = max(abs(_RustTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_RustTexture, sampler_RustTexture, positionWS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }
        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 positionOS = input.positionOS;
            output.positionWS = TransformObjectToWorld(positionOS);
            float3 heightSources = float3(input.uv.y, input.uv1.y,
                input.color.r);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.normalizedRustHeight = FantasyCampSelectNormalizedHeight(
                heightSources, _RustHeightSource, _RustHeightInvert);
            output.uv = input.uv * _BaseColor_ST.xy + _BaseColor_ST.zw;
            return output;
        }

        struct DepthVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        DepthVaryings DepthVert(Attributes input)
        {
            DepthVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = input.uv * _BaseColor_ST.xy + _BaseColor_ST.zw;
            return output;
        }

        float4 DepthFragment(DepthVaryings input) : SV_Target
        {
            SampleSurface(input.uv);
            return 0;
        }
        ENDHLSL

        // A compact forward adapter. It intentionally keeps the shared surface
        // contract small; full HDRP Lit features can be added in a later pass.
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
            #else
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH
            #endif
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"

            float4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 tex = SampleSurface(input.uv);
                // HDRP uses camera-relative positions for lighting. Convert
                // once so world-projected textures remain fixed in space.
                float3 surfacePositionWS = GetAbsolutePositionWS(
                    input.positionWS);
                float3 projectionWeights = float3(1.0, 0.0, 0.0);
                if (_DetailEnabled > 0.5
                    || (_RustEnabled > 0.5 && _RustUseTexture > 0.5))
                {
                    float3 geometricNormalWS = normalize(cross(
                        ddx(surfacePositionWS), ddy(surfacePositionWS)));
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
                    // HDRP's inverse matrix expects camera-relative world coordinates.
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
                        SampleDetailTriplanar(surfacePositionWS,
                            projectionWeights), _DetailColor.rgb,
                        _DetailIntensity, _DetailBlendMode);
                if (_MossEnabled > 0.5)
                {
                    float mossMask = FantasyCampMossMask(mossHeightFromPivot,
                        _MossGroundHeight, _MossHeight, _MossEdgeSoftness,
                        _MossIntensity);
                    float3 moss = SampleMossTriplanar(mossPositionOS,
                        mossProjectionWeights).rgb * _MossColor.rgb;
                    tex.rgb = lerp(tex.rgb, moss, mossMask);
                }
                if (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0)
                {
                    float topMossMask = FantasyCampTopMossMask(mossHeightFromPivot,
                        _TopMossTopHeight, _TopMossHeight, _TopMossEdgeSoftness,
                        _TopMossIntensity);
                    float3 topMoss = SampleTopMossTriplanar(mossPositionOS,
                        mossProjectionWeights).rgb * _TopMossColor.rgb;
                    tex.rgb = lerp(tex.rgb, topMoss, topMossMask);
                }
                if (_RustEnabled > 0.5)
                {
                    float topDownMask = FantasyCampRustTopDownNormalizedMask(
                        input.normalizedRustHeight,
                        _RustDownwardCoverage, _RustTopDownSoftness,
                        _RustTopDownIntensity);
                    float rustMask = topDownMask;
                    if (_RustProceduralEnabled > 0.5)
                    {
                        float proceduralMask = FantasyCampProceduralRustMask(surfacePositionWS,
                            _RustProceduralScale, _RustProceduralCoverage,
                            _RustProceduralSoftness, _RustProceduralSeed);
                        rustMask *= lerp(1.0, proceduralMask, _RustProceduralInfluence);
                    }
                    if (_RustFlowEnabled > 0.5)
                    {
                        float flowMask = FantasyCampRustFlowMask(surfacePositionWS, _RustFlowScale,
                            _RustFlowStretch, _RustFlowDistortion, _RustFlowCoverage,
                            _RustFlowSoftness, _RustProceduralSeed);
                        rustMask *= lerp(1.0, flowMask, _RustFlowInfluence);
                    }
                    float3 rustTexture = _RustUseTexture > 0.5
                        ? SampleRustTriplanar(surfacePositionWS,
                            projectionWeights).rgb
                        : float3(1.0, 1.0, 1.0);
                    float3 rust = rustTexture * _RustColor.rgb;
                    tex.rgb = lerp(tex.rgb, rust, rustMask);
                }
                float3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0 : -1.0);
                float3 lighting = 0.18;
                if (_DirectionalLightCount > 0)
                {
                    DirectionalLightData light = _DirectionalLightDatas[0];
                    float3 lightDirectionWS = -light.forward;
                    float shadow = 1.0;
                    if (light.shadowIndex >= 0 && light.shadowDimmer > 0.0)
                    {
                        HDShadowContext shadowContext = InitShadowContext();
                        shadow = GetDirectionalShadowAttenuation(shadowContext,
                            input.positionCS.xy, input.positionWS, normalWS,
                            light.shadowIndex, lightDirectionWS);
                        shadow = lerp(1.0, shadow,
                            saturate(light.shadowDimmer));
                    }
                    float ndotl = saturate(dot(normalWS, lightDirectionWS));
                    lighting += light.color * light.lightDimmer
                        * light.diffuseDimmer * ndotl * shadow;
                }
                return float4(tex.rgb * lighting
                    * GetCurrentExposureMultiplier(), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode"="DepthForwardOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampSurfaceShaderGUI"
}
