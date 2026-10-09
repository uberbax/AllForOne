Shader "Animpic Studio/Fantasy Character/Water/HDRP"
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
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="HDLitShader"
            "Queue"="Transparent"
            "DisableBatching"="True"
        }
        LOD 300

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="Forward" }
            // Refraction is already composed into Base Color. Do not blend it twice.
            Blend One Zero
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]
            ColorMask 0 1
            ColorMask 0 2
            Stencil
            {
                Ref 0
                WriteMask 3
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            #if UNITY_VERSION >= 202220
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
            #else
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH
            #endif
            #pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma multi_compile_fragment _ LIGHT_LAYERS

            #ifndef SHADER_STAGE_FRAGMENT
                #define SHADOW_LOW
                #define USE_FPTL_LIGHTLIST
            #endif

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _ENABLE_FOG_ON_TRANSPARENT 1
            #define _DISABLE_DECALS 1
            #define _DISABLE_SSR 1
            #define _DISABLE_SSR_TRANSPARENT 1
            #define _DOUBLESIDED_ON 1
            #define HAVE_MESH_MODIFICATION 1

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #define FANTASY_CAMP_WATER_SRP
            #include "Assets/Animpic Studio/POLY-FantasyCharacter/Dependencies/Camp/Shaders/HLSL/FantasyCampWaterCommon.hlsl"
            TEXTURE2D(_RefreactionNormal);
            SAMPLER(sampler_RefreactionNormal);

            // Material.hlsl uses this for its native alpha/fog composition.
            // Final opacity is one, so this is not an additional transparency control.
            static const float _BlendMode = 0.0;
            #if UNITY_VERSION >= 202220
            static const float4 _DoubleSidedConstants = float4(-1.0, -1.0, -1.0, 0.0);
            #endif
            #define SHADERPASS SHADERPASS_FORWARD
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #define HAS_LIGHTLOOP
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                input.positionOS = FantasyCampWaterPositionOS(input.positionOS,
                    timeParameters.x);
                return input;
            }

            void GetSurfaceAndBuiltinData(FragInputs input, float3 V,
                inout PositionInputs posInput, out SurfaceData surfaceData,
                out BuiltinData builtinData)
            {
                // positionRWS is camera-relative. Never add the camera position before
                // HDRP's view transforms or scene depth comparisons.
                float surfaceEyeDepth = max(-TransformWorldToView(input.positionRWS).z, 0.0);
                posInput.linearDepth = surfaceEyeDepth;
                float2 screenUV = posInput.positionNDC;
                float depthDifference = 100000.0;
                if (_DepthEnabled > 0.5)
                {
                    float sceneEyeDepth = FantasyCampWaterEyeDepth(
                        SampleCameraDepth(screenUV));
                    depthDifference = max(sceneEyeDepth - surfaceEyeDepth, 0.0);
                }

                float3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_RefreactionNormal,
                    sampler_RefreactionNormal, FantasyCampWaterNormalUV(
                        input.texCoord0.xy, _TimeParameters.x, 1.0)));
                float3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_RefreactionNormal,
                    sampler_RefreactionNormal, FantasyCampWaterNormalUV(
                        input.texCoord0.xy, _TimeParameters.x, -1.0)));
                FantasyCampWaterSurface water = FantasyCampEvaluateWater(
                    input.texCoord0.xy, _TimeParameters.x, normalA, normalB,
                    depthDifference);

                float3 sceneColor = water.color.rgb;
                if (_RefractionEnabled > 0.5 && water.color.a < 0.9999)
                {
                    float2 refractedUV = FantasyCampWaterRefractionUV(screenUV,
                        water.refractionOffset, _ScreenSize.zw);
                    if (_DepthEnabled > 0.5)
                    {
                        float refractedEyeDepth = FantasyCampWaterEyeDepth(
                            SampleCameraDepth(refractedUV));
                        if (refractedEyeDepth <= surfaceEyeDepth + 0.001)
                            refractedUV = screenUV;
                    }
                    // HDRP's color pyramid is pre-exposed; Lit applies exposure later.
                    sceneColor = SampleCameraColor(refractedUV)
                        * GetInverseCurrentExposureMultiplier();
                }

                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
                surfaceData.baseColor = FantasyCampWaterComposite(water.color, sceneColor);
                surfaceData.perceptualSmoothness = saturate(_Smoothness);
                surfaceData.metallic = 0.0;
                surfaceData.ambientOcclusion = 1.0;
                surfaceData.specularOcclusion = 1.0;
                surfaceData.ior = 1.0;
                surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
                surfaceData.atDistance = 1.0;

                // Use the same two-sided tangent-normal handling as HDRP Shader Graph.
                float3 doubleSidedConstants = float3(-1.0, -1.0, -1.0);
                ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants);
                GetNormalWS(input, water.normalTS, surfaceData.normalWS,
                    doubleSidedConstants);
                surfaceData.geomNormalWS = input.tangentToWorld[2];
                surfaceData.tangentWS = Orthonormalize(
                    normalize(input.tangentToWorld[0]), surfaceData.normalWS);

                InitBuiltinData(posInput, 1.0, surfaceData.normalWS,
                    -input.tangentToWorld[2], input.texCoord1, input.texCoord2,
                    builtinData);
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
            }

            // Native HDRP Lit forward: light lists, shadow maps, probes, exposure and fog.
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
    CustomEditor "Animpic.Local.POLYFantasyCharacter.Camp.FantasyCampWaterShaderGUI"
}
