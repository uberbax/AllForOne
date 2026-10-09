Shader "Animpic Studio/Fantasy Character/Leaf/HDRP"
{
    Properties
    {
        [Header(Appearance)]
        [MainTexture] _BaseColor("Base Color Texture", 2D) = "white" {}
        [HideInInspector] _MainTex("Legacy Main Texture", 2D) = "white" {}
        [MainColor] _Tint("Texture Tint", Color) = (1, 1, 1, 1)
        _TextureColorStrength("Texture Color Strength", Range(0, 1)) = 1
        _TintStrength("Tint Strength", Range(0, 1)) = 1
        _Alpha("Alpha", Range(0, 2)) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.05
        _Occlusion("Occlusion", Range(0, 1)) = 1
        _NormalStrength("Normal From Color", Range(0, 8)) = 0
        _BackfaceLighting("Backface Lighting", Range(0, 1)) = 0.65
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0, 1)) = 0.8
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 0

        [Header(Leaf Transmission)]
        _TransmissionColor("Transmission Tint", Color) = (1, 1, 1, 1)
        _TransmissionStrength("Transmission Strength", Range(0, 2)) = 0.65
        _TransmissionPower("Transmission Sharpness", Range(0.5, 8)) = 1.5
        _TransmissionAmbient("Indirect Transmission", Range(0, 1)) = 0.12
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
        _WindResponse("Wind Response", Range(0, 2)) = 1
        [Toggle] _Wind_Enabled("Wind Enabled", Float) = 1
        _WindPower("Wind Strength", Range(0, 20)) = 5
        _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0, 10)) = 1
        _UV("World Wave Scale", Vector) = (0.35, 0.35, 0, 0)
        _WindTurbulence("Wind Turbulence", Range(0, 3)) = 1
        _LeafWindScale("Flutter Scale", Range(0.01, 20)) = 5
        _LeafWindPower("Flutter Speed", Range(0.01, 5)) = 1
        _LeafWindSwing("Flutter Strength", Range(0, 0.5)) = 0.05
        _WindRootHeight("Root Height (Object Y)", Float) = 0
        _WindFlexibility("Flexibility per Meter", Range(0.01, 2)) = 0.65
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
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampLeafCommon.hlsl"

        TEXTURE2D(_BaseColor);
        SAMPLER(sampler_BaseColor);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor_ST;
            float4 _Tint;
            float4 _BotColor;
            float4 _TopColor;
            float4 _TransmissionColor;
            float4 _WindDirection;
            float4 _UV;
            float _Alpha;
            float _Cutoff;
            float _TextureColorStrength;
            float _TintStrength;
            float _Metallic;
            float _Smoothness;
            float _Occlusion;
            float _NormalStrength;
            float _TransmissionStrength;
            float _TransmissionPower;
            float _TransmissionAmbient;
            float _BackfaceLighting;
            float _ReceiveShadowStrength;
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
            float _WindSpeed;
            float _WindTurbulence;
            float _LeafWindScale;
            float _LeafWindPower;
            float _LeafWindSwing;
            float _WindRootHeight;
            float _WindFlexibility;
        CBUFFER_END

        struct LeafAttributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct LeafVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            float gradientFactor : TEXCOORD4;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float3 LeafPositionOS(LeafAttributes input)
        {
            float3 originalOS = input.positionOS;
            float3 displacedOS = originalOS;
            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled > 0.5)
            {
                float3 positionWS = GetAbsolutePositionWS(
                    TransformObjectToWorld(originalOS));
                float3 originWS = GetAbsolutePositionWS(
                    TransformObjectToWorld(float3(0.0, 0.0, 0.0)));
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
                float flutterScale = FantasyCampResolveFlutterScale(
                    _LeafWindScale, _UseGlobalWind);
                float flutterSpeed = FantasyCampResolveFlutterSpeed(
                    _LeafWindPower, _UseGlobalWind);
                float flutterStrength = FantasyCampResolveFlutterStrength(
                    _LeafWindSwing, _UseGlobalWind);
                float3 offsetWS = FantasyCampLeafWindOffsetWS(positionWS,
                    originWS, originalOS.y, _TimeParameters.x, windStrength,
                    windDirection, windSpeed, waveScale, turbulence,
                    rootHeight, flexibility, flutterScale, flutterSpeed,
                    flutterStrength);
                displacedOS += TransformWorldToObjectDir(offsetWS, false);
            }
            return displacedOS;
        }

        float LeafGradientFactor(LeafAttributes input)
        {
            return FantasyCampLeafGradient(input.uv, input.uv1, input.color,
                input.positionOS, _GradientSource, _GradientObjectBottom,
                _GradientObjectTop, _GradientInvert, _GradientPosition,
                _GradientOffset, _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        LeafVaryings LeafVert(LeafAttributes input)
        {
            LeafVaryings output = (LeafVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 positionOS = LeafPositionOS(input);
            output.positionWS = TransformObjectToWorld(positionOS);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz),
                input.tangentOS.w * GetOddNegativeScale());
            output.uv = input.uv * _BaseColor_ST.xy + _BaseColor_ST.zw;
            output.gradientFactor = LeafGradientFactor(input);
            return output;
        }

        float4 LeafSample(float2 uv)
        {
            float4 sampleValue = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            return sampleValue;
        }

        float4 LeafDepthFragment(LeafVaryings input) : SV_Target
        {
            LeafSample(input.uv);
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex LeafVert
            #pragma fragment LeafFragment
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
            #else
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH
            #endif
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"

            float4 LeafFragment(LeafVaryings input,
                bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 textureSample = LeafSample(input.uv);
                float3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                    saturate(input.gradientFactor));
                float3 albedo = FantasyCampLeafAlbedo(textureSample.rgb,
                    _TextureColorStrength, _Tint.rgb, _TintStrength,
                    gradientColor, _IfGradient);

                float faceSign = isFrontFace ? 1.0 : -1.0;
                float3 baseNormalWS = normalize(input.normalWS);
                float3 normalWS = baseNormalWS * faceSign;
                if (_NormalStrength > 0.0001)
                {
                    float3 normalTS = FantasyCampLeafNormalFromColor(textureSample.rgb,
                        _NormalStrength);
                    normalTS.z *= faceSign;
                    float3 bitangentWS = cross(baseNormalWS, input.tangentWS.xyz)
                        * input.tangentWS.w;
                    normalWS = normalize(mul(normalTS,
                        float3x3(input.tangentWS.xyz, bitangentWS, baseNormalWS)));
                }

                float backfaceLighting = isFrontFace
                    ? 1.0
                    : saturate(_BackfaceLighting);
                float3 lighting = 0.18 * _Occlusion;
                float3 transmissionLighting = _TransmissionStrength > 0.0001
                    ? 0.18 * _Occlusion * _TransmissionAmbient
                    : 0.0;
                float3 specularLighting = 0.0;
                if (_DirectionalLightCount > 0)
                {
                    DirectionalLightData light = _DirectionalLightDatas[0];
                    float3 lightDirectionWS = -light.forward;
                    float3 diffuseLightColor = light.color * light.lightDimmer
                        * light.diffuseDimmer;
                    float rawShadow = 1.0;
                    if (light.shadowIndex >= 0 && light.shadowDimmer > 0.0)
                    {
                        HDShadowContext shadowContext = InitShadowContext();
                        rawShadow = GetDirectionalShadowAttenuation(shadowContext,
                            input.positionCS.xy, input.positionWS, baseNormalWS,
                            light.shadowIndex, lightDirectionWS);
                        rawShadow = lerp(1.0, rawShadow,
                            saturate(light.shadowDimmer));
                    }
                    float receiveShadow = lerp(1.0, rawShadow,
                        saturate(_ReceiveShadowStrength));
                    float ndotl = saturate(dot(normalWS, lightDirectionWS));
                    lighting += diffuseLightColor * ndotl * receiveShadow
                        * backfaceLighting;
                    if (_TransmissionStrength > 0.0001)
                    {
                        float backLighting = FantasyCampLeafTransmission(normalWS,
                            lightDirectionWS, _TransmissionPower);
                        transmissionLighting += diffuseLightColor * backLighting
                            * receiveShadow * backfaceLighting;
                    }

                    float3 viewDirectionWS =
                        GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 halfDirection = normalize(lightDirectionWS + viewDirectionWS);
                    float specularPower = exp2(1.0 + _Smoothness * 10.0);
                    float specular = pow(saturate(dot(normalWS, halfDirection)),
                        specularPower) * lerp(0.04, 1.0, _Metallic);
                    specularLighting = light.color * light.lightDimmer
                        * light.specularDimmer * specular
                        * lerp(float3(1.0, 1.0, 1.0), albedo, _Metallic)
                        * receiveShadow * backfaceLighting;
                }
                float3 finalColor = (albedo * lighting
                    + albedo * _TransmissionColor.rgb * _TransmissionStrength
                        * (1.0 - _Metallic) * transmissionLighting
                    + specularLighting)
                    * GetCurrentExposureMultiplier();
                return float4(finalColor, 1.0);
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
            #pragma vertex LeafVert
            #pragma fragment LeafDepthFragment
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
            #pragma vertex LeafVert
            #pragma fragment LeafDepthFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampLeafShaderGUI"
}
