Shader "Animpic Studio/Fantasy Character/Nature/HDRP"
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
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }
        LOD 300
        Cull [_Cull]

        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampNatureCommon.hlsl"

        TEXTURE2D(_BaseColor);
        SAMPLER(sampler_BaseColor);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor_ST;
            float4 _Tint;
            float4 _BotColor;
            float4 _TopColor;
            float4 _WindDirection;
            float4 _Offset;
            float _Alpha;
            float _Cutoff;
            float _Metallic;
            float _Smoothness;
            float _Occlusion;
            float _NormalStrength;
            float _IfGradient;
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
        CBUFFER_END

        struct NatureAttributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct NatureVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            float gradientFactor : TEXCOORD4;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float3 NaturePositionOS(NatureAttributes input)
        {
            float3 originalOS = input.positionOS;
            float3 displacedOS = originalOS + _Offset.xyz;
            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled > 0.5)
            {
                float bendMask = FantasyCampNatureSelectMask(input.uv, input.uv1,
                    input.color, originalOS, _WindMaskSource, _WindObjectBottom,
                    _WindObjectTop, _WindMaskInvert);
                float3 positionWS = TransformObjectToWorld(originalOS);
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
                float3 offsetWS = FantasyCampNatureWindOffsetWS(positionWS, bendMask,
                    _TimeParameters.x, windStrength, windDirection, windSpeed,
                    windScale, turbulence, flutterScale, flutterSpeed,
                    flutterStrength, _WindMaskPower);
                displacedOS += TransformWorldToObjectDir(offsetWS, false);
            }
            return displacedOS;
        }

        float NatureGradientFactor(NatureAttributes input)
        {
            return FantasyCampNatureGradient(input.uv, input.uv1, input.color,
                input.positionOS, _GradientSource, _GradientObjectBottom,
                _GradientObjectTop, _GradientInvert, _GradientPosition,
                _GradientOffset, _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        NatureVaryings NatureVert(NatureAttributes input)
        {
            NatureVaryings output = (NatureVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 positionOS = NaturePositionOS(input);
            output.positionWS = TransformObjectToWorld(positionOS);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz),
                input.tangentOS.w * GetOddNegativeScale());
            output.uv = input.uv * _BaseColor_ST.xy + _BaseColor_ST.zw;
            output.gradientFactor = NatureGradientFactor(input);
            return output;
        }

        float4 NatureSample(float2 uv)
        {
            float4 sampleValue = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            return sampleValue;
        }

        float4 NatureDepthFragment(NatureVaryings input) : SV_Target
        {
            NatureSample(input.uv);
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex NatureVert
            #pragma fragment NatureFragment
            #pragma multi_compile_instancing

            float4 NatureFragment(NatureVaryings input,
                bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 textureSample = NatureSample(input.uv);
                float3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                    saturate(input.gradientFactor));
                float3 albedo = textureSample.rgb * _Tint.rgb
                    * lerp(float3(1.0, 1.0, 1.0), gradientColor, saturate(_IfGradient));

                float faceSign = isFrontFace ? 1.0 : -1.0;
                float3 normalWS = normalize(input.normalWS) * faceSign;
                if (_NormalStrength > 0.0001)
                {
                    float3 normalTS = FantasyCampNatureNormalFromColor(textureSample.rgb,
                        _NormalStrength);
                    float3 bitangentWS = cross(normalWS, input.tangentWS.xyz)
                        * input.tangentWS.w;
                    normalWS = normalize(mul(normalTS,
                        float3x3(input.tangentWS.xyz, bitangentWS, normalWS)));
                }

                float3 lighting = 0.18 * _Occlusion;
                float3 specularLighting = 0.0;
                if (_DirectionalLightCount > 0)
                {
                    DirectionalLightData light = _DirectionalLightDatas[0];
                    float3 lightDirectionWS = -light.forward;
                    float ndotl = saturate(dot(normalWS, lightDirectionWS));
                    lighting += light.color * ndotl;

                    float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                    float3 halfDirection = normalize(lightDirectionWS + viewDirectionWS);
                    float specularPower = exp2(1.0 + _Smoothness * 10.0);
                    float specular = pow(saturate(dot(normalWS, halfDirection)), specularPower)
                        * lerp(0.04, 1.0, _Metallic);
                    specularLighting = light.color * specular
                        * lerp(float3(1.0, 1.0, 1.0), albedo, _Metallic);
                }
                return float4(albedo * lighting + specularLighting, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex NatureVert
            #pragma fragment NatureDepthFragment
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
            #pragma vertex NatureVert
            #pragma fragment NatureDepthFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampNatureShaderGUI"
}
