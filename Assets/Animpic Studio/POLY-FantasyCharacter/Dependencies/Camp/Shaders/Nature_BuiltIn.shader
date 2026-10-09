Shader "Animpic Studio/Fantasy Character/Nature/Built-in"
{
    Properties
    {
        [Header(Appearance)]
        [MainTexture] _BaseColor("Base Color Texture", 2D) = "white" {}
        [MainColor] _Tint("Texture Tint", Color) = (1, 1, 1, 1)
        _Alpha("Alpha", Range(0, 2)) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.05
        _Occlusion("Occlusion", Range(0, 1)) = 1
        _NormalStrength("Normal From Color", Range(0, 8)) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 0

        [Header(Color Gradient)]
        [Toggle] _IfGradient("Gradient Enabled", Float) = 0
        _BotColor("Bottom Color", Color) = (0.56, 0.18, 0.03, 1)
        _TopColor("Top Color", Color) = (1, 0.6, 0, 1)
        [Enum(UV0 V,0,UV1 V,1,Vertex Color R,2,Object Y,3)] _GradientSource("Gradient Source", Float) = 0
        _GradientPosition("Gradient Height", Range(0.05, 4)) = 1
        _GradientOffset("Gradient Offset", Range(-1, 1)) = 0
        [Toggle] _GradientInvert("Invert Gradient", Float) = 0
        _GradientObjectBottom("Object Bottom", Float) = 0
        _GradientObjectTop("Object Top", Float) = 1
        _GradientNoiseAmount("Gradient Breakup", Range(0, 1)) = 0
        _GradientNoiseScale("Gradient Breakup Scale", Float) = 2

        [Header(Wind)]
        [Toggle] _UseGlobalWind("Use Global Wind", Float) = 1
        _WindResponse("Wind Response", Range(0, 2)) = 0.4
        [Toggle] _Wind_Enabled("Wind Enabled", Float) = 1
        _WindPower("Wind Strength", Range(0, 20)) = 5
        _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        _LeaveSpeed("Wind Speed", Range(0, 10)) = 1
        _WindScale("Wind Wave Scale", Range(0.01, 5)) = 0.35
        _WindChaotic("Wind Turbulence", Range(0, 3)) = 1
        _LeafWindScale("Small Motion Scale", Range(0.01, 20)) = 5
        _LeafWindPower("Small Motion Speed", Range(0.01, 5)) = 1
        _LeafWindSwing("Small Motion Strength", Range(0, 0.5)) = 0.05
        [Enum(UV0 V,0,UV1 V,1,Vertex Color R,2,Object Y,3)] _WindMaskSource("Root Mask Source", Float) = 0
        [Toggle] _WindMaskInvert("Invert Root Mask", Float) = 0
        _WindMaskPower("Root Stiffness", Range(0.1, 8)) = 1.5
        _WindObjectBottom("Object Bottom", Float) = 0
        _WindObjectTop("Object Top", Float) = 1
        _Offset("Geometry Offset", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 300
        Cull [_Cull]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface Surface Standard fullforwardshadows addshadow vertex:Vertex alphatest:_Cutoff
        #pragma multi_compile_instancing

        #include "UnityCG.cginc"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampNatureCommon.hlsl"

        sampler2D _BaseColor;
        float4 _Tint;
        float _Alpha;
        float _Metallic;
        float _Smoothness;
        float _Occlusion;
        float _NormalStrength;
        float _IfGradient;
        float4 _BotColor;
        float4 _TopColor;
        float _GradientSource;
        float _GradientPosition;
        float _GradientOffset;
        float _GradientInvert;
        float _GradientObjectBottom;
        float _GradientObjectTop;
        float _GradientNoiseAmount;
        float _GradientNoiseScale;
        float _UseGlobalWind;
        float _WindResponse;
        float _Wind_Enabled;
        float _WindPower;
        float4 _WindDirection;
        float _LeaveSpeed;
        float _WindScale;
        float _WindChaotic;
        float _LeafWindScale;
        float _LeafWindPower;
        float _LeafWindSwing;
        float _WindMaskSource;
        float _WindMaskInvert;
        float _WindMaskPower;
        float _WindObjectBottom;
        float _WindObjectTop;
        float4 _Offset;

        struct Input
        {
            float2 uv_BaseColor;
            float gradientFactor;
            fixed facing : VFACE;
        };

        void Vertex(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            float3 originalOS = vertex.vertex.xyz;
            float2 uv0 = vertex.texcoord.xy;
            float2 uv1 = vertex.texcoord1.xy;
            float4 vertexColor = vertex.color;

            float bendMask = FantasyCampNatureSelectMask(uv0, uv1, vertexColor,
                originalOS, _WindMaskSource, _WindObjectBottom, _WindObjectTop,
                _WindMaskInvert);
            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled > 0.5)
            {
                float3 positionWS = mul(unity_ObjectToWorld, float4(originalOS, 1.0)).xyz;
                float windStrength = FantasyCampResolveWindStrength(_WindPower,
                    _UseGlobalWind, _WindResponse);
                float2 windDirection = FantasyCampResolveWindDirection(
                    _WindDirection.xy, _UseGlobalWind);
                float windSpeed = FantasyCampResolveWindSpeed(_LeaveSpeed,
                    _UseGlobalWind);
                float2 resolvedWaveScale = FantasyCampResolveWindWaveScale(
                    float2(_WindScale, _WindScale), _UseGlobalWind);
                float windScale = dot(abs(resolvedWaveScale), float2(0.5, 0.5));
                float turbulence = FantasyCampResolveWindTurbulence(
                    _WindChaotic, _UseGlobalWind);
                float flutterScale = FantasyCampResolveFlutterScale(
                    _LeafWindScale, _UseGlobalWind);
                float flutterSpeed = FantasyCampResolveFlutterSpeed(
                    _LeafWindPower, _UseGlobalWind);
                float flutterStrength = FantasyCampResolveFlutterStrength(
                    _LeafWindSwing, _UseGlobalWind);
                float3 windOffsetWS = FantasyCampNatureWindOffsetWS(positionWS, bendMask,
                    _Time.y, windStrength, windDirection, windSpeed, windScale,
                    turbulence, flutterScale, flutterSpeed, flutterStrength,
                    _WindMaskPower);
                vertex.vertex.xyz += mul((float3x3)unity_WorldToObject, windOffsetWS);
            }
            vertex.vertex.xyz += _Offset.xyz;

            output.gradientFactor = FantasyCampNatureGradient(uv0, uv1, vertexColor,
                originalOS, _GradientSource, _GradientObjectBottom, _GradientObjectTop,
                _GradientInvert, _GradientPosition, _GradientOffset,
                _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        void Surface(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 textureSample = tex2D(_BaseColor, input.uv_BaseColor);
            fixed3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                saturate(input.gradientFactor));
            fixed3 tint = _Tint.rgb * lerp(float3(1.0, 1.0, 1.0), gradientColor, saturate(_IfGradient));

            output.Albedo = textureSample.rgb * tint;
            output.Alpha = textureSample.a * _Alpha * _Tint.a;
            output.Metallic = _Metallic;
            output.Smoothness = _Smoothness;
            output.Occlusion = _Occlusion;
            if (_NormalStrength > 0.0001)
            {
                float3 normalTS = FantasyCampNatureNormalFromColor(textureSample.rgb,
                    _NormalStrength);
                output.Normal = normalTS * (input.facing >= 0.0 ? 1.0 : -1.0);
            }
        }
        ENDCG
    }

    FallBack "Transparent/Cutout/Diffuse"
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampNatureShaderGUI"
}
