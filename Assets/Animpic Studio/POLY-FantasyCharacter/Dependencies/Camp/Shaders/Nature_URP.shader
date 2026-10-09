Shader "Animpic Studio/Fantasy Character/Nature/URP"
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
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }
        LOD 300
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 NaturePositionOS(NatureAttributes input)
        {
            float3 originalOS = input.positionOS.xyz;
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
                    _Time.y, windStrength, windDirection, windSpeed, windScale,
                    turbulence, flutterScale, flutterSpeed, flutterStrength,
                    _WindMaskPower);
                displacedOS += TransformWorldToObjectDir(offsetWS, false);
            }
            return displacedOS;
        }

        float NatureGradientFactor(NatureAttributes input)
        {
            return FantasyCampNatureGradient(input.uv, input.uv1, input.color,
                input.positionOS.xyz, _GradientSource, _GradientObjectBottom,
                _GradientObjectTop, _GradientInvert, _GradientPosition,
                _GradientOffset, _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        half4 NatureSample(float2 uv)
        {
            half4 sampleValue = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            return sampleValue;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NatureVert
            #pragma fragment NatureFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct NatureVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half gradientFactor : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            NatureVaryings NatureVert(NatureAttributes input)
            {
                NatureVaryings output = (NatureVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = NaturePositionOS(input);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                output.gradientFactor = NatureGradientFactor(input);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                return output;
            }

            half4 NatureFrag(NatureVaryings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 textureSample = NatureSample(input.uv);
                half3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                    saturate(input.gradientFactor));
                half3 albedo = textureSample.rgb * _Tint.rgb
                    * lerp(half3(1.0h, 1.0h, 1.0h), gradientColor,
                        saturate(_IfGradient));

                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS) * faceSign;
                if (_NormalStrength > 0.0001)
                {
                    half3 normalTS = FantasyCampNatureNormalFromColor(textureSample.rgb,
                        _NormalStrength);
                    half3 bitangentWS = cross(normalWS, input.tangentWS.xyz)
                        * input.tangentWS.w;
                    normalWS = NormalizeNormalPerPixel(mul(normalTS,
                        half3x3(input.tangentWS.xyz, bitangentWS, normalWS)));
                }

                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS)
                    * _Occlusion;
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half mainAttenuation = mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 color = albedo * (bakedGI + mainLight.color * ndotl * mainAttenuation);

                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = exp2(1.0h + _Smoothness * 10.0h);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower)
                    * lerp(0.04h, 1.0h, _Metallic) * mainAttenuation;
                color += mainLight.color * specular
                    * lerp(half3(1.0h, 1.0h, 1.0h), albedo, _Metallic);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half attenuation = light.distanceAttenuation * light.shadowAttenuation;
                    color += albedo * light.color * saturate(dot(normalWS, light.direction))
                        * attenuation;
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
            #pragma vertex NatureShadowVert
            #pragma fragment NatureDepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            float3 _LightDirection;
            float3 _LightPosition;

            struct NatureDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            NatureDepthVaryings NatureShadowVert(NatureAttributes input)
            {
                NatureDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionOS = NaturePositionOS(input);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS,
                    normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z,
                        output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z,
                        output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }

            half4 NatureDepthFrag(NatureDepthVaryings input) : SV_Target
            {
                NatureSample(input.uv);
                return 0;
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
            #pragma target 3.0
            #pragma vertex NatureDepthVert
            #pragma fragment NatureDepthFrag
            #pragma multi_compile_instancing

            struct NatureDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            NatureDepthVaryings NatureDepthVert(NatureAttributes input)
            {
                NatureDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(NaturePositionOS(input));
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }

            half4 NatureDepthFrag(NatureDepthVaryings input) : SV_Target
            {
                NatureSample(input.uv);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampNatureShaderGUI"
}
