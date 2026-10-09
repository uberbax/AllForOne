Shader "Animpic Studio/Fantasy Character/Water/URP"
{
    Properties
    {
        _SurfaceColor("Shallow Color (A = Density)", Color) = (0, 0.74793315, 1, 1)
        _DeepColor("Deep Color (A = Density)", Color) = (0.0064524733, 0.2375831, 0.2735849, 1)
        _Distance("Depth Color Distance", Float) = 2
        [Normal] _RefreactionNormal("Water Normal Map", 2D) = "bump" {}
        _RefractionScale("Normal Tiling", Vector) = (5, 5, 0, 0)
        _RefractionSpeed("Normal Scroll Speed", Float) = 0.25
        _RefractionPower("Refraction Strength", Float) = 1
        _NormalPower("Normal Strength", Float) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _FoamColor("Foam Color (A = Coverage)", Color) = (0.8242257, 0.9245283, 0.92248, 1)
        _FoamAmount("Foam Depth Distance", Float) = 2
        _FoamSpeed("Foam Scroll Speed", Float) = 1
        _FoamScale("Foam Pattern Scale", Float) = 30.73
        _FoamCuttoff("Foam Threshold", Float) = 1
        _HightFrequency("Wave Tiling / Density", Float) = 1
        _WaveSpeed("Wave Speed", Float) = 1
        _WaveAmplitude("Wave Amplitude (Object Units)", Float) = 1
        [ToggleUI] _DepthEnabled("Depth Color Enabled", Float) = 1
        [ToggleUI] _RefractionEnabled("Scene Refraction Enabled", Float) = 1
        [ToggleUI] _FoamEnabled("Shore Foam Enabled", Float) = 1
        [ToggleUI] _WavesEnabled("Vertex Waves Enabled", Float) = 1
        [Enum(Off,0,Front,1,Back,2)] _Cull("Cull Mode", Float) = 0
        [ToggleUI] _ZWrite("Write Depth", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Lit"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest LEqual
            // Refraction is composed once inside the shader, not blended twice.
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Transparent receivers must use shadow maps, not opaque screen shadows.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #define FANTASY_CAMP_WATER_SRP 1
            #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampWaterCommon.hlsl"

            TEXTURE2D(_RefreactionNormal);
            SAMPLER(sampler_RefreactionNormal);
            float4 _CameraOpaqueTexture_TexelSize;

            struct WaterAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct WaterVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half4 fogAndVertexLight : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            WaterVaryings WaterVertex(WaterAttributes input)
            {
                WaterVaryings output = (WaterVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = FantasyCampWaterPositionOS(input.positionOS.xyz, _Time.y);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                // Preserve the graph's mesh normals/tangents; only position is displaced.
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.fogAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z),
                    VertexLighting(positionInputs.positionWS, normalInputs.normalWS));
                return output;
            }

            half4 WaterFragment(WaterVaryings input,
                FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float surfaceEyeDepth = -TransformWorldToView(input.positionWS).z;
                float depthDifference = 100000.0;
                if (_DepthEnabled > 0.5)
                    depthDifference = FantasyCampWaterEyeDepth(SampleSceneDepth(screenUV)) - surfaceEyeDepth;

                float2 normalUVA = FantasyCampWaterNormalUV(input.uv, _Time.y, 1.0);
                float2 normalUVB = FantasyCampWaterNormalUV(input.uv, _Time.y, -1.0);
                half3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_RefreactionNormal, sampler_RefreactionNormal, normalUVA));
                half3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_RefreactionNormal, sampler_RefreactionNormal, normalUVB));
                FantasyCampWaterSurface water = FantasyCampEvaluateWater(input.uv, _Time.y,
                    normalA, normalB, depthDifference);

                float3 sceneColor = water.color.rgb;
                if (_RefractionEnabled > 0.5 && water.color.a < 0.9999)
                {
                    float2 refractionUV = FantasyCampWaterRefractionUV(screenUV,
                        water.refractionOffset, _CameraOpaqueTexture_TexelSize.xy);
                    if (_DepthEnabled > 0.5)
                    {
                        float refractedEyeDepth = FantasyCampWaterEyeDepth(SampleSceneDepth(refractionUV));
                        // Do not pull an object in front of the water into its surface.
                        if (refractedEyeDepth <= surfaceEyeDepth + 0.001)
                            refractionUV = screenUV;
                    }
                    sceneColor = SampleSceneColor(refractionUV);
                }

                half3 geometricNormalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 tangentWS = SafeNormalize(input.tangentWS.xyz);
                half3 bitangentWS = cross(geometricNormalWS, tangentWS) * input.tangentWS.w;
                half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, geometricNormalWS);
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(water.normalTS, tangentToWorld));
                normalWS *= IS_FRONT_VFACE(facing, 1.0h, -1.0h);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.tangentToWorld = tangentToWorld;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogAndVertexLight.x);
                inputData.vertexLighting = input.fogAndVertexLight.yzw;
                // Per-pixel SH also uses the flipped, normal-mapped back face.
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = unity_ProbesOcclusion;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = FantasyCampWaterComposite(water.color, sceneColor);
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.04h, 0.04h, 0.04h);
                surfaceData.smoothness = saturate(_Smoothness);
                surfaceData.normalTS = water.normalTS;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }
        // No opaque depth/shadow passes: camera depth must contain the scene below water.
    }
    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampWaterShaderGUI"
}
