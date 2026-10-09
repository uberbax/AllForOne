Shader "Animpic Studio/Fantasy Character/Tree/URP"
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
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "DisableBatching"="True"
        }
        LOD 300
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampTreeCommon.hlsl"

        TEXTURE2D(_BaseColor);
        SAMPLER(sampler_BaseColor);
        TEXTURE2D(_MossTexture);
        SAMPLER(sampler_MossTexture);
        TEXTURE2D(_TopMossTexture);
        SAMPLER(sampler_TopMossTexture);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor_ST;
            float4 _Tint;
            float4 _TreeBottomColor;
            float4 _TreeTopColor;
            float4 _WindDirection;
            float4 _UV;
            float _Alpha;
            float _AlphaClip;
            float _Cutoff;
            float _Metallic;
            float _Smoothness;
            float _Occlusion;
            float _NormalStrength;
            float _TreeGradientEnabled;
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
            float _WindSpeed;
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
        CBUFFER_END

        float4 TreeSampleMossTriplanar(float3 positionOS, float3 weights)
        {
            float scale = max(abs(_MossTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_MossTexture, sampler_MossTexture, positionOS.xy * scale);
            return sideX * weights.x + topY * weights.y + sideZ * weights.z;
        }

        float4 TreeSampleTopMossTriplanar(float3 positionOS, float3 weights)
        {
            float scale = max(abs(_TopMossTiling), 0.0001);
            float4 sideX = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.zy * scale);
            float4 topY = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xz * scale);
            float4 sideZ = SAMPLE_TEXTURE2D(_TopMossTexture, sampler_TopMossTexture, positionOS.xy * scale);
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
                    float3 moss = TreeSampleMossTriplanar(mossPositionOS, mossWeights).rgb * _MossColor.rgb;
                    albedo = lerp(albedo, moss, mossMask);
                }
                if (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0)
                {
                    float topMossMask = FantasyCampTopMossMask(mossHeightFromPivot,
                        _TopMossTopHeight, _TopMossHeight, _TopMossEdgeSoftness,
                        _TopMossIntensity);
                    float3 topMoss = TreeSampleTopMossTriplanar(mossPositionOS, mossWeights).rgb * _TopMossColor.rgb;
                    albedo = lerp(albedo, topMoss, topMossMask);
                }
            }
            return albedo;
        }

        struct TreeAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 TreePositionOS(TreeAttributes input)
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
                float3 offsetWS = FantasyCampTreeWindOffsetWS(positionWS,
                    originWS, originalOS.y, _Time.y, windStrength,
                    windDirection, windSpeed, waveScale, turbulence,
                    rootHeight, flexibility);
                displacedOS += TransformWorldToObjectDir(offsetWS, false);
            }
            return displacedOS;
        }

        float TreeGradient(TreeAttributes input)
        {
            return FantasyCampTreeGradientFactor(input.uv, input.uv1, input.color,
                input.positionOS.xyz, _TreeGradientSource,
                _TreeGradientObjectBottom, _TreeGradientObjectTop,
                _TreeGradientInvert, _TreeGradientScale, _TreeGradientOffset,
                _TreeGradientBreakup, _TreeGradientBreakupScale);
        }

        half4 TreeSample(float2 uv)
        {
            half4 sampleValue = SAMPLE_TEXTURE2D(_BaseColor, sampler_BaseColor, uv);
            #if defined(_ALPHATEST_ON)
                clip(sampleValue.a * _Alpha * _Tint.a - _Cutoff);
            #endif
            return sampleValue;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TreeVert
            #pragma fragment TreeFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct TreeVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 treeSurfaceData : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
                half3 vertexLighting : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TreeVaryings TreeVert(TreeAttributes input)
            {
                TreeVaryings output = (TreeVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = TreePositionOS(input);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS,
                    input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                // Rest-pose coordinates are independent of the vertex wind deformation.
                output.treeSurfaceData = float4(input.positionOS.xyz, TreeGradient(input));
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                output.vertexLighting = VertexLighting(positionInputs.positionWS,
                    output.normalWS);
                return output;
            }

            half4 TreeFrag(TreeVaryings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 textureSample = TreeSample(input.uv);
                half3 gradientColor = lerp(_TreeBottomColor.rgb, _TreeTopColor.rgb,
                    saturate(input.treeSurfaceData.w));
                half3 albedo = textureSample.rgb * _Tint.rgb
                    * lerp(half3(1.0h, 1.0h, 1.0h), gradientColor,
                        saturate(_TreeGradientEnabled));

                if (_MossEnabled > 0.5 || (_TopMossEnabled > 0.5 && _TopMossIntensity > 0.0 && _TopMossHeight > 0.0))
                {
                    // Match Surface's world-up mask using the visible, bent geometry.
                    float3 displacedOS = TransformWorldToObject(input.positionWS);
                    float mossHeightFromPivot = FantasyCampMossHeightFromPivot(
                        displacedOS, (float3x3)GetObjectToWorldMatrix());
                    albedo = TreeApplyMoss(albedo, input.treeSurfaceData.xyz,
                        mossHeightFromPivot);
                }

                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS) * faceSign;
                if (_NormalStrength > 0.0001)
                {
                    half3 normalTS = FantasyCampTreeNormalFromColor(textureSample.rgb,
                        _NormalStrength);
                    half3 bitangentWS = cross(normalWS, input.tangentWS.xyz)
                        * input.tangentWS.w;
                    normalWS = NormalizeNormalPerPixel(mul(normalTS,
                        half3x3(input.tangentWS.xyz, bitangentWS, normalWS)));
                }

                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS)
                    * _Occlusion;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half attenuation = mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 color = albedo * (bakedGI + mainLight.color * ndotl * attenuation);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    color += albedo * input.vertexLighting;
                #endif

                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = exp2(1.0h + _Smoothness * 10.0h);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower)
                    * lerp(0.04h, 1.0h, _Metallic) * attenuation;
                color += mainLight.color * specular
                    * lerp(half3(1.0h, 1.0h, 1.0h), albedo, _Metallic);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half lightAttenuation = light.distanceAttenuation
                        * light.shadowAttenuation;
                    color += albedo * light.color
                        * saturate(dot(normalWS, light.direction)) * lightAttenuation;
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
            #pragma vertex TreeShadowVert
            #pragma fragment TreeDepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            float3 _LightDirection;
            float3 _LightPosition;

            struct TreeDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TreeDepthVaryings TreeShadowVert(TreeAttributes input)
            {
                TreeDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionOS = TreePositionOS(input);
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

            half4 TreeDepthFrag(TreeDepthVaryings input) : SV_Target
            {
                TreeSample(input.uv);
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
            #pragma vertex TreeDepthVert
            #pragma fragment TreeDepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct TreeDepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TreeDepthVaryings TreeDepthVert(TreeAttributes input)
            {
                TreeDepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(TreePositionOS(input));
                output.uv = TRANSFORM_TEX(input.uv, _BaseColor);
                return output;
            }

            half4 TreeDepthFrag(TreeDepthVaryings input) : SV_Target
            {
                TreeSample(input.uv);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampTreeShaderGUI"
}
