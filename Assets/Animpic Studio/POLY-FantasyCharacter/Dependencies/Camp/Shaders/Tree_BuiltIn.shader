Shader "Animpic Studio/Fantasy Character/Tree/Built-in"
{
    Properties
    {
        [Header(Bark Surface)]
        [MainTexture] _BaseColor("Base Color Texture", 2D) = "white" {}
        [MainColor] _Tint("Texture Tint", Color) = (1, 1, 1, 1)
        _Alpha("Alpha", Range(0, 4)) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.1
        _Occlusion("Occlusion", Range(0, 1)) = 1
        _NormalStrength("Normal From Color", Range(0, 8)) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 2

        [Header(Trunk Color Gradient)]
        [Toggle] _TreeGradientEnabled("Gradient Enabled", Float) = 0
        _TreeBottomColor("Bottom Color", Color) = (0.75, 0.7, 0.6, 1)
        _TreeTopColor("Top Color", Color) = (1, 1, 1, 1)
        [Enum(UV0 V,0,UV1 V,1,Vertex Color R,2,Object Y,3)] _TreeGradientSource("Gradient Source", Float) = 3
        _TreeGradientScale("Gradient Height", Range(0.05, 4)) = 1
        _TreeGradientOffset("Gradient Offset", Range(-1, 1)) = 0
        [Toggle] _TreeGradientInvert("Invert Gradient", Float) = 0
        _TreeGradientObjectBottom("Object Bottom", Float) = 0
        _TreeGradientObjectTop("Object Top", Float) = 1
        _TreeGradientBreakup("Gradient Breakup", Range(0, 1)) = 0.1
        _TreeGradientBreakupScale("Breakup Scale", Float) = 1

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
        _TriplanarSharpness("Projection Sharpness", Range(1, 16)) = 6
        _TriplanarSeamSmoothing("Projection Seam Smoothing", Range(0, 1)) = 0.25

        [Header(Tree Wind)]
        [Toggle] _UseGlobalWind("Use Global Wind", Float) = 1
        _WindResponse("Wind Response", Range(0, 2)) = 1
        [Toggle] _Wind_Enabled("Wind Enabled", Float) = 1
        _WindPower("Wind Strength", Range(0, 20)) = 5
        _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0, 10)) = 1
        _UV("World Wave Scale", Vector) = (0.35, 0.35, 0, 0)
        _WindTurbulence("Wind Turbulence", Range(0, 3)) = 1
        _WindRootHeight("Root Height (Object Y)", Float) = 0
        _WindFlexibility("Flexibility per Meter", Range(0.01, 2)) = 0.65
    }

    SubShader
    {
        // Preserve each renderer's object space for wind and moss projection.
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
        LOD 300
        Cull [_Cull]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface Surface Standard fullforwardshadows addshadow vertex:Vertex
        #pragma shader_feature_local _ALPHATEST_ON
        #pragma multi_compile_instancing

        #include "UnityCG.cginc"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampTreeCommon.hlsl"

        sampler2D _BaseColor;
        sampler2D _MossTexture;
        sampler2D _TopMossTexture;
        float4 _Tint;
        float _Alpha;
        float _Cutoff;
        float _Metallic;
        float _Smoothness;
        float _Occlusion;
        float _NormalStrength;
        float _TreeGradientEnabled;
        float4 _TreeBottomColor;
        float4 _TreeTopColor;
        float _TreeGradientSource;
        float _TreeGradientScale;
        float _TreeGradientOffset;
        float _TreeGradientInvert;
        float _TreeGradientObjectBottom;
        float _TreeGradientObjectTop;
        float _TreeGradientBreakup;
        float _TreeGradientBreakupScale;
        float _UseGlobalWind;
        float _WindResponse;
        float _Wind_Enabled;
        float _WindPower;
        float4 _WindDirection;
        float _WindSpeed;
        float4 _UV;
        float _WindTurbulence;
        float _WindRootHeight;
        float _WindFlexibility;
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
        float _TriplanarSharpness;
        float _TriplanarSeamSmoothing;

        fixed4 TreeSampleMossTriplanar(sampler2D textureSampler,
            float3 positionOS, float tiling, float3 weights)
        {
            float scale = max(abs(tiling), 0.0001);
            fixed4 sideX = tex2D(textureSampler, positionOS.zy * scale);
            fixed4 topY = tex2D(textureSampler, positionOS.xz * scale);
            fixed4 sideZ = tex2D(textureSampler, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        float3 TreeApplyMoss(float3 albedo, float3 mossPositionOS,
            float mossHeightFromPivot)
        {
            if (_MossEnabled > 0.5 || (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0))
            {
                // Undeformed coordinates keep the texture attached to the bark.
                float3 mossNormalOS = normalize(cross(
                    ddx(mossPositionOS), ddy(mossPositionOS)));
                float3 mossWeights = FantasyCampTriplanarWeights(mossNormalOS,
                    _TriplanarSharpness, _TriplanarSeamSmoothing);
                if (_MossEnabled > 0.5)
                {
                    float mossMask = FantasyCampMossMask(mossHeightFromPivot,
                        _MossGroundHeight, _MossHeight, _MossEdgeSoftness,
                        _MossIntensity);
                    float3 moss = TreeSampleMossTriplanar(_MossTexture, mossPositionOS,
                        _MossTiling, mossWeights).rgb * _MossColor.rgb;
                    albedo = lerp(albedo, moss, mossMask);
                }
                if (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0)
                {
                    float topMossMask = FantasyCampTopMossMask(mossHeightFromPivot,
                        _TopMossTopHeight, _TopMossHeight, _TopMossEdgeSoftness,
                        _TopMossIntensity);
                    float3 topMoss = TreeSampleMossTriplanar(_TopMossTexture, mossPositionOS,
                        _TopMossTiling, mossWeights).rgb * _TopMossColor.rgb;
                    albedo = lerp(albedo, topMoss, topMossMask);
                }
            }
            return albedo;
        }

        struct Input
        {
            float2 uv_BaseColor;
            float treeGradient;
            float4 mossData;
            fixed facing : VFACE;
        };

        void Vertex(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            float3 originalOS = vertex.vertex.xyz;
            float2 uv0 = vertex.texcoord.xy;
            float2 uv1 = vertex.texcoord1.xy;
            float4 vertexColor = vertex.color;

            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled > 0.5)
            {
                float3 positionWS = mul(unity_ObjectToWorld, float4(originalOS, 1.0)).xyz;
                float3 originWS = mul(unity_ObjectToWorld,
                    float4(0.0, 0.0, 0.0, 1.0)).xyz;
                float windStrength = FantasyCampResolveWindStrength(_WindPower,
                    _UseGlobalWind, _WindResponse);
                float2 windDirection = FantasyCampResolveWindDirection(
                    _WindDirection.xy, _UseGlobalWind);
                float windSpeed = FantasyCampResolveWindSpeed(_WindSpeed,
                    _UseGlobalWind);
                float2 waveScale = FantasyCampResolveWindWaveScale(_UV.xy,
                    _UseGlobalWind);
                float turbulence = FantasyCampResolveWindTurbulence(
                    _WindTurbulence, _UseGlobalWind);
                float rootHeight = FantasyCampResolveWindRootHeight(
                    _WindRootHeight, _UseGlobalWind);
                float flexibility = FantasyCampResolveWindFlexibility(
                    _WindFlexibility, _UseGlobalWind);
                float3 offsetWS = FantasyCampTreeWindOffsetWS(positionWS,
                    originWS, originalOS.y, _Time.y, windStrength,
                    windDirection, windSpeed, waveScale, turbulence,
                    rootHeight, flexibility);
                vertex.vertex.xyz += mul((float3x3)unity_WorldToObject, offsetWS);
            }
            output.treeGradient = FantasyCampTreeGradientFactor(uv0, uv1,
                vertexColor, originalOS, _TreeGradientSource,
                _TreeGradientObjectBottom, _TreeGradientObjectTop,
                _TreeGradientInvert, _TreeGradientScale, _TreeGradientOffset,
                _TreeGradientBreakup, _TreeGradientBreakupScale);
            // Rest XYZ keeps the texture stable; post-wind height keeps the mask horizontal.
            output.mossData = float4(originalOS,
                FantasyCampMossHeightFromPivot(vertex.vertex.xyz,
                    (float3x3)unity_ObjectToWorld));
        }

        void Surface(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 textureSample = tex2D(_BaseColor, input.uv_BaseColor);
            #if defined(_ALPHATEST_ON)
                clip(textureSample.a * _Alpha * _Tint.a - _Cutoff);
            #endif

            fixed3 gradientColor = lerp(_TreeBottomColor.rgb, _TreeTopColor.rgb,
                saturate(input.treeGradient));
            fixed3 tint = _Tint.rgb * lerp(float3(1.0, 1.0, 1.0), gradientColor,
                saturate(_TreeGradientEnabled));
            output.Albedo = TreeApplyMoss(textureSample.rgb * tint,
                input.mossData.xyz, input.mossData.w);
            output.Alpha = textureSample.a * _Alpha * _Tint.a;
            output.Metallic = _Metallic;
            output.Smoothness = _Smoothness;
            output.Occlusion = _Occlusion;
            if (_NormalStrength > 0.0001)
            {
                float3 normalTS = FantasyCampTreeNormalFromColor(textureSample.rgb,
                    _NormalStrength);
                output.Normal = normalTS * (input.facing >= 0.0 ? 1.0 : -1.0);
            }
        }
        ENDCG
    }

    FallBack "Standard"
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampTreeShaderGUI"
}
