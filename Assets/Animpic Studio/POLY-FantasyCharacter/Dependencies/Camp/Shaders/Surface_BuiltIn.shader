Shader "Animpic Studio/Fantasy Character/Surface/Built-in"
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        LOD 250
        Cull Off

        CGPROGRAM
        #pragma surface Surface Lambert fullforwardshadows addshadow vertex:Vertex
        #pragma target 3.0
        #pragma shader_feature_local _ALPHATEST_ON
        #pragma multi_compile_instancing

        #include "UnityCG.cginc"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampSurfaceCommon.hlsl"

        sampler2D _BaseColor;
        sampler2D _MossTexture;
        sampler2D _TopMossTexture;
        sampler2D _DetailTexture;
        sampler2D _RustTexture;
        half _Cutoff;
        float _TriplanarSharpness;
        float _TriplanarSeamSmoothing;
        float _MossEnabled;
        float4 _MossColor;
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
        float _DetailEnabled;
        float4 _DetailColor;
        float _DetailTiling;
        float _DetailIntensity;
        float _DetailBlendMode;
        float _RustEnabled;
        float _RustUseTexture;
        float4 _RustColor;
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

        struct Input
        {
            float2 uv_BaseColor;
            float3 worldPos;
            float normalizedRustHeight;
            float facing : VFACE;
        };

        fixed4 SampleTriplanar(
            sampler2D textureSampler,
            float3 projectionPosition,
            float tiling,
            float3 weights)
        {
            float scale = max(abs(tiling), 0.0001);
            fixed4 sideX = tex2D(textureSampler, projectionPosition.zy * scale);
            fixed4 topY = tex2D(textureSampler, projectionPosition.xz * scale);
            fixed4 sideZ = tex2D(textureSampler, projectionPosition.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        void Vertex(inout appdata_full v, out Input outputInput)
        {
            UNITY_INITIALIZE_OUTPUT(Input, outputInput);
            float3 heightSources = float3(v.texcoord.y, v.texcoord1.y, v.color.r);
            outputInput.normalizedRustHeight = FantasyCampSelectNormalizedHeight(
                heightSources, _RustHeightSource, _RustHeightInvert);
        }

        void Surface(Input input, inout SurfaceOutput output)
        {
            fixed4 sampleValue = tex2D(_BaseColor, input.uv_BaseColor);
            #if defined(_ALPHATEST_ON)
                clip(sampleValue.a - _Cutoff);
            #endif
            float3 triplanarWeights = float3(1.0, 0.0, 0.0);
            if (_DetailEnabled > 0.5
                || (_RustEnabled > 0.5 && _RustUseTexture > 0.5))
            {
                float3 geometricNormalWS = normalize(cross(
                    ddx(input.worldPos), ddy(input.worldPos)));
                triplanarWeights = FantasyCampTriplanarWeights(
                    geometricNormalWS, _TriplanarSharpness,
                    _TriplanarSeamSmoothing);
            }
            float3 mossPositionOS = float3(0.0, 0.0, 0.0);
            float mossHeightFromPivot = 0.0;
            float3 mossProjectionWeights = float3(1.0, 0.0, 0.0);
            if (_MossEnabled > 0.5
                || (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0))
            {
                mossPositionOS = mul(unity_WorldToObject,
                    float4(input.worldPos, 1.0)).xyz;
                mossHeightFromPivot = FantasyCampMossHeightFromPivot(
                    mossPositionOS, (float3x3)unity_ObjectToWorld);
                float3 mossNormalOS = normalize(cross(
                    ddx(mossPositionOS), ddy(mossPositionOS)));
                mossProjectionWeights = FantasyCampTriplanarWeights(
                    mossNormalOS, _TriplanarSharpness,
                    _TriplanarSeamSmoothing);
            }
            if (_DetailEnabled > 0.5)
            {
                fixed4 detail = SampleTriplanar(_DetailTexture, input.worldPos,
                    _DetailTiling, triplanarWeights);
                sampleValue.rgb = FantasyCampApplyDetail(sampleValue.rgb, detail,
                    _DetailColor.rgb, _DetailIntensity, _DetailBlendMode);
            }
            fixed3 finalAlbedo = sampleValue.rgb;
            if (_MossEnabled > 0.5)
            {
                float mossMask = FantasyCampMossMask(mossHeightFromPivot, _MossGroundHeight,
                    _MossHeight, _MossEdgeSoftness, _MossIntensity);
                fixed3 moss = SampleTriplanar(_MossTexture, mossPositionOS,
                    _MossTiling, mossProjectionWeights).rgb * _MossColor.rgb;
                finalAlbedo = lerp(finalAlbedo, moss, mossMask);
            }
            if (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0)
            {
                float topMossMask = FantasyCampTopMossMask(mossHeightFromPivot,
                    _TopMossTopHeight, _TopMossHeight, _TopMossEdgeSoftness,
                    _TopMossIntensity);
                fixed3 topMoss = SampleTriplanar(_TopMossTexture, mossPositionOS,
                    _TopMossTiling, mossProjectionWeights).rgb * _TopMossColor.rgb;
                finalAlbedo = lerp(finalAlbedo, topMoss, topMossMask);
            }
            if (_RustEnabled > 0.5)
            {
                float rustMask = FantasyCampRustTopDownNormalizedMask(
                    input.normalizedRustHeight, _RustDownwardCoverage,
                    _RustTopDownSoftness, _RustTopDownIntensity);
                if (_RustProceduralEnabled > 0.5)
                {
                    float proceduralMask = FantasyCampProceduralRustMask(input.worldPos,
                        _RustProceduralScale, _RustProceduralCoverage,
                        _RustProceduralSoftness, _RustProceduralSeed);
                    rustMask *= lerp(1.0, proceduralMask, _RustProceduralInfluence);
                }
                if (_RustFlowEnabled > 0.5)
                {
                    float flowMask = FantasyCampRustFlowMask(input.worldPos, _RustFlowScale,
                        _RustFlowStretch, _RustFlowDistortion, _RustFlowCoverage,
                        _RustFlowSoftness, _RustProceduralSeed);
                    rustMask *= lerp(1.0, flowMask, _RustFlowInfluence);
                }
                fixed3 rustTexture = _RustUseTexture > 0.5
                    ? SampleTriplanar(_RustTexture, input.worldPos,
                        _RustTiling, triplanarWeights).rgb : fixed3(1.0, 1.0, 1.0);
                fixed3 rust = rustTexture * _RustColor.rgb;
                finalAlbedo = lerp(finalAlbedo, rust, rustMask);
            }
            output.Albedo = finalAlbedo;
            output.Normal = input.facing >= 0.0
                ? float3(0.0, 0.0, 1.0)
                : float3(0.0, 0.0, -1.0);
            output.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampSurfaceShaderGUI"
}
