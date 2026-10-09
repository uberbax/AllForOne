Shader "Animpic Studio/Fantasy Character/Leaf/URP"
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
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }
        LOD 300
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 LeafPositionOS(LeafAttributes input)
        {
            float3 originalOS = input.positionOS.xyz;
            float3 displacedOS = originalOS;
            float windEnabled = FantasyCampResolveWindEnabled(_Wind_Enabled,
                _UseGlobalWind);
            if (windEnabled > 0.5)
            {
                float3 positionWS = TransformObjectToWorld(originalOS);
                float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
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
                    originWS, originalOS.y, _Time.y, windStrength,
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
                input.positionOS.xyz, _GradientSource, _GradientObjectBottom,
                _GradientObjectTop, _GradientInvert, _GradientPosition,
                _GradientOffset, _GradientNoiseAmount, _GradientNoiseScale,
                float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0)).x;
        }

        half4 LeafSample(float2 uv)
        {
            half4 sampleValue = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            return sampleValue;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LeafVert
            #pragma fragment LeafFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct LeafVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half gradientFactor : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
                half3 vertexLighting : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            LeafVaryings LeafVert(LeafAttributes input)
            {
                LeafVaryings output = (LeafVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = LeafPositionOS(input);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS,
                    input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                output.gradientFactor = LeafGradientFactor(input);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                output.vertexLighting = VertexLighting(positionInputs.positionWS,
                    output.normalWS);
                return output;
            }

            half4 LeafFrag(LeafVaryings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 textureSample = LeafSample(input.uv);
                half3 gradientColor = lerp(_BotColor.rgb, _TopColor.rgb,
                    saturate(input.gradientFactor));
                half3 albedo = FantasyCampLeafAlbedo(textureSample.rgb,
                    _TextureColorStrength, _Tint.rgb, _TintStrength,
                    gradientColor, _IfGradient);

                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 baseNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 normalWS = baseNormalWS * faceSign;
                if (_NormalStrength > 0.0001)
                {
                    half3 normalTS = FantasyCampLeafNormalFromColor(textureSample.rgb,
                        _NormalStrength);
                    normalTS.z *= faceSign;
                    half3 bitangentWS = cross(baseNormalWS, input.tangentWS.xyz)
                        * input.tangentWS.w;
                    normalWS = NormalizeNormalPerPixel(mul(normalTS,
                        half3x3(input.tangentWS.xyz, bitangentWS, baseNormalWS)));
                }

                half backfaceLighting = faceSign > 0.0h
                    ? 1.0h
                    : saturate(_BackfaceLighting);
                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS)
                    * _Occlusion;
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half mainShadow = lerp(1.0h, mainLight.shadowAttenuation,
                    saturate(_ReceiveShadowStrength));
                half mainAttenuation = mainLight.distanceAttenuation * mainShadow;
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 color = albedo * (bakedGI
                    + mainLight.color * ndotl * mainAttenuation
                        * backfaceLighting);
                half3 transmissionTint = albedo * _TransmissionColor.rgb
                    * _TransmissionStrength * (1.0h - _Metallic);
                if (_TransmissionStrength > 0.0001)
                {
                    half mainTransmission = FantasyCampLeafTransmission(normalWS,
                        mainLight.direction, _TransmissionPower);
                    color += transmissionTint
                        * (mainLight.color * mainTransmission
                            * mainLight.distanceAttenuation * mainShadow
                            * backfaceLighting
                            + bakedGI * _TransmissionAmbient);
                }

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    color += albedo * input.vertexLighting
                        * backfaceLighting;
                #endif

                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = exp2(1.0h + _Smoothness * 10.0h);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower)
                    * lerp(0.04h, 1.0h, _Metallic) * mainAttenuation;
                color += mainLight.color * specular
                    * lerp(half3(1.0h, 1.0h, 1.0h), albedo, _Metallic)
                    * backfaceLighting;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS,
                        half4(1.0h, 1.0h, 1.0h, 1.0h));
                    half lightShadow = lerp(1.0h, light.shadowAttenuation,
                        saturate(_ReceiveShadowStrength));
                    half attenuation = light.distanceAttenuation * lightShadow;
                    color += albedo * light.color
                        * saturate(dot(normalWS, light.direction)) * attenuation
                        * backfaceLighting;
                    if (_TransmissionStrength > 0.0001)
                    {
                        half lightTransmission = FantasyCampLeafTransmission(
                            normalWS, light.direction, _TransmissionPower);
                        color += transmissionTint * light.color
                            * lightTransmission * light.distanceAttenuation
                            * lightShadow * backfaceLighting;
                    }
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
            #pragma vertex LeafShadowVert
            #pragma fragment LeafDepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            float3 _LightDirection;
            float3 _LightPosition;

            struct LeafDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            LeafDepthVaryings LeafShadowVert(LeafAttributes input)
            {
                LeafDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionOS = LeafPositionOS(input);
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

            half4 LeafDepthFrag(LeafDepthVaryings input) : SV_Target
            {
                LeafSample(input.uv);
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
            #pragma vertex LeafDepthVert
            #pragma fragment LeafDepthFrag
            #pragma multi_compile_instancing

            struct LeafDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            LeafDepthVaryings LeafDepthVert(LeafAttributes input)
            {
                LeafDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(LeafPositionOS(input));
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }

            half4 LeafDepthFrag(LeafDepthVaryings input) : SV_Target
            {
                LeafSample(input.uv);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampLeafShaderGUI"
}
